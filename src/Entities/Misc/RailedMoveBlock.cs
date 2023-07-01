using MonoMod.Utils;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/RailedMoveBlock")]
internal class RailedMoveBlock : Solid
{
    private class RailedMoveBlockPathRenderer : Entity
    {
        public RailedMoveBlock block;

        private readonly MTexture cog;

        private Vector2 from;
        private Vector2 to;
        private Vector2 sparkAdd;

        private readonly float sparkDirFromA;
        private readonly float sparkDirFromB;
        private readonly float sparkDirToA;
        private readonly float sparkDirToB;

        public RailedMoveBlockPathRenderer(RailedMoveBlock zipMover)
        {
            base.Depth = 5000;
            block = zipMover;

            from = block.start + new Vector2(block.Width / 2f, block.Height / 2f);
            to = block.target + new Vector2(block.Width / 2f, block.Height / 2f);

            sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();

            float num = (from - to).Angle();
            sparkDirFromA = num + ((float) Math.PI / 8f);
            sparkDirFromB = num - ((float) Math.PI / 8f);
            sparkDirToA = num + (float) Math.PI - ((float) Math.PI / 8f);
            sparkDirToB = num + (float) Math.PI + ((float) Math.PI / 8f);

            cog = GFX.Game["objects/zipmover/cog"];
        }

        public void CreateSparks()
        {
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
        }

        public override void Render()
        {
            DrawCogs(Vector2.UnitY, Color.Black);
            DrawCogs(Vector2.Zero);
        }

        private void DrawCogs(Vector2 offset, Color? colorOverride = null)
        {
            Vector2 vector = (to - from).SafeNormalize();
            Vector2 value = vector.Perpendicular() * 3f;
            Vector2 value2 = -vector.Perpendicular() * 4f;

            float rotation = block.percent * (float) Math.PI * 2f;

            Draw.Line(from + value + offset, to + value + offset, colorOverride ?? block.fillColor);
            Draw.Line(from + value2 + offset, to + value2 + offset, colorOverride ?? block.fillColor);

            Color highlightColor = Color.Lerp(block.fillColor, Color.White, 0.18f);

            for (float num = 4f - (block.percent * (float) Math.PI * 8f % 4f); num < (to - from).Length(); num += 4f)
            {
                Vector2 value3 = from + value + vector.Perpendicular() + (vector * num);
                Vector2 value4 = to + value2 - (vector * num);

                Draw.Line(value3 + offset, value3 + (vector * 2f) + offset, colorOverride ?? highlightColor);
                Draw.Line(value4 + offset, value4 - (vector * 2f) + offset, colorOverride ?? highlightColor);
            }
            cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
            cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
        }
    }
    private RailedMoveBlockPathRenderer pathRenderer;

    private class Border : Entity
    {
        public RailedMoveBlock Parent;

        public Border(RailedMoveBlock parent)
        {
            Parent = parent;
            base.Depth = 1;
        }

        public override void Update()
        {
            if (Parent.Scene != base.Scene)
            {
                RemoveSelf();
            }
            base.Update();
        }

        public override void Render()
        {
            Draw.Rect(Parent.X + Parent.Shake.X - 1f, Parent.Y + Parent.Shake.Y - 1f, Parent.Width + 2f, Parent.Height + 2f, Color.Black);
        }
    }
    private Border border;

    public enum SteeringMode
    {
        Horizontal, Vertical, Both, None
    }
    private readonly SteeringMode steeringMode;

    private readonly List<Image> body = new();

    private readonly List<Image> topButton = new();
    private readonly List<Image> leftButton = new();
    private readonly List<Image> rightButton = new();
    private bool leftPressed, rightPressed, topPressed, bottomPressed;

    private Color fillColor = IdleBgFill;
    public static readonly Color IdleBgFill = Calc.HexToColor("474070");
    public static readonly Color MoveBgFill = Calc.HexToColor("30b335");
    public static readonly Color StopBgFill = Calc.HexToColor("cc2541");
    public static readonly Color PlacementErrorBgFill = Calc.HexToColor("cc7c27");

    private readonly DynamicData platformData;
    private Vector2 start, target, dir;
    private float percent;
    private readonly float length;
    private readonly bool hasTopButtons, hasSideButtons;
    private float speed;

    public static MTexture XIcon, LeftIcon, RightIcon, UpIcon, DownIcon;
    private readonly MTexture idleIcon;

    private MTexture icon;
    private bool showX;

    private readonly SoundSource sfx;

    private readonly float moveSpeed;

