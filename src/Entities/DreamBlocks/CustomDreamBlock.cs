using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.CommunalHelper.Entities;

[TrackedAs(typeof(DreamBlock), true)]
public abstract class CustomDreamBlock : DreamBlock
{
    /*
     * We want to tie the Custom DreamParticles to the vanilla DreamParticles, but since DreamBlock.DreamParticle is private it would require a lot of reflection.
     * Instead we just IL hook stuff and ignore accessibility modifiers entirely. It's fine.
     */
    protected struct DreamParticle
    {
        internal static Type t_DreamParticle = typeof(DreamBlock).GetNestedType("DreamParticle", BindingFlags.NonPublic);

#pragma warning disable IDE0052, CS0414, CS0649 // Remove unread private members; Field is assigned to but never read; Field is never assigned to
        // Used in IL hooks
        private readonly DreamBlock dreamBlock;
        private readonly int idx;
        private static Vector2 tempVec2;
#pragma warning restore IDE0052, CS0414, CS0649

        public Vector2 Position
        {
            get { UpdatePos(); return tempVec2; }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdatePos() { Console.Error.Write("NoInlining"); throw new NoInliningException(); }

        public int Layer
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
        }
        public Color Color
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
        }
        public float TimeOffset
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
        }

        // Feather particle stuff
        public float Speed;
        public float Spin;
        public float MaxRotate;
        public float RotationCounter;

        public DreamParticle(DreamBlock block, int idx)
            : this()
        {
            dreamBlock = block;
            this.idx = idx;
        }
    }

    private static readonly MethodInfo m_DreamBlock_PutInside = typeof(DreamBlock).GetMethod("PutInside", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo m_DreamBlock_WobbleLine = typeof(DreamBlock).GetMethod("WobbleLine", BindingFlags.NonPublic | BindingFlags.Instance);

    protected MTexture[] featherTextures;
    protected DreamParticle[] particles;
    protected MTexture[] doubleRefillStarTextures;

    public bool PlayerHasDreamDash => baseData.Get<bool>("playerHasDreamDash");

    public static Color ActiveLineColor => (Color) f_DreamBlock_activeLineColor.GetValue(null);
    private static readonly FieldInfo f_DreamBlock_activeLineColor = typeof(DreamBlock).GetField("activeLineColor", BindingFlags.NonPublic | BindingFlags.Static);
    public static Color DisabledLineColor => (Color) f_DreamBlock_disabledLineColor.GetValue(null);
    private static readonly FieldInfo f_DreamBlock_disabledLineColor = typeof(DreamBlock).GetField("disabledLineColor", BindingFlags.NonPublic | BindingFlags.Static);
    public static Color ActiveBackColor => (Color) f_DreamBlock_activeBackColor.GetValue(null);
    private static readonly FieldInfo f_DreamBlock_activeBackColor = typeof(DreamBlock).GetField("activeBackColor", BindingFlags.NonPublic | BindingFlags.Static);
    public static Color DisabledBackColor => (Color) f_DreamBlock_disabledBackColor.GetValue(null);
    private static readonly FieldInfo f_DreamBlock_disabledBackColor = typeof(DreamBlock).GetField("disabledBackColor", BindingFlags.NonPublic | BindingFlags.Static);

    // All dream colors in one array, independent of layer.
    public static readonly Color[] DreamColors = new Color[9] {
        Calc.HexToColor("FFEF11"), Calc.HexToColor("FF00D0"), Calc.HexToColor("08a310"),
        Calc.HexToColor("5fcde4"), Calc.HexToColor("7fb25e"), Calc.HexToColor("E0564C"),
        Calc.HexToColor("5b6ee1"), Calc.HexToColor("CC3B3B"), Calc.HexToColor("7daa64"),
    };

    public bool FeatherMode;
    protected int RefillCount;
    protected bool shattering = false;
    public float ColorLerp = 0.0f;
    public bool QuickDestroy;

    private bool shakeToggle = false;
    private readonly ParticleType shakeParticle;
    private readonly float[] particleRemainders = new float[4];

    private bool delayedSetupParticles;
    protected bool awake;

    protected DynamicData baseData;

    public CustomDreamBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Bool("featherMode"), data.Bool("oneUse"), GetRefillCount(data), data.Bool("below"), data.Bool("quickDestroy")) { }

    public CustomDreamBlock(Vector2 position, int width, int height, bool featherMode, bool oneUse, int refillCount, bool below, bool quickDestroy)
        : base(position, width, height, null, false, oneUse, below)
    {
        baseData = new(typeof(DreamBlock), this);
        QuickDestroy = quickDestroy;
        RefillCount = refillCount;

        FeatherMode = featherMode;
        //if (altLineColor) { Dropped in favour of symbol
        //    activeLineColor = Calc.HexToColor("FF66D9"); 
        //}
        shakeParticle = new ParticleType(SwitchGate.P_Behind)
        {
            Color = ActiveLineColor,
            ColorMode = ParticleType.ColorModes.Static,
            Acceleration = Vector2.Zero,
            DirectionRange = (float) Math.PI / 2
        };

        featherTextures = new MTexture[] {
            GFX.Game["particles/CommunalHelper/featherBig"],
            GFX.Game["particles/CommunalHelper/featherMedium"],
            GFX.Game["particles/CommunalHelper/featherSmall"]
        };

        doubleRefillStarTextures = new MTexture[4] {
            GFX.Game["objects/CommunalHelper/customDreamBlock/particles"].GetSubtexture(14, 0, 7, 7),
            GFX.Game["objects/CommunalHelper/customDreamBlock/particles"].GetSubtexture(7, 0, 7, 7),
            GFX.Game["objects/CommunalHelper/customDreamBlock/particles"].GetSubtexture(0, 0, 7, 7),
            GFX.Game["objects/CommunalHelper/customDreamBlock/particles"].GetSubtexture(7, 0, 7, 7)
        };
    }

    protected static int GetRefillCount(EntityData data)
    {
        return data.Bool("doubleRefill") ? 2 : data.Int("refillCount", -1);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        Glitch.Value = 0f;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        awake = true;
        if (delayedSetupParticles)
            SetupCustomParticles(Width, Height);
    }

    public virtual void SetupCustomParticles(float canvasWidth, float canvasHeight)
    {
        float countFactor = (FeatherMode ? 0.5f : 0.7f) * RefillCount != -1 ? 1.2f : 1;
        particles = new DreamParticle[(int) (canvasWidth / 8f * (canvasHeight / 8f) * 0.7f * countFactor)];
        baseData.Set("particles", Array.CreateInstance(DreamParticle.t_DreamParticle, particles.Length));

        // Necessary to get the player's spritemode
        if (!awake && RefillCount != -1)
        {
            delayedSetupParticles = true;
            return;
        }

        Color[] dashColors = new Color[3];
        if (RefillCount != -1)
        {
            dashColors[0] = Scene.Tracker.GetEntity<Player>()?.GetHairColor(RefillCount) ?? Color.White;
            dashColors[1] = Color.Lerp(dashColors[0], Color.White, 0.5f);
            dashColors[2] = Color.Lerp(dashColors[1], Color.White, 0.5f);
        }

        for (int i = 0; i < particles.Length; i++)
        {
            int layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
            particles[i] = new DreamParticle(this, i)
            {
                Position = new Vector2(Calc.Random.NextFloat(canvasWidth), Calc.Random.NextFloat(canvasHeight)),
                Layer = layer,
                Color = GetParticleColor(layer, dashColors),
                TimeOffset = Calc.Random.NextFloat()
            };

            #region Feather particle stuff

            if (FeatherMode)
            {
                particles[i].Speed = Calc.Random.Range(6f, 16f);
                particles[i].Spin = Calc.Random.Range(8f, 12f) * 0.2f;
                particles[i].RotationCounter = Calc.Random.NextAngle();
                particles[i].MaxRotate = Calc.Random.Range(0.3f, 0.6f) * ((float) Math.PI / 2f);
            }

            #endregion

        }
    }

    private Color GetParticleColor(int layer, Color[] dashColors)
    {
        return PlayerHasDreamDash
            ? RefillCount != -1
                ? dashColors[layer]
                : layer switch
                {
                    0 => Calc.Random.Choose(DreamColors[0], DreamColors[1], DreamColors[2]),
                    1 => Calc.Random.Choose(DreamColors[3], DreamColors[4], DreamColors[5]),
                    2 => Calc.Random.Choose(DreamColors[6], DreamColors[7], DreamColors[8]),
                    _ => throw new NotImplementedException()
                }
            : Color.LightGray * (0.5f + (layer / 2f * 0.5f));
    }

    private void ShakeParticles()
    {
        Vector2 position;
        Vector2 positionRange;
        float angle;
        float num2;
        for (int i = 0; i < 4; ++i)
        {
            switch (i)
            {
                case 0:
                    position = CenterLeft + Vector2.UnitX;
                    positionRange = Vector2.UnitY * (Height - 4f);
                    angle = (float) Math.PI;
                    num2 = Height / 32f;
                    break;
                case 1:
                    position = CenterRight;
                    positionRange = Vector2.UnitY * (Height - 4f);
                    angle = 0f;
                    num2 = Height / 32f;
                    break;
                case 2:
                    position = TopCenter + Vector2.UnitY;
                    positionRange = Vector2.UnitX * (Width - 4f);
                    angle = -(float) Math.PI / 2f;
                    num2 = Width / 32f;
                    break;
                default:
                    position = BottomCenter;
                    positionRange = Vector2.UnitX * (Width - 4f);
                    angle = (float) Math.PI / 2f;
                    num2 = Width / 32f;
                    break;
            }

            num2 *= 0.25f;
            particleRemainders[i] += num2;
            int amount = (int) particleRemainders[i];
            particleRemainders[i] -= amount;
            positionRange *= 0.5f;
            if (amount > 0f)
            {
                SceneAs<Level>().ParticlesBG.Emit(shakeParticle, amount, position, positionRange, angle);
            }
        }
    }

    public override void Update()
    {
        base.Update();

        if (FeatherMode)
        {
            UpdateParticles();
        }

        if (Visible && PlayerHasDreamDash && baseData.Get<bool>("oneUse") && Scene.OnInterval(0.03f))
        {
            Vector2 shake = baseData.Get<Vector2>("shake");
            if (shakeToggle)
            {
                shake.X = Calc.Random.Next(-1, 2);
            }
            else
            {
                shake.Y = Calc.Random.Next(-1, 2);
            }
            baseData.Set("shake", shake);
            shakeToggle = !shakeToggle;
            if (!shattering)
                ShakeParticles();
        }
    }

    protected virtual void UpdateParticles()
    {
        if (PlayerHasDreamDash)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                Vector2 pos = particles[i].Position;
                pos.Y += 0.5f * particles[i].Speed * GetLayerScaleFactor(particles[i].Layer) * Engine.DeltaTime;
                particles[i].Position = pos;
                particles[i].RotationCounter += particles[i].Spin * Engine.DeltaTime;
            }
        }
    }

    private float GetLayerScaleFactor(int layer)
    {
        return 1 / (0.3f + (0.25f * layer));
    }

    protected void WobbleLine(Vector2 from, Vector2 to, float offset)
    {
        m_DreamBlock_WobbleLine.Invoke(this, new object[] { from, to, offset });
    }

    public override void Render()
    {
        Camera camera = SceneAs<Level>().Camera;
        if (Right < camera.Left || Left > camera.Right || Bottom < camera.Top || Top > camera.Bottom)
        {
            return;
        }

        Vector2 shake = baseData.Get<Vector2>("shake");

        float whiteFill = baseData.Get<float>("whiteFill");
        float whiteHeight = baseData.Get<float>("whiteHeight");

        Color backColor = Color.Lerp(PlayerHasDreamDash ? ActiveBackColor : DisabledBackColor, Color.White, ColorLerp);
        Color lineColor = PlayerHasDreamDash ? ActiveLineColor : DisabledLineColor;

        Draw.Rect(shake.X + X, shake.Y + Y, Width, Height, backColor);

        #region Particles

        Vector2 cameraPositon = camera.Position;
        for (int i = 0; i < particles.Length; i++)
        {
            DreamParticle particle = particles[i];
            int layer = particle.Layer;
            Vector2 position = particle.Position + (cameraPositon * (0.3f + (0.25f * layer)));
            float rotation = ((float) Math.PI / 2f) - 0.8f + (float) Math.Sin(particle.RotationCounter * particle.MaxRotate);
            if (FeatherMode)
            {
                position += Calc.AngleToVector(rotation, 4f);
            }
            position = (Vector2) m_DreamBlock_PutInside.Invoke(this, new object[] { position });
            if (!CheckParticleCollide(position))
                continue;

            Color color = Color.Lerp(particle.Color, Color.Black, ColorLerp);

            if (FeatherMode)
            {
                featherTextures[layer].DrawCentered(position, color, 1, rotation);
            }
            else
            {
                MTexture[] particleTextures = RefillCount != -1 ? doubleRefillStarTextures : baseData.Get<MTexture[]>("particleTextures");
                MTexture particleTexture;
                switch (layer)
                {
                    case 0:
                    {
                        int index = (int) (((particle.TimeOffset * 4f) + baseData.Get<float>("animTimer")) % 4f);
                        particleTexture = particleTextures[3 - index];
                        break;
                    }
                    case 1:
                    {
                        int index = (int) (((particle.TimeOffset * 2f) + baseData.Get<float>("animTimer")) % 2f);
                        particleTexture = particleTextures[1 + index];
                        break;
                    }
                    default:
                        particleTexture = particleTextures[2];
                        break;
                }
                particleTexture.DrawCentered(position, color);
            }
        }

        #endregion

        if (whiteFill > 0f)
        {
            Draw.Rect(X + shake.X, Y + shake.Y, Width, Height * whiteHeight, Color.White * whiteFill);
        }

        WobbleLine(shake + new Vector2(X, Y), shake + new Vector2(X + Width, Y), 0f);
        WobbleLine(shake + new Vector2(X + Width, Y), shake + new Vector2(X + Width, Y + Height), 0.7f);
        WobbleLine(shake + new Vector2(X + Width, Y + Height), shake + new Vector2(X, Y + Height), 1.5f);
        WobbleLine(shake + new Vector2(X, Y + Height), shake + new Vector2(X, Y), 2.5f);

        Draw.Rect(shake + new Vector2(X, Y), 2f, 2f, lineColor);
        Draw.Rect(shake + new Vector2(X + Width - 2f, Y), 2f, 2f, lineColor);
        Draw.Rect(shake + new Vector2(X, Y + Height - 2f), 2f, 2f, lineColor);
        Draw.Rect(shake + new Vector2(X + Width - 2f, Y + Height - 2f), 2f, 2f, lineColor);
    }

    protected bool CheckParticleCollide(Vector2 position)
    {
        float offset = 2f;
        return position.X >= X + offset && position.Y >= Y + offset && position.X < Right - offset && position.Y < Bottom - offset;
    }

    protected bool ShatterCheck()
    {
        return !shattering;
    }

    public virtual void BeginShatter()
    {
        if (ShatterCheck())
        {
            shattering = true;
            Audio.Play(CustomSFX.game_connectedDreamBlock_dreamblock_shatter, Position);
            Add(new Coroutine(ShatterSequence()));
        }
    }

    private IEnumerator ShatterSequence()
    {
        if (QuickDestroy)
        {
            Collidable = false;
            foreach (StaticMover entity in staticMovers)
            {
                entity.Entity.Collidable = false;
            }
        }
        else
        {
            yield return 0.28f;
        }

        while (ColorLerp < 2.0f)
        {
            ColorLerp += Engine.DeltaTime * 10.0f;
            yield return null;
        }

        ColorLerp = 1.0f;
        if (!QuickDestroy)
        {
            yield return 0.05f;
        }

        Level level = SceneAs<Level>();
        level.Shake(.65f);
        Vector2 camera = level.Camera.Position;

        for (int i = 0; i < particles.Length; i++)
        {
            Vector2 position = particles[i].Position;
            position += camera * (0.3f + (0.25f * particles[i].Layer));
            position = (Vector2) m_DreamBlock_PutInside.Invoke(this, new object[] { position });

            Color flickerColor = Color.Lerp(particles[i].Color, Color.White, 0.6f);
            ParticleType type = new(Lightning.P_Shatter)
            {
                ColorMode = ParticleType.ColorModes.Fade,
                Color = particles[i].Color,
                Color2 = flickerColor,
                Source = FeatherMode ? featherTextures[particles[i].Layer] : baseData.Get<MTexture[]>("particleTextures")[2],
                SpinMax = FeatherMode ? (float) Math.PI : 0,
                RotationMode = FeatherMode ? ParticleType.RotationModes.Random : ParticleType.RotationModes.None,
                Direction = (position - Center).Angle()
            };
            level.ParticlesFG.Emit(type, 1, position, Vector2.One * 3f);
        }
        OneUseDestroy();

        Glitch.Value = 0.22f;
        while (Glitch.Value > 0.0f)
        {
            Glitch.Value -= 0.5f * Engine.DeltaTime;
            yield return null;
        }
        Glitch.Value = 0.0f;
        RemoveSelf();
    }

    protected virtual void OneUseDestroy()
    {
        Collidable = Visible = false;
        DisableStaticMovers();
    }

    #region Hooks

    private static readonly List<IDetour> hooks_DreamParticle_Properties = new();

    internal static void Load()
    {
        On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
        On.Celeste.DreamBlock.OnPlayerExit += DreamBlock_OnPlayerExit;
        On.Celeste.DreamBlock.OneUseDestroy += DreamBlock_OneUseDestroy;

        On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
        On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
        IL.Celeste.Player.DreamDashEnd += Player_DreamDashEnd;

        ConnectedDreamBlock.Hook();
        DreamMoveBlock.Load();
        DreamCrumbleWallOnRumble.Load();

        foreach (PropertyInfo prop in typeof(DreamParticle).GetProperties())
        {
            FieldInfo targetField = DreamParticle.t_DreamParticle.GetField(prop.Name);
            if (targetField != null)
            {
                // Special case for position Get method
                if (prop.Name == "Position")
                {
                    hooks_DreamParticle_Properties.Add(new ILHook(
                        typeof(DreamParticle).GetMethod("UpdatePos", BindingFlags.NonPublic | BindingFlags.Instance),
                        ctx => DreamParticle_UpdatePos(ctx, targetField)));
                }
                else
                {
                    hooks_DreamParticle_Properties.Add(new ILHook(prop.GetGetMethod(), ctx => DreamParticle_get_Prop(ctx, targetField)));
                }
                hooks_DreamParticle_Properties.Add(new ILHook(prop.GetSetMethod(), ctx => DreamParticle_set_Prop(ctx, targetField)));
            }
        }
    }

    internal static void Unload()
    {
        On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
        On.Celeste.DreamBlock.OnPlayerExit -= DreamBlock_OnPlayerExit;
        On.Celeste.DreamBlock.OneUseDestroy -= DreamBlock_OneUseDestroy;

        On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
        On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
        IL.Celeste.Player.DreamDashEnd -= Player_DreamDashEnd;

        ConnectedDreamBlock.Unhook();
        DreamMoveBlock.Unload();
        DreamCrumbleWallOnRumble.Unload();

        foreach (IDetour detour in hooks_DreamParticle_Properties)
            detour.Dispose();
    }

    private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self)
    {
        if (self is CustomDreamBlock block)
            block.SetupCustomParticles(block.Width, block.Height);
        else
            orig(self);
    }

    private static void DreamBlock_OnPlayerExit(On.Celeste.DreamBlock.orig_OnPlayerExit orig, DreamBlock dreamBlock, Player player)
    {
        orig(dreamBlock, player);
        if (dreamBlock is CustomDreamBlock customDreamBlock)
        {
            if (customDreamBlock.RefillCount > -1)
            {
                player.Dashes = customDreamBlock.RefillCount;
                Color color = player.GetHairColor(customDreamBlock.RefillCount);
                ParticleType shatter = new(Refill.P_ShatterTwo)
                {
                    Friction = 2f,
                    LifeMin = 0.4f,
                    LifeMax = 0.6f,
                    Color = Color.Lerp(color, Color.White, 0.5f),
                    Color2 = color,
                    ColorMode = ParticleType.ColorModes.Choose
                };
                player.SceneAs<Level>().ParticlesFG.Emit(shatter, 5, player.Position, Vector2.Zero, player.DashDir.Angle());
                Audio.Play(SFX.game_10_pinkdiamond_touch, player.Position);
            }
        }
    }

    private static void DreamBlock_OneUseDestroy(On.Celeste.DreamBlock.orig_OneUseDestroy orig, DreamBlock self)
    {
        if (self is CustomDreamBlock customDreamBlock && customDreamBlock.Collidable)
            customDreamBlock.BeginShatter();
        else
            orig(self);
    }

    private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player player)
    {
        orig(player);
        DynamicData playerData = player.GetData();
        DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
        if (dreamBlock is CustomDreamBlock customDreamBlock)
        {
            if (customDreamBlock.FeatherMode)
            {
                SoundSource dreamSfxLoop = playerData.Get<SoundSource>("dreamSfxLoop");
                player.Stop(dreamSfxLoop);
                player.Loop(dreamSfxLoop, CustomSFX.game_connectedDreamBlock_dreamblock_fly_travel);
            }

            // Ensures the player always properly enters a dream block even when it's moving fast
            if (customDreamBlock is DreamZipMover or DreamSwapBlock)
            {
                player.Position.X += Math.Sign(player.DashDir.X);
                player.Position.Y += Math.Sign(player.DashDir.Y);
            }
        }

    }

    private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player player)
    {
        DynamicData playerData = player.GetData();
        DreamBlock dreamBlock = playerData.Get<DreamBlock>("dreamBlock");
        if (dreamBlock is CustomDreamBlock customDreamBlock && customDreamBlock.FeatherMode)
        {
            Vector2 input = Input.Aim.Value.SafeNormalize();
            if (input != Vector2.Zero)
            {
                Vector2 vector = player.Speed.SafeNormalize();
                if (vector != Vector2.Zero)
                {
                    vector = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector;
                    vector = vector.CorrectJoystickPrecision();
                    player.DashDir = vector;
                    player.Speed = vector * 240f;
                }
            }
        }
        return orig(player);
    }

    // Currently secret/unimplemented, setting RefillCount to -2 will not refill dash
    private static void Player_DreamDashEnd(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>("RefillDash")))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(player => player.GetData().Get<DreamBlock>("dreamBlock") is CustomDreamBlock block && block.RefillCount == -2);
            cursor.Emit(OpCodes.Brtrue_S, cursor.Next.Next);
        }
    }

    #region Cursed

    private static readonly FieldInfo f_CustomDreamParticle_dreamBlock = typeof(DreamParticle).GetField("dreamBlock", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo f_DreamBlock_particles = typeof(DreamBlock).GetField("particles", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo f_CustomDreamParticle_idx = typeof(DreamParticle).GetField("idx", BindingFlags.NonPublic | BindingFlags.Instance);

    /*
     * Position needs some extra care because of issues with methods that return Structs.
     * We use a static field (not threadsafe!) to temporarily store the variable, then return it normally in the property accessor.
     */
    private static void DreamParticle_UpdatePos(ILContext context, FieldInfo targetField)
    {
        FieldInfo f_DreamParticle_tempVec2 = typeof(DreamParticle).GetField("tempVec2", BindingFlags.NonPublic | BindingFlags.Static);
        context.Instrs.Clear();

        ILCursor cursor = new(context);
        // this.dreamBlock.particles
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_dreamBlock);
        cursor.Emit(OpCodes.Ldfld, f_DreamBlock_particles);
        // [this.idx].Position
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_idx);
        cursor.Emit(OpCodes.Ldelema, DreamParticle.t_DreamParticle);
        cursor.Emit(OpCodes.Ldfld, targetField);
        // -> DreamParticle.tempVec2
        cursor.Emit(OpCodes.Stsfld, f_DreamParticle_tempVec2);
        cursor.Emit(OpCodes.Ret);
    }

    private static void DreamParticle_set_Prop(ILContext context, FieldInfo targetField)
    {
        context.Instrs.Clear();

        ILCursor cursor = new(context);
        // this.dreamBlock.particles[this.idx].{targetField} = value
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_dreamBlock);
        cursor.Emit(OpCodes.Ldfld, f_DreamBlock_particles);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_idx);
        cursor.Emit(OpCodes.Ldelema, DreamParticle.t_DreamParticle);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Stfld, targetField);
        // return
        cursor.Emit(OpCodes.Ret);
    }

    private static void DreamParticle_get_Prop(ILContext context, FieldInfo targetField)
    {
        context.Instrs.Clear();

        ILCursor cursor = new(context);
        // return this.dreamBlock.particles[this.idx].{targetField}
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_dreamBlock);
        cursor.Emit(OpCodes.Ldfld, f_DreamBlock_particles);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_idx);
        cursor.Emit(OpCodes.Ldelema, DreamParticle.t_DreamParticle);
        cursor.Emit(OpCodes.Ldfld, targetField);
        cursor.Emit(OpCodes.Ret);
    }

    #endregion

    #endregion

}
