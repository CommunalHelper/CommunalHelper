namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[CustomEntity("CommunalHelper/SJ/LoopBlock")]
public class LoopBlock : Solid
{
    // The third dimension is to store the same tiles with different details and variations.
    private readonly MTexture[,,] outerEdges = new MTexture[3, 3, 3];
    private readonly MTexture[,,] wallEdges = new MTexture[3, 2, 3];
    // This one has no variation, always used 4 times, or never.
    private readonly MTexture[,] innerCorners = new MTexture[2, 2];
    // Center tile doesn't need to be stored in a structured 2d array, but it has 8 variations.
    private readonly MTexture[] centerTiles = new MTexture[8];

    private Vector2 start;
    private Vector2 speed;

    private MTexture[,] tiles;
    private Vector2 scale = Vector2.One;
    private Color color;

    private ParticleType particleType;

    private Level level;

    private bool waiting = true;
    private bool canRumble;
    private bool returning, returningDash;
    private bool dashed, scaledSpikes;

    private float respawnTimer;
    private float targetSpeedX;
    private float dashedDirX;

    private readonly int edgeThickness;

    private const float SIDEBOOST_SPEED = 310f; // decided by mapper
    private const string DEFAULT_TEXTURE = "objects/CommunalHelper/strawberryJam/loopBlock/tiles"; // original sj texture