    public RailedMoveBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.NodesOffset(offset)[0], data.Enum("steeringMode", SteeringMode.Both), data.Float("speed", 120f)) { }

    public RailedMoveBlock(Vector2 position, int width, int height, Vector2 node, SteeringMode steeringMode, float speed)
        : base(position, width, height, safe: false)
    {
        start = position;
        target = node;

        if (speed <= 0f)
            steeringMode = SteeringMode.None;
        else
            moveSpeed = speed;

        dir = Calc.SafeNormalize(target - start);
        length = Vector2.Distance(start, target);

        if (dir == Vector2.Zero)
            steeringMode = SteeringMode.None;
        else
        {
            if (dir.Y == 0 & (steeringMode == SteeringMode.Both || steeringMode == SteeringMode.Vertical))
                steeringMode = SteeringMode.Horizontal;
            if (dir.X == 0 & (steeringMode == SteeringMode.Both || steeringMode == SteeringMode.Horizontal))
                steeringMode = SteeringMode.Vertical;
        }
        this.steeringMode = steeringMode;
        hasSideButtons = steeringMode is SteeringMode.Both or SteeringMode.Horizontal;
        hasTopButtons = steeringMode is SteeringMode.Both or SteeringMode.Vertical;

        int tilesWidth = width / 8;
        int tilesHeight = height / 8;

        MTexture button = GFX.Game["objects/moveBlock/button"];
        MTexture block;
        switch (steeringMode)
        {
            default:
                block = GFX.Game["objects/moveBlock/base"];
                idleIcon = GFX.Game["objects/CommunalHelper/railedMoveBlock/o"];
                break;
            case SteeringMode.Both:
                block = GFX.Game["objects/CommunalHelper/railedMoveBlock/base_both"];
                idleIcon = GFX.Game["objects/CommunalHelper/railedMoveBlock/both"];
                break;
            case SteeringMode.Vertical:
                block = GFX.Game["objects/moveBlock/base_h"];
                idleIcon = GFX.Game["objects/CommunalHelper/railedMoveBlock/v"];
                break;
            case SteeringMode.Horizontal:
                block = GFX.Game["objects/moveBlock/base_v"];
                idleIcon = GFX.Game["objects/CommunalHelper/railedMoveBlock/h"];
                break;
        }
        icon = idleIcon;

        if (hasTopButtons)
        {
            for (int i = 0; i < tilesWidth; i++)
            {
                int num3 = (i != 0) ? ((i < tilesWidth - 1) ? 1 : 2) : 0;
                AddImage(button.GetSubtexture(num3 * 8, 0, 8, 8), new Vector2(i * 8, -4f), 0f, new Vector2(1f, 1f), topButton);
            }
        }

        if (hasSideButtons)
        {
            for (int j = 0; j < tilesHeight; j++)
            {
                int num4 = (j != 0) ? ((j < tilesHeight - 1) ? 1 : 2) : 0;
                AddImage(button.GetSubtexture(num4 * 8, 0, 8, 8), new Vector2(-4f, j * 8), (float) Math.PI / 2f, new Vector2(1f, -1f), leftButton);
                AddImage(button.GetSubtexture(num4 * 8, 0, 8, 8), new Vector2(((tilesWidth - 1) * 8) + 4, j * 8), (float) Math.PI / 2f, new Vector2(1f, 1f), rightButton);
            }
        }

        for (int i = 0; i < tilesWidth; i++)
        {
            for (int j = 0; j < tilesHeight; j++)
            {
                int tx = (i != 0) ? ((i < tilesWidth - 1) ? 1 : 2) : 0;
                int ty = (j != 0) ? ((j < tilesHeight - 1) ? 1 : 2) : 0;
                AddImage(block.GetSubtexture(tx * 8, ty * 8, 8, 8), new Vector2(i, j) * 8f, 0f, new Vector2(1f, 1f), body);
            }
        }

        UpdateColors(fillColor);
        Add(sfx = new SoundSource()
        {
            Position = new Vector2(width / 2f, Height / 2f)
        });
        sfx.Play(CustomSFX.game_railedMoveBlock_railedmoveblock_move, "arrow_stop", 1f);
        Add(new LightOcclude(0.5f));

        platformData = new(typeof(Platform), this);
    }

    private void AddImage(MTexture tex, Vector2 position, float rotation, Vector2 scale, List<Image> addTo)
    {
        Image image = new(tex)
        {
            Position = position + new Vector2(4f, 4f)
        };
        image.CenterOrigin();
        image.Rotation = rotation;
        image.Scale = scale;
        Add(image);
        addTo?.Add(image);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        scene.Add(border = new Border(this));
        scene.Add(pathRenderer = new RailedMoveBlockPathRenderer(this));
    }

    public override void Removed(Scene scene)
    {
        scene.Remove(pathRenderer);
        pathRenderer = null;

        scene.Remove(border);
        border = null;

        base.Removed(scene);
    }

    public override void Update()
    {
        base.Update();

        bool leftCheck = hasSideButtons && CollideCheck<Player>(Position - Vector2.UnitX);
        bool rightCheck = hasSideButtons && CollideCheck<Player>(Position + Vector2.UnitX);
        bool topCheck = hasTopButtons && CollideCheck<Player>(Position - Vector2.UnitY);
        bool bottomCheck = hasTopButtons && CollideCheck<Player>(Position + Vector2.UnitY);

        foreach (Image image in topButton)
        {
            image.Y = topCheck ? 2 : 0;
        }
        foreach (Image image in leftButton)
        {
            image.X = leftCheck ? 2 : 0;
        }
        foreach (Image image in rightButton)
        {
            image.X = Width + (rightCheck ? (-2) : 0);
        }

        if ((leftCheck && !leftPressed) || (topCheck && !topPressed) || (rightCheck && !rightPressed) || (bottomCheck && !bottomPressed))
        {
            Audio.Play(SFX.game_04_arrowblock_side_depress, Position);
        }
        if ((!leftCheck && leftPressed) || (!topCheck && topPressed) || (!rightCheck && rightPressed) || (!bottomCheck && bottomPressed))
        {
            Audio.Play(SFX.game_04_arrowblock_side_release, Position);
        }

        leftPressed = leftCheck;
        rightPressed = rightCheck;
        topPressed = topCheck;
        bottomPressed = bottomCheck;

        Color newFillColor = IdleBgFill;
        showX = false;

        if (dir != Vector2.Zero)
        {
            float newSpeed = 0f;

            icon = idleIcon;
            if ((topPressed || bottomPressed) && HasPlayerRider() && Input.MoveY.Value != 0)
            {
                newSpeed = moveSpeed * Input.MoveY.Value * (dir.Y > 0f ? 1f : -1f);
                newFillColor = MoveBgFill;
                icon = Input.MoveY.Value == 1 ? DownIcon : UpIcon;
            }
            else if ((leftPressed || rightPressed) && HasPlayerClimbing() && Input.MoveX.Value != 0)
            {
                newSpeed = moveSpeed * Input.MoveX.Value * (dir.X > 0f ? 1f : -1f);
                newFillColor = MoveBgFill;
                icon = Input.MoveX.Value == 1 ? RightIcon : LeftIcon;
            }

            if (Math.Sign(speed) != Math.Sign(newSpeed))
            {
                if (Scene.OnInterval(0.05f))
                    SceneAs<Level>().Particles.Emit(ZipMover.P_Scrape, 2, Center - (dir * speed * Engine.DeltaTime * 4), Vector2.One * 2);
            }
            speed = Calc.Approach(speed, newSpeed, 300f * Engine.DeltaTime);

            if ((newSpeed > 0f && percent >= 1f) || (newSpeed < 0f && percent <= 0f))
            {
                showX = true;
                newFillColor = StopBgFill;
                speed = 0f;
            }
            else if (speed != 0f)
            {
                if (Scene.OnInterval(0.25f))
                    pathRenderer.CreateSparks();
            }

            if (newFillColor != StopBgFill)
            {
                Vector2 lift = dir * speed;
                Vector2 move = lift * Engine.DeltaTime;

                Vector2 position = Position;
                position += move;

                percent = Vector2.Distance(start, position) / length;

                bool impact = false;
                if (percent > 1f)
                {
                    impact = speed > 60f;
                    move += target - position;
                    speed = 0f;
                    percent = 1f;
                    newFillColor = StopBgFill;
                }
                else if (Vector2.Distance(Position, target) > length)
                {
                    impact = speed < -60f;
                    move += start - position;
                    speed = 0f;
                    percent = 0f;
                    newFillColor = StopBgFill;
                }

                MoveH(move.X, lift.X);
                MoveV(move.Y, lift.Y);

                if (impact)
                {
                    SceneAs<Level>().DirectionalShake(dir, 0.2f);
                    StartShaking(0.1f);
                    Audio.Play(CustomSFX.game_railedMoveBlock_railedmoveblock_impact, Center);
                }
            }

            sfx.Param("arrow_stop", 1 - Math.Abs(speed / (moveSpeed / 2f)));
        }

        UpdateColors(steeringMode == SteeringMode.None ? PlacementErrorBgFill : newFillColor);
    }

    private void UpdateColors(Color color)
    {
        fillColor = Color.Lerp(fillColor, color, 10f * Engine.DeltaTime);

        foreach (Image image in topButton)
        {
            image.Color = fillColor;
        }
        foreach (Image image in leftButton)
        {
            image.Color = fillColor;
        }
        foreach (Image image in rightButton)
        {
            image.Color = fillColor;
        }
    }

    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;

        foreach (Image image in leftButton)
        {
            image.Render();
        }
        foreach (Image image in rightButton)
        {
            image.Render();
        }
        foreach (Image image in topButton)
        {
            image.Render();
        }

        Draw.Rect(X + 3f, Y + 3f, Width - 6f, Height - 6f, fillColor);
        foreach (Image tile in body)
        {
            tile.Render();
        }
        Draw.Rect(Center.X - 4f, Center.Y - 4f, 8f, 8f, fillColor);
        (showX ? XIcon : icon).DrawCentered(Center);

        Position = position;
    }

    public static void InitializeTextures()
    {
        UpIcon = GFX.Game["objects/CommunalHelper/railedMoveBlock/up"];
        DownIcon = GFX.Game["objects/CommunalHelper/railedMoveBlock/down"];
        RightIcon = GFX.Game["objects/CommunalHelper/railedMoveBlock/right"];
        LeftIcon = GFX.Game["objects/CommunalHelper/railedMoveBlock/left"];
        XIcon = GFX.Game["objects/moveBlock/x"];
    }
}
