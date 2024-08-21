using FMOD.Studio;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DreamSwapBlock")]
public class DreamSwapBlock : CustomDreamBlock
{
    private class PathRenderer : Entity
    {
        private readonly DreamSwapBlock block;

        private float timer = 0f;

        public PathRenderer(DreamSwapBlock block)
            : base(block.Position)
        {
            this.block = block;
            Depth = Depths.BGDecals - 1;
            timer = Calc.Random.NextFloat();
        }

        public override void Update()
        {
            base.Update();
            timer += Engine.DeltaTime * 4f;
        }

        public override void Render()
        {
            float scale = 0.5f * (0.5f + (((float) Math.Sin(timer) + 1f) * 0.25f));
            scale = Calc.LerpClamp(scale, 1, block.ColorLerp);
            Util.DrawBlockStyle(SceneAs<Level>().Camera, new Vector2(block.moveRect.X, block.moveRect.Y), block.moveRect.Width, block.moveRect.Height, block.nineSliceTarget, null, ActiveLineColor * scale);
        }
    }

    private const float ReturnTime = 0.8f;

    public Vector2 Direction;
    public bool Swapping;

    private Vector2 start;
    private Vector2 end;
    private float lerp;
    private int target;
    private Rectangle moveRect;

    private float speed;
    private readonly float maxForwardSpeed;
    private readonly float maxBackwardSpeed;
    private float returnTimer;

    private readonly MTexture[,] nineSliceTarget;

    private PathRenderer path;

    private EventInstance moveSfx;
    private EventInstance returnSfx;

    private DisplacementRenderer.Burst burst;
    private float particlesRemainder;

    private static readonly ParticleType[] dreamParticles;
    private int particleIndex = 0;

    private readonly bool noReturn;
    private readonly MTexture cross;
    private bool shattered = false;

    static DreamSwapBlock()
    {
        ParticleType particle = new(SwapBlock.P_Move)
        {
            ColorMode = ParticleType.ColorModes.Choose,
            FadeMode = ParticleType.FadeModes.Late,
            LifeMin = 0.6f
        };
        particle.LifeMin = 1f;

        dreamParticles = new ParticleType[4];
        for (int i = 0; i < 4; i++)
        {
            dreamParticles[i] = new ParticleType(particle);
        }
    }

    public DreamSwapBlock(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        start = Position;
        end = data.Nodes[0] + offset;
        noReturn = data.Bool("noReturn", false);

        maxForwardSpeed = 360f / Vector2.Distance(start, end);
        maxBackwardSpeed = maxForwardSpeed * 0.4f;
        Direction.X = Math.Sign(end.X - start.X);
        Direction.Y = Math.Sign(end.Y - start.Y);
        Add(new DashListener { OnDash = OnDash });

        int left = (int) MathHelper.Min(X, end.X);
        int top = (int) MathHelper.Min(Y, end.Y);
        int right = (int) MathHelper.Max(X + Width, end.X + Width);
        int bottom = (int) MathHelper.Max(Y + Height, end.Y + Height);
        moveRect = new Rectangle(left, top, right - left, bottom - top);

        MTexture targetTexture = GFX.Game["objects/swapblock/target"];
        nineSliceTarget = new MTexture[3, 3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                nineSliceTarget[i, j] = targetTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
            }
        }