    public LoopBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Int("edgeThickness", 1), data.HexColor("color"), data.Attr("texture", DEFAULT_TEXTURE))
    { }

    public LoopBlock(Vector2 position, int width, int height, int edgeThickness, Color color, string texture = DEFAULT_TEXTURE)
        : base(position, width, height, false)
    {
        Depth = Depths.FGTerrain + 1;
        SurfaceSoundIndex = SurfaceIndex.Snow;

        start = position;

        int minEdgeSize = Math.Min(width, height) / 8;
        this.edgeThickness = Calc.Clamp(edgeThickness, 1, (int) ((minEdgeSize - 1) / 2f));
        this.color = color;

        particleType = new(Cloud.P_Cloud)
        {
            Color = color
        };

        OnDashCollide = OnDashed;

        InitializeTextures(texture);
        SetupTiles();
    }

    private void SetupTiles()
    {
        int w = (int) (Width / 8f);
        int h = (int) (Height / 8f);

        tiles = new MTexture[w, h];
        VirtualMap<bool> tileMap = new(w, h);

        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                tileMap[i, j] = i < edgeThickness || i >= w - edgeThickness || j < edgeThickness || j >= h - edgeThickness;

        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                if (!tileMap[i, j])
                    continue;

                int index = Calc.Random.Next(3);

                bool left = tileMap[i - 1, j];
                bool right = tileMap[i + 1, j];
                bool up = tileMap[i, j - 1];
                bool down = tileMap[i, j + 1];
                bool upleft = tileMap[i - 1, j - 1];
                bool upright = tileMap[i + 1, j - 1];
                bool downleft = tileMap[i - 1, j + 1];
                bool downright = tileMap[i + 1, j + 1];

                bool innerEdge = left && right && up && down;
                bool filler = innerEdge && upleft && upright && downleft && downright;

                MTexture texture = null;
                if (filler)
                    texture = centerTiles[Calc.Random.Next(8)];
                else if (innerEdge)
                {
                    if (!downright)
                        texture = innerCorners[0, 0];
                    else if (!downleft)
                        texture = innerCorners[1, 0];
                    else if (!upright)
                        texture = innerCorners[0, 1];
                    else if (!upleft)
                        texture = innerCorners[1, 1];
                }
                else
                {
                    if (!up && down && left && right)
                        texture = outerEdges[1, 0, index];
                    else if (up && !down && left && right)
                        texture = outerEdges[1, 2, index];
                    else if (up && down && !left && right)
                        texture = outerEdges[0, 1, index];
                    else if (up && down && left && !right)
                        texture = outerEdges[2, 1, index];
                    else if (right && down)
                        texture = (downright ? outerEdges[0, 0, index] : wallEdges[0, 0, index]);
                    else if (left && down)
                        texture = (downleft ? outerEdges[2, 0, index] : wallEdges[1, 0, index]);
                    else if (right && up)
                        texture = (upright ? outerEdges[0, 2, index] : wallEdges[0, 1, index]);
                    else if (left && up)
                        texture = (upleft ? outerEdges[2, 2, index] : wallEdges[1, 1, index]);
                    else if (left && right && !up && !down)
                        texture = wallEdges[2, 0, index];
                    else if (!left && !right && up && down)
                        texture = wallEdges[2, 1, index];
                }

                if (texture is not null)
                    tiles[i, j] = texture;
            }
        }
    }

    private DashCollisionResults OnDashed(Player player, Vector2 dir)
    {
        if (dir.Y == 0)
        {
            dashedDirX = dir.X;

            // Math.Abs(dashedDirX) is always 1.
            scale = new Vector2(0.6f, 1.4f);

            int dashes = player.Dashes;
            float stamina = player.Stamina;

            // We'll rescale spikes here instead, and it won't be done in Update.
            // Because of the Celeste.Freeze call in Player.ExplodeLaunch,
            // they would remain unscaled during the freeze time, which looked a bit weird.
            RescaleSpikes();
            player.ExplodeLaunch(new Vector2(Center.X, player.Center.Y), false, true);

            player.Speed.X = -SIDEBOOST_SPEED * dir.X;

            player.Dashes = dashes;
            player.Stamina = stamina;

            level.DirectionalShake(Vector2.UnitX, 0.2f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);

            Audio.Play(CustomSFX.game_strawberryJam_loop_block_sideboost, Center);

            speed.X = dir.X * 180f;
            targetSpeedX = -dir.X * 90f;

            dashed = true;
        }
        else if (dir.Y == -1)
        {
            // Only has a visual purpose, not an used mechanic.
            scale = new Vector2(1.4f, 0.8f);

            waiting = false;
            canRumble = true;
            speed.Y = -90f;

            return DashCollisionResults.Bounce;
        }
        return DashCollisionResults.NormalCollision;
    }

    private void RescaleSpikes()
    {
        if (scaledSpikes)
            return;

        foreach (StaticMover staticMover in staticMovers)
        {
            if (staticMover.Entity is Spikes spikes)
            {
                spikes.SetOrigins(Center);
                foreach (Component component in spikes.Components)
                    if (component is Image image)
                        image.Scale = scale;
            }
        }

        scaledSpikes = true;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        level = scene as Level;
    }

    public override void Update()
    {
        base.Update();

        scale = Calc.Approach(scale, Vector2.One, 3f * Engine.DeltaTime);
        if (!scaledSpikes)
            RescaleSpikes();

        scaledSpikes = false;

        if (respawnTimer > 0f)
        {
            respawnTimer -= Engine.DeltaTime;
            if (respawnTimer <= 0f)
            {
                waiting = true;
                Y = start.Y;
                speed.Y = 0f;
                Collidable = true;
            }
            return;
        }

        if (dashed)
        {
            if (returningDash)
            {
                speed.X = Calc.Approach(speed.X, -targetSpeedX * 0.75f, 600f * Engine.DeltaTime);
                MoveH(speed.X * Engine.DeltaTime);
                if ((dashedDirX < 0 && X <= start.X) || (dashedDirX > 0 && X >= start.X))
                {
                    returningDash = dashed = false;
                    MoveToX(start.X);
                    speed.X = 0f;
                }
            }
            else
            {
                speed.X = Calc.Approach(speed.X, targetSpeedX, 1200f * Engine.DeltaTime);
                MoveH(speed.X * Engine.DeltaTime);
                if (speed.X == targetSpeedX && ((dashedDirX < 0 && X > start.X) || (dashedDirX > 0 && X < start.X)))
                    returningDash = true;
            }
        }

        if (waiting)
        {
            Player playerRider = GetPlayerRider();
            if (playerRider != null && playerRider.StateMachine.State != Player.StCassetteFly)
            {
                canRumble = true;
                speed.Y = 180f;
                waiting = false;
                Audio.Play(SFX.game_04_cloud_blue_boost, Center);
            }
            return;
        }

        if (returning)
        {
            speed.Y = Calc.Approach(speed.Y, 180f, 600f * Engine.DeltaTime);

            // Acts like Platform.MoveTowardsY, but with custom liftspeed.
            // Essentially makes it possible to get the Y boost in a larger window of time.
            MoveToY(Calc.Approach(ExactPosition.Y, start.Y, speed.Y * Engine.DeltaTime), speed.Y < 0f ? -220f : speed.Y);

            if (ExactPosition.Y == start.Y)
            {
                returning = false;
                waiting = true;
                speed.Y = 0f;
            }
            return;
        }

        if (speed.Y < 0f && canRumble)
        {
            canRumble = false;
            if (HasPlayerRider())
            {
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            }
        }

        if (speed.Y < 0f && Scene.OnInterval(0.02f))
            level.ParticlesBG.Emit(particleType, 1, Position + new Vector2(Width / 2, Height), new Vector2(Width / 2f, 1f), (float) Math.PI / 2f);

        if (Y >= start.Y)
            speed.Y -= 1200f * Engine.DeltaTime;
        else
        {
            speed.Y += 1200f * Engine.DeltaTime;
            if (speed.Y >= -100f)
            {
                Player playerRider = GetPlayerRider();
                if (playerRider != null && playerRider.Speed.Y >= 0f && !HasPlayerClimbing())
                    playerRider.Speed.Y = -200f;
                returning = true;
            }
        }

        MoveV(speed.Y * Engine.DeltaTime, speed.Y < 0f ? -220f : speed.Y);
    }

    public override void Render()
    {
        base.Render();

        int w = (int) (Width / 8f);
        int h = (int) (Height / 8f);

        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                MTexture tile = tiles[i, j];
                if (tile != null)
                {
                    Vector2 pos = Center + (new Vector2(X + i * 8 + 4, Y + j * 8 + 4) - Center) * scale;
                    tile.DrawCentered(pos, color, scale);
                }
            }
        }
    }

    public void InitializeTextures(string texture)
    {
        MTexture tiles = GFX.Game[texture];
        for (int i = 0; i < 3; i++)
        {
            int tx = i * 8;

            outerEdges[0, 0, i] = tiles.GetSubtexture(tx, 0, 8, 8); // outer top left
            outerEdges[2, 0, i] = tiles.GetSubtexture(24 + tx, 0, 8, 8); // outer top right
            outerEdges[0, 2, i] = tiles.GetSubtexture(tx, 8, 8, 8); // outer bottom left
            outerEdges[2, 2, i] = tiles.GetSubtexture(24 + tx, 8, 8, 8); // outer bottom right
            outerEdges[1, 0, i] = tiles.GetSubtexture(tx, 16, 8, 8); // outer top
            outerEdges[1, 2, i] = tiles.GetSubtexture(tx, 24, 8, 8); // outer bottom
            outerEdges[0, 1, i] = tiles.GetSubtexture(24 + tx, 16, 8, 8); // outer left
            outerEdges[2, 1, i] = tiles.GetSubtexture(24 + tx, 24, 8, 8); // outer right

            wallEdges[0, 0, i] = tiles.GetSubtexture(tx, 32, 8, 8); // outer inner top left
            wallEdges[1, 0, i] = tiles.GetSubtexture(24 + tx, 32, 8, 8); // outer inner top right
            wallEdges[0, 1, i] = tiles.GetSubtexture(tx, 40, 8, 8); // outer inner bottom left
            wallEdges[1, 1, i] = tiles.GetSubtexture(24 + tx, 40, 8, 8); // outer inner bottom right
            wallEdges[2, 0, i] = tiles.GetSubtexture(tx, 48, 8, 8); // outer inner horizontal
            wallEdges[2, 1, i] = tiles.GetSubtexture(24 + tx, 48, 8, 8); // outer inner vertical

            if (i > 0)
            {
                centerTiles[i - 1] = tiles.GetSubtexture(tx, 56, 8, 8); // center 0, 1
                centerTiles[i + 1] = tiles.GetSubtexture(24 + tx, 56, 8, 8); // center 2, 3
                centerTiles[i + 3] = tiles.GetSubtexture(tx, 64, 8, 8); // center 4, 5
                centerTiles[i + 5] = tiles.GetSubtexture(24 + tx, 64, 8, 8); // center 6, 7
            }
        }

        innerCorners[0, 0] = tiles.GetSubtexture(0, 56, 8, 8); // inner top left
        innerCorners[1, 0] = tiles.GetSubtexture(24, 56, 8, 8); // inner top right
        innerCorners[0, 1] = tiles.GetSubtexture(0, 64, 8, 8); // inner bottom left
        innerCorners[1, 1] = tiles.GetSubtexture(24, 64, 8, 8); // inner bottom right
    }
}
