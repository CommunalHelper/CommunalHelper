using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.Imports;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DreamTheoCrystal")]
[TrackedAs(typeof(TheoCrystal))]
[Tracked(true)]
internal class DreamTheoCrystal : TheoCrystal
{
    public static readonly ParticleType[] P_DreamImpact = new ParticleType[CustomDreamBlock.DreamColors.Length];

    private static readonly Rectangle particleBounds = new(-12, -24, 24, 48);
    private readonly DreamSprite dreamSprite;
    private readonly Sprite overlaySprite;

    public DreamHoldable DreamHold;

    public DreamTheoCrystal(EntityData data, Vector2 offset) : base(data, offset)
    {
        Remove(sprite);
        Add(sprite = dreamSprite = new DreamSprite(CommunalHelperGFX.SpriteBank.Create("dreamTheoCrystal"), particleBounds)
        {
            OnChange = (_, current) => overlaySprite.Play(current, true)
        });

        Add(overlaySprite = CommunalHelperGFX.SpriteBank.Create("dreamTheoCrystalOverlay"));

        Remove(Hold);
        Add(Hold = DreamHold = new DreamHoldable(
            new Hitbox(20, 20, -10, -20),
            0.1f,
            () =>
            {
                dreamSprite.Enabled = true;
                dreamSprite.Flash = 0.5f;
                Audio.Play(CustomSFX.game_dreamJellyfish_jelly_refill);
            },
            () =>
            {
                dreamSprite.Enabled = false;
                dreamSprite.Flash = 1f;
                Audio.Play(CustomSFX.game_dreamJellyfish_jelly_use);
            }
        )
        {
            PickupCollider = new Hitbox(16f, 22f, -8f, -16f),
            SlowFall = false,
            SlowRun = true,
            OnPickup = OnPickup,
            OnRelease = OnRelease,
            DangerousCheck = Dangerous,
            OnHitSeeker = HitSeeker,
            OnSwat = Swat,
            OnHitSpring = HitSpring,
            OnHitSpinner = HitSpinner,
            SpeedGetter = () => Speed,
            SpeedSetter = delegate(Vector2 speed)
            {
                Speed = speed;
            },
        });

        Component listener = GravityHelper.CreateGravityListener?.Invoke(this, (_, value, _) =>
            overlaySprite.Scale.Y = value == (int) GravityType.Inverted ? -1 : 1
        );
        if (listener is not null)
            Add(listener);
    }

    internal static void InitializeParticles()
    {
        for (int i = 0; i < CustomDreamBlock.DreamColors.Length; i++)
        {
            P_DreamImpact[i] = new ParticleType(P_Impact)
            {
                Color = CustomDreamBlock.DreamColors[i],
                Color2 = Color.Lerp(CustomDreamBlock.DreamColors[(i + 2) % CustomDreamBlock.DreamColors.Length], P_Impact.Color2, 0.4f),
            };
        }
    }

    #region Hooks

    private static readonly MethodInfo m_ParticleSystem_Emit = typeof(ParticleSystem).GetMethod("Emit", BindingFlags.Public | BindingFlags.Instance, new Type[] { typeof(ParticleType), typeof(int), typeof(Vector2), typeof(Vector2), typeof(float) });

    internal static void Load()
    {
        // Change particles
        IL.Celeste.TheoCrystal.ImpactParticles += TheoCrystal_ImpactParticles;
    }

    internal static void Unload()
    {
        IL.Celeste.TheoCrystal.ImpactParticles -= TheoCrystal_ImpactParticles;
    }

    private static void TheoCrystal_ImpactParticles(ILContext il) {
        ILCursor cursor = new(il);

        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt(m_ParticleSystem_Emit)))
        {
            ILLabel normalBehavior = cursor.DefineLabel();
            ILLabel afterNormalBehavior = cursor.DefineLabel();

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<TheoCrystal, bool>>(theo => theo is DreamTheoCrystal);
            cursor.Emit(OpCodes.Brfalse, normalBehavior);
            cursor.EmitDelegate<Action<ParticleSystem, ParticleType, int, Vector2, Vector2, float>>((particleSystem, _, num, position, positionRange, direction) =>
            {
                for (int i = 0; i < num; i++)
                {
                    particleSystem.Emit(Calc.Random.Choose(P_DreamImpact), 1, position, positionRange, direction);
                }
            });
            cursor.Emit(OpCodes.Br, afterNormalBehavior);
            cursor.MarkLabel(normalBehavior);
            cursor.Index++;
            cursor.MarkLabel(afterNormalBehavior);
        }
    }

    #endregion
}