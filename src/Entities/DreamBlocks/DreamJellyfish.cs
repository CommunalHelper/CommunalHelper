using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.DashStates;
using Celeste.Mod.CommunalHelper.Imports;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

// todo: add actAsDreamTunnel support
[CustomEntity("CommunalHelper/DreamJellyfish")]
[Tracked(true)]
internal class DreamJellyfish : Glider
{
    private static readonly MethodInfo m_Player_Pickup = typeof(Player).GetMethod("Pickup", BindingFlags.NonPublic | BindingFlags.Instance);

    public static readonly ParticleType[] P_DreamGlow = new ParticleType[CustomDreamBlock.DreamColors.Length];
    public static readonly ParticleType[] P_DreamGlideUp = new ParticleType[CustomDreamBlock.DreamColors.Length];
    public static readonly ParticleType[] P_DreamGlide = new ParticleType[CustomDreamBlock.DreamColors.Length];

    private static readonly Rectangle particleBounds = new(-23, -35, 48, 60);

    private readonly DreamDashCollider dreamDashCollider;
    public bool AllowDreamDash
    {
        get => dreamDashCollider.Active;
        set => dreamDashCollider.Active = value;
    }

    private readonly DynamicData gliderData;

    private readonly Sprite Sprite;
    private readonly DreamSprite dreamSprite;

    private readonly bool oneUse;
    private readonly bool quickDestroy;

    private bool shouldShatter;
    private bool shattering;

    private readonly bool refillOnFloorSprings, refillOnWallSprings;

    public DreamJellyfish(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Bool("bubble"), data.Bool("tutorial"), data.Bool("oneUse", false), data.Bool("quickDestroy", false), data.Bool("fixedInvertedColliderOffset", false), data.Bool("refillOnFloorSprings", false), data.Bool("refillOnWallSprings", true)) { }

