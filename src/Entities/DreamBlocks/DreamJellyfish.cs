using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.Imports;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DreamJellyfish")]
[Tracked(true)]
internal class DreamJellyfish : Glider
{
    private static readonly MethodInfo m_Player_Pickup = typeof(Player).GetMethod("Pickup", BindingFlags.NonPublic | BindingFlags.Instance);

    public static readonly ParticleType[] P_DreamGlow = new ParticleType[CustomDreamBlock.DreamColors.Length];
    public static readonly ParticleType[] P_DreamGlideUp = new ParticleType[CustomDreamBlock.DreamColors.Length];
    public static readonly ParticleType[] P_DreamGlide = new ParticleType[CustomDreamBlock.DreamColors.Length];

    // Could maybe use CustomDreamBlock.DreamParticle.
    public struct DreamParticle
    {
        public Vector2 Position;
        public int Layer;
        public Color EnabledColor, DisabledColor;
        public float TimeOffset;
    }
    public DreamParticle[] Particles;
    public static MTexture[] ParticleTextures;
    public float Flash;

    public static readonly Rectangle ParticleBounds = new(-23, -35, 48, 60);

    private readonly DreamDashCollider dreamDashCollider;
    public bool AllowDreamDash
    {
        get => dreamDashCollider.Active;
        set => dreamDashCollider.Active = value;
    }

    private readonly DynamicData gliderData;

    public Sprite Sprite;

    public DreamJellyfish(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Bool("bubble"), data.Bool("tutorial")) { }

    public DreamJellyfish(Vector2 position, bool bubble, bool tutorial)
        : base(position, bubble, tutorial)
    {
        gliderData = new DynamicData(typeof(Glider), this);

        Sprite oldSprite = gliderData.Get<Sprite>("sprite");
        Remove(oldSprite);
        gliderData.Set("sprite", Sprite = CommunalHelperGFX.SpriteBank.Create("dreamJellyfish"));
        Add(Sprite);

        Visible = Sprite.Visible = false;

        Add(dreamDashCollider = new DreamDashCollider(new Hitbox(28, 16, -13, -18), OnDreamDashEnter, OnDreamDashExit));

        // The Dreamdash Collider does not shift down when this entity is inverted (via GravityHelper)
        // So let's add a listener that does this for us.
        Component listener = GravityHelper.CreateGravityListener?.Invoke(this, (_, value, _) =>
        {
            bool inverted = value == (int) GravityType.Inverted;
            dreamDashCollider.Collider.Position.Y = inverted ? 1 : -18; // a bit hacky
        });
        if (listener is not null)
            Add(listener);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        int w = ParticleBounds.Width;
        int h = ParticleBounds.Height;
        Particles = new DreamParticle[(int) (w / 8f * (h / 8f) * 1.5f)];
        for (int i = 0; i < Particles.Length; i++)
        {
            Particles[i].Position = new Vector2(Calc.Random.NextFloat(w), Calc.Random.NextFloat(h));
            Particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
            Particles[i].TimeOffset = Calc.Random.NextFloat();

            Particles[i].DisabledColor = Color.LightGray * (0.5f + (Particles[i].Layer / 2f * 0.5f));
            Particles[i].DisabledColor.A = 255;

            Particles[i].EnabledColor = Particles[i].Layer switch
            {
                0 => Calc.Random.Choose(CustomDreamBlock.DreamColors[0], CustomDreamBlock.DreamColors[1], CustomDreamBlock.DreamColors[2]),
                1 => Calc.Random.Choose(CustomDreamBlock.DreamColors[3], CustomDreamBlock.DreamColors[4], CustomDreamBlock.DreamColors[5]),
                2 => Calc.Random.Choose(CustomDreamBlock.DreamColors[6], CustomDreamBlock.DreamColors[7], CustomDreamBlock.DreamColors[8]),
                _ => throw new NotImplementedException()
            };
        }

        scene.Tracker.GetEntity<DreamJellyfishRenderer>().Track(this);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        scene.Tracker.GetEntity<DreamJellyfishRenderer>().Untrack(this);
    }

    private void OnDreamDashEnter(Player player)
    {
        DynamicData data = DynamicData.For(player);

        BloomPoint starFlyBloom = data.Get<BloomPoint>("starFlyBloom");

        // Prevents a crash caused by entering the feather fly state while dream dashing through a dream jellyfish.
        starFlyBloom ??= new(new Vector2(0f, -6f), 0f, 16f) { Visible = false };

        data.Set("starFlyBloom", starFlyBloom);
    }

    public void OnDreamDashExit(Player player)
    {
        DisableDreamDash();
        if (Input.GrabCheck && player.DashDir.Y <= 0)
        {
            // force-allow pickup
            player.GetData().Set("minHoldTimer", 0f);
            DynamicData.For(Hold).Set("cannotHoldTimer", 0);

            if ((bool) m_Player_Pickup.Invoke(player, new object[] { Hold }))
            {
                player.StateMachine.State = Player.StPickup;
            }
        }
    }

    private void EnableDreamDash()
    {
        if (AllowDreamDash)
            return;
        AllowDreamDash = true;
        Flash = 0.5f;
        Sprite.Scale = new Vector2(1.3f, 1.2f);
        Audio.Play(CustomSFX.game_dreamJellyfish_jelly_refill);
    }

