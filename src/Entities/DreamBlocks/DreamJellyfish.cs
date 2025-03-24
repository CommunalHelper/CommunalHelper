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
    public static readonly ParticleType[] P_DreamGlow = new ParticleType[CustomDreamBlock.DreamColors.Length];
    public static readonly ParticleType[] P_DreamGlideUp = new ParticleType[CustomDreamBlock.DreamColors.Length];
    public static readonly ParticleType[] P_DreamGlide = new ParticleType[CustomDreamBlock.DreamColors.Length];

    private static readonly Rectangle particleBounds = new(-23, -35, 48, 60);
    private readonly DreamSprite dreamSprite;

    public DreamHoldable DreamHold;

    public DreamJellyfish(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Bool("bubble"), data.Bool("tutorial")) { }

    public DreamJellyfish(Vector2 position, bool bubble, bool tutorial)
        : base(position, bubble, tutorial)
    {
        Remove(sprite);
        Add(sprite = dreamSprite = new DreamSprite(
            CommunalHelperGFX.SpriteBank.Create("dreamJellyfish"),
            particleBounds,
            delegate (ref Vector2 position, ref Vector2 scale, ref float rotation)
            {
                scale = new(scale.X, -scale.Y);
                rotation = -rotation;
            }
        ));

        Remove(Hold);
        Add(Hold = DreamHold = new DreamHoldable(
            new Hitbox(28, 16, -13, -18),
            () =>
            {
                dreamSprite.Enabled = true;
                dreamSprite.Flash = 0.5f;
                dreamSprite.Scale = new Vector2(1.3f, 1.2f);
                Audio.Play(CustomSFX.game_dreamJellyfish_jelly_refill);
            },
            () =>
            {
                dreamSprite.Enabled = false;
                dreamSprite.Flash = 1f;
                Audio.Play(CustomSFX.game_dreamJellyfish_jelly_use);
            },
            0.3f
        )
        {
            PickupCollider = new Hitbox(20f, 22f, -10f, -16f),
            SlowFall = true,
            SlowRun = false,
            OnPickup = OnPickup,
            OnRelease = OnRelease,
            SpeedGetter = () => Speed,
            OnHitSpring = HitSpring,
            SpeedSetter = delegate(Vector2 speed)
            {
                Speed = speed;
            },
        });

        // The Dreamdash Collider does not shift down when this entity is inverted (via GravityHelper)
        // So let's add a listener that does this for us.
        Component listener = GravityHelper.CreateGravityListener?.Invoke(this, (_, value, _) =>
        {
            bool inverted = value == (int) GravityType.Inverted;
            DreamHold.DreamDashCollider.Collider.Position.Y = inverted ? 1 : -18; // a bit hacky
        });
        if (listener is not null)
            Add(listener);
    }

    internal static void InitializeParticles()
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
        // Change particles
        IL.Celeste.Glider.Update += Glider_Update;
    }

    internal static void Unload()
    {
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

    #endregion
}