        Add(new LightOcclude(0.2f));
        cross = GFX.Game["objects/CommunalHelper/dreamMoveBlock/x"];
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        scene.Add(path = new PathRenderer(this));
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        Audio.Stop(moveSfx);
        Audio.Stop(returnSfx);
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);
        Audio.Stop(moveSfx);
        Audio.Stop(returnSfx);
    }

    private void OnDash(Vector2 direction)
    {
        if (noReturn)
        {
            Swapping = true;
            target = 1 - target;
            burst = (Scene as Level).Displacement.AddBurst(Center, 0.2f, 0f, 16f);
            float relativeLerp = target == 1 ? lerp : 1 - lerp;
            speed = relativeLerp >= 0.2f ? maxForwardSpeed : MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, relativeLerp / 0.2f);
            Audio.Stop(moveSfx);
            moveSfx = Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamSwapBlock_dream_swap_block_move : SFX.game_05_swapblock_move, Center);
        }
        else
        {
            Swapping = lerp < 1f;
            target = 1;
            returnTimer = ReturnTime;
            burst = (Scene as Level).Displacement.AddBurst(Center, 0.2f, 0f, 16f);
            speed = lerp >= 0.2f ? maxForwardSpeed : MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
            Audio.Stop(returnSfx);
            Audio.Stop(moveSfx);
            if (!Swapping)
            {
                Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamSwapBlock_dream_swap_block_move_end : SFX.game_05_swapblock_move_end, Center);
            }
            else
            {
                moveSfx = Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamSwapBlock_dream_swap_block_move : SFX.game_05_swapblock_move, Center);
            }
        }
    }

    protected override void OneUseDestroy()
    {
        base.OneUseDestroy();
        Audio.Stop(moveSfx);
        Audio.Stop(returnSfx);
        Scene.Remove(path);
        shattered = true;
    }

    public override void Update()
    {
        base.Update();
        if (shattered)
        {
            return;
        }

        if (burst != null)
        {
            burst.Position = Center;
        }

        if (noReturn)
        {
            speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
            float num = lerp;
            lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
            if (lerp is 0 or 1)
                Audio.Stop(moveSfx);
            if (lerp != num)
            {
                Vector2 liftSpeed = (end - start) * speed;
                Vector2 position = Position;
                if (target == 1)
                {
                    liftSpeed = (end - start) * maxForwardSpeed;
                }
                if (lerp < num)
                {
                    liftSpeed *= -1f;
                }
                if (Scene.OnInterval(0.02f))
                {
                    // Allows move particles in both directions
                    MoveParticles((end - start) * (target - 0.5f) * 2);
                }
                MoveTo(Vector2.Lerp(start, end, lerp), liftSpeed);
                if (position != Position)
                {
                    Audio.Position(moveSfx, Center);
                    if (Position == start || Position == end)
                    {
                        //Audio.Play("event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_return_end", base.Center);
                        Audio.Stop(moveSfx);
                        Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamSwapBlock_dream_swap_block_move_end : SFX.game_05_swapblock_move_end, Center);
                    }
                }
            }
            if (Swapping && lerp >= 1f)
            {
                Swapping = false;
            }
            StopPlayerRunIntoAnimation = lerp is <= 0f or >= 1f;
        }
        else
        {
            if (returnTimer > 0f)
            {
                returnTimer -= Engine.DeltaTime;
                if (returnTimer <= 0f)
                {
                    target = 0;
                    speed = 0f;
                    returnSfx = Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamSwapBlock_dream_swap_block_return : SFX.game_05_swapblock_return, Center);
                }
            }
            speed = target == 1
                ? Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime)
                : Calc.Approach(speed, maxBackwardSpeed, maxBackwardSpeed / 1.5f * Engine.DeltaTime);
            float num = lerp;
            lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
            if (lerp == 1)
                Audio.Stop(moveSfx);
            if (lerp != num)
            {
                Vector2 liftSpeed = (end - start) * speed;
                Vector2 position = Position;
                if (target == 1)
                {
                    liftSpeed = (end - start) * maxForwardSpeed;
                }
                if (lerp < num)
                {
                    liftSpeed *= -1f;
                }
                if (target == 1 && Scene.OnInterval(0.02f))
                {
                    MoveParticles(end - start);
                }
                MoveTo(Vector2.Lerp(start, end, lerp), liftSpeed);
                if (position != Position)
                {
                    Audio.Position(moveSfx, Center);
                    Audio.Position(returnSfx, Center);
                    if (Position == start && target == 0)
                    {
                        Audio.SetParameter(returnSfx, "end", 1f);
                        Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamSwapBlock_dream_swap_block_return_end : SFX.game_05_swapblock_return_end, Center);
                    }
                    else if (Position == end && target == 1)
                    {
                        Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamSwapBlock_dream_swap_block_move_end : SFX.game_05_swapblock_move_end, Center);
                        Audio.Stop(moveSfx);
                    }
                }
            }
            if (Swapping && lerp >= 1f)
            {
                Swapping = false;
            }
            StopPlayerRunIntoAnimation = lerp is <= 0f or >= 1f;
        }
    }

    public override void Render()
    {
        base.Render();
        if (noReturn)
        {
            cross.DrawCentered(Center + baseData.Get<Vector2>("shake"));
        }
    }

    public override void SetupCustomParticles(float canvasWidth, float canvasHeight)
    {
        base.SetupCustomParticles(canvasWidth, canvasHeight);
        if (PlayerHasDreamDash)
        {
            dreamParticles[0].Color = Calc.HexToColor("FFEF11");
            dreamParticles[0].Color2 = Calc.HexToColor("FF00D0");

            dreamParticles[1].Color = Calc.HexToColor("08a310");
            dreamParticles[1].Color2 = Calc.HexToColor("5fcde4");

            dreamParticles[2].Color = Calc.HexToColor("7fb25e");
            dreamParticles[2].Color2 = Calc.HexToColor("E0564C");

            dreamParticles[3].Color = Calc.HexToColor("5b6ee1");
            dreamParticles[3].Color2 = Calc.HexToColor("CC3B3B");
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                dreamParticles[i].Color = Color.LightGray * 0.5f;
                dreamParticles[i].Color2 = Color.LightGray * 0.75f;
            }
        }
    }

    private void MoveParticles(Vector2 normal)
    {
        Vector2 position;
        Vector2 positionRange;
        float direction;
        float num;
        if (normal.X > 0f)
        {
            position = CenterLeft;
            positionRange = Vector2.UnitY * (Height - 6f);
            direction = (float) Math.PI;
            num = Math.Max(2f, Height / 14f);
        }
        else if (normal.X < 0f)
        {
            position = CenterRight;
            positionRange = Vector2.UnitY * (Height - 6f);
            direction = 0f;
            num = Math.Max(2f, Height / 14f);
        }
        else if (normal.Y > 0f)
        {
            position = TopCenter;
            positionRange = Vector2.UnitX * (Width - 6f);
            direction = -(float) Math.PI / 2f;
            num = Math.Max(2f, Width / 14f);
        }
        else
        {
            position = BottomCenter;
            positionRange = Vector2.UnitX * (Width - 6f);
            direction = (float) Math.PI / 2f;
            num = Math.Max(2f, Width / 14f);
        }

        particlesRemainder += num;
        int amount = (int) particlesRemainder;
        particlesRemainder -= amount;
        positionRange *= 0.5f;
        SceneAs<Level>().Particles.Emit(dreamParticles[particleIndex], amount, position, positionRange, direction);
        ++particleIndex;
        particleIndex %= 4;
    }
}