    private void DisableDreamDash()
    {
        if (!AllowDreamDash)
            return;
        AllowDreamDash = false;
        Flash = 1f;
        Audio.Play(CustomSFX.game_dreamJellyfish_jelly_use);
    }

    public override void Update()
    {
        base.Update();

        Flash = Calc.Approach(Flash, 0f, Engine.DeltaTime * 2.5f);

        if ((Hold.Holder == null && OnGround()) || (Hold.Holder != null && Hold.Holder.OnGround()))
        {
            EnableDreamDash();
        }
    }

    public static void InitializeTextures()
    {
        ParticleTextures = new MTexture[4] {
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
        };
    }

    public static void InitializeParticles()
    {
        Color flash = P_Glow.Color2;
        for (int i = 0; i < CustomDreamBlock.DreamColors.Length; i++)
        {
            Color color = CustomDreamBlock.DreamColors[i];
            Color highlight = i % 2 == 0 ? P_Glide.Color : P_Glide.Color2;
            Color next = Color.Lerp(CustomDreamBlock.DreamColors[(i + 2) % CustomDreamBlock.DreamColors.Length], flash, 0.4f);

            P_DreamGlow[i] = new ParticleType(P_Glow)
            {
                Color = color,
                Color2 = next,
            };
            P_DreamGlide[i] = new ParticleType(P_Glide)
            {
                Color = color,
                Color2 = highlight,
            };
            P_DreamGlideUp[i] = new ParticleType(P_GlideUp)
            {
                Color = color,
                Color2 = highlight,
            };
        }
    }

    #region Hooks

    private static readonly FieldInfo f_Glider_P_Glow = typeof(Glider).GetField("P_Glow", BindingFlags.Public | BindingFlags.Static);
    private static readonly FieldInfo f_Glider_P_GlideUp = typeof(Glider).GetField("P_GlideUp", BindingFlags.Public | BindingFlags.Static);
    private static readonly FieldInfo f_Glider_P_Glide = typeof(Glider).GetField("P_Glide", BindingFlags.Public | BindingFlags.Static);

    internal static void Load()
    {
        On.Celeste.Holdable.Pickup += Holdable_Pickup;
        On.Celeste.Holdable.Check += Holdable_Check;

        On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
        On.Celeste.Player.StartDash += Player_StartDash;

        On.Celeste.Glider.HitSpring += Glider_HitSpring;
        On.Celeste.Player.SideBounce += Player_SideBounce;

        // Change particles
        IL.Celeste.Glider.Update += Glider_Update;
    }

    internal static void Unload()
    {
        On.Celeste.Holdable.Pickup -= Holdable_Pickup;
        On.Celeste.Holdable.Check -= Holdable_Check;

        On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
        On.Celeste.Player.StartDash -= Player_StartDash;

        On.Celeste.Glider.HitSpring -= Glider_HitSpring;
        On.Celeste.Player.SideBounce -= Player_SideBounce;

        IL.Celeste.Glider.Update -= Glider_Update;
    }

    private static void Glider_Update(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(f_Glider_P_Glow)))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<ParticleType, Glider, ParticleType>>((particleType, glider) =>
                glider is DreamJellyfish ? Calc.Random.Choose(P_DreamGlow) : particleType
            );
        }

        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(f_Glider_P_GlideUp)))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<ParticleType, Glider, ParticleType>>((particleType, glider) =>
                glider is DreamJellyfish ? Calc.Random.Choose(P_DreamGlideUp) : particleType
            );
        }
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(f_Glider_P_Glide)))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<ParticleType, Glider, ParticleType>>((particleType, glider) =>
                glider is DreamJellyfish ? Calc.Random.Choose(P_DreamGlide) : particleType
            );
        }
    }

    private static bool Holdable_Pickup(On.Celeste.Holdable.orig_Pickup orig, Holdable self, Player player)
    {
        bool result = orig(self, player);

        if (self.Entity is DreamJellyfish jelly)
            jelly.Visible = false;

        return result;
    }

    private static bool Player_SideBounce(On.Celeste.Player.orig_SideBounce orig, Player self, int dir, float fromX, float fromY)
    {
        bool result = orig(self, dir, fromX, fromY);
        if (result && self.Holding?.Entity is DreamJellyfish jelly)
            jelly.EnableDreamDash();
        return result;
    }

    private static bool Glider_HitSpring(On.Celeste.Glider.orig_HitSpring orig, Glider self, Spring spring)
    {
        if (self is DreamJellyfish jelly)
            jelly.EnableDreamDash();
        return orig(self, spring);
    }

    private static int Player_StartDash(On.Celeste.Player.orig_StartDash orig, Player self)
    {
        if (self.Holding?.Entity is DreamJellyfish && Input.MoveY.Value == -1f)
            self.Drop();
        return orig(self);
    }

    private static bool Holdable_Check(On.Celeste.Holdable.orig_Check orig, Holdable self, Player player)
    {
        return self.Entity is DreamJellyfish jelly && jelly.AllowDreamDash && player.DashAttacking ? false : orig(self, player);
    }

    private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self)
    {
        int result = orig(self);

        if (self.Holding?.Entity is DreamJellyfish jelly && jelly.AllowDreamDash && self.CanDash && Input.MoveY.Value == -1f)
        {
            self.Drop();
            return self.StartDash();
        }

        return result;
    }

    #endregion
}