    public DreamJellyfish(Vector2 position, bool bubble, bool tutorial, bool oneUse, bool quickDestroy, bool fixedInvertedColliderOffset, bool refillOnFloorSprings, bool refillOnWallSprings)
        : base(position, bubble, tutorial)
    {
        gliderData = new DynamicData(typeof(Glider), this);

        Sprite oldSprite = gliderData.Get<Sprite>("sprite");
        Remove(oldSprite);
        gliderData.Set("sprite", Sprite = CommunalHelperGFX.SpriteBank.Create("dreamJellyfish"));
        Add(Sprite);
        Sprite.Visible = false;

        Add(dreamSprite = new(Sprite, particleBounds, outlineOffset: new(0, -4), maskOffset: new(-1, 1), invertedSpriteYOffset: 8));

        Add(dreamDashCollider = new DreamDashCollider(new Hitbox(28, 16, -13, -18), OnDreamDashEnter, OnDreamDashExit));

        this.oneUse = oneUse;
        this.quickDestroy = quickDestroy;

        this.refillOnFloorSprings = refillOnFloorSprings;
        this.refillOnWallSprings = refillOnWallSprings;

        // The Dreamdash Collider does not shift down when this entity is inverted (via GravityHelper)
        // So let's add a listener that does this for us.
        // Note: this used to be 1 pixel higher than would be "correct" (ie. preserving relative spacing between colliders) by default, this is why fixedInvertedColliderOffset exists
        Component listener = GravityHelper.CreateGravityListener?.Invoke(this, (_, value, _) =>
        {
            bool inverted = value == (int) GravityType.Inverted;
            dreamDashCollider.Collider.Position.Y = inverted ? (fixedInvertedColliderOffset ? 2 : 1) : -18; // a bit hacky
        });
        if (listener is not null)
            Add(listener);
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
        // if (player.CollideCheck<Solid>())
        //     player.Die(Vector2.Zero);

        DisableDreamDash();

        if (oneUse)
        {
            shouldShatter = true;
        }

        if (Input.GrabCheck && player.DashDir.Y <= 0 && player.Holding == null && player.StateMachine.State != DreamTunnelDash.StDreamTunnelDash)
        {
            // force-allow pickup
            player.GetData().Set("minHoldTimer", 0f);
            DynamicData.For(Hold).Set("cannotHoldTimer", 0f);

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
        dreamSprite.DreamEnabled = AllowDreamDash = true;
        dreamSprite.Flash = 0.5f;
        Sprite.Scale = new Vector2(1.3f, 1.2f);
        Audio.Play(CustomSFX.game_dreamJellyfish_jelly_refill);
    }

    private void DisableDreamDash()
    {
        if (!AllowDreamDash)
            return;
        dreamSprite.DreamEnabled = AllowDreamDash = false;
        dreamSprite.Flash = 1f;
        Audio.Play(CustomSFX.game_dreamJellyfish_jelly_use);
    }

    public override void Update()
    {
        base.Update();

        if (shouldShatter && !shattering)
        {
            Audio.Play(CustomSFX.game_connectedDreamBlock_dreamblock_shatter, Position);
            Add(new Coroutine(ShatterSequence()));
            shouldShatter = false;
            shattering = true;
        }

        if ((Hold.Holder == null && OnGround()) || (Hold.Holder != null && Hold.Holder.OnGround()))
        {
            EnableDreamDash();
        }
    }

    private IEnumerator ShatterSequence()
    {
        if (quickDestroy)
        {
            Collidable = false;
        }
        else
        {
            yield return 0.28f;
        }

        dreamSprite.Active = false;
        while (dreamSprite.Flash <= 1f)
        {
            dreamSprite.Flash += Engine.DeltaTime * 10.0f;
            yield return null;
        }
        dreamSprite.Flash = 1.0f;

        if (!quickDestroy)
        {
            yield return 0.05f;
        }

        Level level = SceneAs<Level>();
        level.Shake(.65f);

        for (int i = 0; i < dreamSprite.Particles.Length; i++)
        {
            Vector2 position = dreamSprite.Particles[i].Position + Position;
            if (!dreamDashCollider.Collider.Bounds.Contains((int) position.X, (int) position.Y)) // eeh
                continue;

            Color flickerColor = Color.Lerp(dreamSprite.Particles[i].EnabledColor, Color.White, 0.6f);
            ParticleType type = new(Lightning.P_Shatter)
            {
                ColorMode = ParticleType.ColorModes.Fade,
                Color = dreamSprite.Particles[i].EnabledColor,
                Color2 = flickerColor,
                Source = DreamSprite.ParticleTextures[2],
                SpinMax = 0,
                RotationMode = ParticleType.RotationModes.None,
                Direction = (position - Center).Angle()
            };
            level.ParticlesFG.Emit(type, 1, position, Vector2.One * 3f);
        }

        Collidable = Visible = false;

        Glitch.Value = 0.22f;
        while (Glitch.Value > 0.0f)
        {
            Glitch.Value -= 0.5f * Engine.DeltaTime;
            yield return null;
        }
        Glitch.Value = 0.0f;

        RemoveSelf();
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
        On.Celeste.Holdable.Check += Holdable_Check;

        On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
        On.Celeste.Player.StartDash += Player_StartDash;

        On.Celeste.Glider.HitSpring += Glider_HitSpring;
        On.Celeste.Player.SideBounce += Player_SideBounce;
        On.Celeste.Player.SuperBounce += Player_SuperBounce;

        // Change particles
        IL.Celeste.Glider.Update += Glider_Update;
    }

    internal static void Unload()
    {
        On.Celeste.Holdable.Check -= Holdable_Check;

        On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
        On.Celeste.Player.StartDash -= Player_StartDash;

        On.Celeste.Glider.HitSpring -= Glider_HitSpring;
        On.Celeste.Player.SideBounce -= Player_SideBounce;
        On.Celeste.Player.SuperBounce -= Player_SuperBounce;

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

    private static bool Player_SideBounce(On.Celeste.Player.orig_SideBounce orig, Player self, int dir, float fromX, float fromY)
    {
        bool result = orig(self, dir, fromX, fromY);
        if (result && self.Holding?.Entity is DreamJellyfish jelly && jelly.refillOnWallSprings)
            jelly.EnableDreamDash();
        return result;
    }

    private static void Player_SuperBounce(On.Celeste.Player.orig_SuperBounce orig, Player self, float fromY)
    {
        orig(self, fromY);
        if (self.Holding?.Entity is DreamJellyfish jelly && jelly.refillOnFloorSprings)
            jelly.EnableDreamDash();
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
