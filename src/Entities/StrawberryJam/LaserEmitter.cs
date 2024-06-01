using Celeste.Mod.CommunalHelper.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[CustomEntity("CommunalHelper/SJ/LaserEmitter")]
public class LaserEmitter : Entity
{
    #region Properties

    public float Alpha { get; }
    public bool CollideWithSolids { get; }
    public Color Color { get; }
    public string ColorChannel { get; }
    public bool DisableLasers { get; }
    public bool Flicker { get; }
    public bool KillPlayer { get; }
    public float Thickness { get; }
    public bool TriggerZipMovers { get; }
    // public int Leniency { get; }
    public float EmitterColliderWidth { get; }
    public float EmitterColliderHeight { get; }
    public bool EmitSparks { get; }
    public LaserOrientations Orientation { get; }
    public FlagActionType FlagAction { get; }
    public string FlagName { get; }
    public string SpriteName { get; }
    public bool UseTintOverlay { get; }

    #endregion

    #region Private Fields

    private const float flickerFrequency = 4f;
    private const float beamFlickerRange = 0.25f;
    private const float emitterFlickerRange = 0.15f;
    private const int leniency = 1;

    private float sineValue;

    private readonly Sprite emitterSprite;
    private readonly Sprite tintSprite;
    private readonly LaserColliderComponent laserCollider;

    private static ParticleType P_Sparks;

    #endregion

    private static void LoadParticles()
    {
        P_Sparks ??= new ParticleType(ZipMover.P_Sparks)
        {
            SpeedMultiplier = 3f, LifeMin = 0.3f, LifeMax = 0.5f, FadeMode = ParticleType.FadeModes.Late,
        };
    }

    private static void setLaserSyncFlag(string colorChannel, bool value) =>
        (Engine.Scene as Level)?.Session.SetFlag($"ZipMoverSyncLaser:{colorChannel.ToLower()}", value);

    private static bool getLaserSyncFlag(string colorChannel) =>
        (Engine.Scene as Level)?.Session.GetFlag($"ZipMoverSyncLaser:{colorChannel.ToLower()}") ?? false;

    public LaserEmitter(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        string colorString = data.Attr("color", null);
        string colorChannelString = data.Attr("colorChannel", null);
        colorString ??= colorChannelString ?? "ff0000";
        colorChannelString ??= colorString;

        LoadParticles();
        
        Alpha = Calc.Clamp(data.Float("alpha", 0.4f), 0f, 1f);
        CollideWithSolids = data.Bool("collideWithSolids", true);
        Color = Calc.HexToColor(colorString.ToLower());
        ColorChannel = colorChannelString.ToLower();
        DisableLasers = data.Bool("disableLasers");
        Flicker = data.Bool("flicker", true);
        KillPlayer = data.Bool("killPlayer", true);
        Thickness = Math.Max(data.Float("thickness", 6f), 0f);
        TriggerZipMovers = data.Bool("triggerZipMovers");
        Orientation = data.Enum("orientation", LaserOrientations.Up);
        FlagName = data.Attr("flagName");
        FlagAction = data.Enum("flagAction", FlagActionType.None);
        SpriteName = data.Attr("spriteName");
        UseTintOverlay = data.Bool("useTintOverlay", true);
        // Leniency = Math.Max(0, data.Int("leniency", 1));
        EmitterColliderWidth = Math.Max(0, data.Int("emitterColliderWidth", 14));
        EmitterColliderHeight = Math.Max(0, data.Int("emitterColliderHeight", 6));
        EmitSparks = data.Bool("emitSparks", true);

        Depth = Depths.Above;

        Add(new PlayerCollider(OnPlayerCollide),
            laserCollider = new LaserColliderComponent
            {
                CollideWithSolids = CollideWithSolids,
                Thickness = Thickness - leniency * 2,
                Orientation = Orientation,
                Offset = Orientation.Normal() * EmitterColliderHeight,
            },
            new SineWave(flickerFrequency) { OnUpdate = v => sineValue = v },
            new LedgeBlocker(_ => KillPlayer)
        );

        if (!string.IsNullOrWhiteSpace(SpriteName))
            emitterSprite = GFX.SpriteBank.Create(SpriteName);
        else
            emitterSprite = CommunalHelperGFX.SpriteBank.Create("laserEmitter");

        emitterSprite.Play("base");
        emitterSprite.Rotation = Orientation.Angle();
        Add(emitterSprite);

        if (UseTintOverlay)
        {
            if (!string.IsNullOrWhiteSpace(SpriteName))
                tintSprite = GFX.SpriteBank.Create(SpriteName);
            else
                tintSprite = CommunalHelperGFX.SpriteBank.Create("laserEmitter");

            tintSprite.Play("tint");
            tintSprite.Color = Color;
            tintSprite.Rotation = Orientation.Angle();
            Add(tintSprite);
        }

        if (EmitterColliderWidth > 0 && EmitterColliderHeight > 0)
        {
            var hitbox = new Hitbox(Orientation.Vertical() ? EmitterColliderWidth : EmitterColliderHeight,
                Orientation.Vertical() ? EmitterColliderHeight : EmitterColliderWidth);

            switch (Orientation)
            {
                case LaserOrientations.Up:
                    hitbox.BottomCenter = Vector2.Zero;
                    break;
                case LaserOrientations.Down:
                    hitbox.TopCenter = Vector2.Zero;
                    break;
                case LaserOrientations.Left:
                    hitbox.CenterRight = Vector2.Zero;
                    break;
                default:
                    hitbox.CenterLeft = Vector2.Zero;
                    break;
            }

            Collider = new ColliderList(laserCollider.Collider, hitbox);
        }
        else
        {
            Collider = laserCollider.Collider;
        }

        Add(new StaticMover
        {
            OnAttach = p => Depth = p.Depth + 1,
            SolidChecker = s => Collide.CheckPoint(s, Position - Orientation.Direction()),
            JumpThruChecker = jt => Collide.CheckPoint(jt, Position - Orientation.Direction()),
            OnEnable = () => Collidable = true,
            OnDisable = () => Collidable = false,
            OnMove = v =>
            {
                Position += v;
                laserCollider.UpdateBeam();
            }
        });
    }

    public override void Render()
    {
        // only render beam if we're collidable
        if (Collidable)
        {
            float alphaMultiplier = 1f - (sineValue + 1f) * 0.5f * beamFlickerRange;
            var color = Color * Alpha * (Flicker ? alphaMultiplier : 1f);
            var collider = laserCollider.Collider;
            var bounds = collider.Bounds;

            if (Orientation.Horizontal())
            {
                bounds.Height += leniency * 2;
                bounds.Y -= leniency;
            }
            else
            {
                bounds.Width += leniency * 2;
                bounds.X -= leniency;
            }

            Draw.Rect(bounds, color);

            Vector2 source = laserCollider.Offset;
            Vector2 target = Orientation switch
            {
                LaserOrientations.Up => Collider.TopCenter,
                LaserOrientations.Down => Collider.BottomCenter,
                LaserOrientations.Left => Collider.CenterLeft,
                LaserOrientations.Right => Collider.CenterRight,
                _ => Vector2.Zero,
            };

            float lineThickness = Orientation.Horizontal()
                ? bounds.Height / 3f
                : bounds.Width / 3f;

            Draw.Line(X + source.X, Y + source.Y, X + target.X, Y + target.Y, color, lineThickness);
        }

        // update tint layer based on multiplier and collision
        if (tintSprite != null)
        {
            Color color;
            if (!Collidable)
                color = Color.Gray;
            else
            {
                float alphaMultiplier = 1f - (sineValue + 1f) * 0.5f * emitterFlickerRange;
                color = Color * (Flicker ? alphaMultiplier : 1f);
            }

            color.A = 255;
            tintSprite.Color = color;
        }

        base.Render();
    }

    public override void Update()
    {
        base.Update();

        if (EmitSparks && Collidable && Scene.OnInterval(0.1f) && !laserCollider.CollidedWithScreenBounds)
        {
            var laserHitbox = laserCollider.Collider;
            float angle = Orientation.Angle() + (float) Math.PI / 2f;
            var startX = Orientation switch
            {
                LaserOrientations.Right => laserHitbox.Right,
                LaserOrientations.Left => laserHitbox.Left + 1,
                _ => 0,
            };
            var startY = Orientation switch
            {
                LaserOrientations.Up => laserHitbox.Top + 1,
                LaserOrientations.Down => laserHitbox.Bottom,
                _ => 0,
            };
            var startPos = new Vector2(startX + X, startY + Y);
            var perp = Orientation.Normal().Perpendicular().Abs();
            SceneAs<Level>().ParticlesBG.Emit(P_Sparks, startPos + perp * 3, angle);
            SceneAs<Level>().ParticlesBG.Emit(P_Sparks, startPos - perp * 2, angle);
        }
    }

    public override void Removed(Scene scene)
    {
        setLaserSyncFlag(ColorChannel, false);
        base.Removed(scene);
    }

    private void OnPlayerCollide(Player player)
    {
        var level = player.SceneAs<Level>();

        if (DisableLasers)
        {
            level.Entities.With<LaserEmitter>(emitter =>
            {
                if (emitter.ColorChannel == ColorChannel)
                    emitter.Collidable = false;
            });
        }

        if (TriggerZipMovers)
        {
            setLaserSyncFlag(ColorChannel, true);
        }

        if (!string.IsNullOrWhiteSpace(FlagName) && FlagAction != FlagActionType.None)
        {
            level.Session.SetFlag(FlagName, FlagAction == FlagActionType.Set);
        }

        if (KillPlayer)
        {
            Vector2 direction;
            if (Orientation == LaserOrientations.Left || Orientation == LaserOrientations.Right)
                direction = player.Center.Y <= Position.Y ? -Vector2.UnitY : Vector2.UnitY;
            else
                direction = player.Center.X <= Position.X ? -Vector2.UnitX : Vector2.UnitX;

            player.Die(direction);
        }
    }

    public enum FlagActionType
    {
        None,
        Set,
        Clear,
    }

    #region Hooks

    private const string linkedZipMoverTypeName = "Celeste.Mod.AdventureHelper.Entities.LinkedZipMover";
    private const string linkedZipMoverNoReturnTypeName = "Celeste.Mod.AdventureHelper.Entities.LinkedZipMoverNoReturn";

    private const string zipMoverSoundControllerTypeName =
        "Celeste.Mod.AdventureHelper.Entities.ZipMoverSoundController";

    private static Type linkedZipMoverType;
    private static Type linkedZipMoverNoReturnType;
    private static MethodInfo linkedZipMoverSequence;
    private static PropertyInfo linkedZipMoverColorCode;
    private static MethodInfo linkedZipMoverNoReturnSequence;
    private static PropertyInfo linkedZipMoverNoReturnColorCode;

    private static ILHook linkedZipMoverHook;
    private static ILHook linkedZipMoverNoReturnHook;

    public static void Load()
    {
        // reflect types
        linkedZipMoverType = Everest.Modules
            .FirstOrDefault(m => m.Metadata.Name == "AdventureHelper")?
            .GetType().Assembly.GetType(linkedZipMoverTypeName);
        linkedZipMoverNoReturnType = Everest.Modules
            .FirstOrDefault(m => m.Metadata.Name == "AdventureHelper")?
            .GetType().Assembly.GetType(linkedZipMoverNoReturnTypeName);
        
        // reflect methods and properties
        linkedZipMoverSequence = linkedZipMoverType?.GetMethod("Sequence", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        linkedZipMoverColorCode = linkedZipMoverType?.GetProperty("ColorCode", BindingFlags.Public | BindingFlags.Instance);
        linkedZipMoverNoReturnSequence = linkedZipMoverNoReturnType?.GetMethod("Sequence", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget();
        linkedZipMoverNoReturnColorCode = linkedZipMoverNoReturnType?.GetProperty("ColorCode", BindingFlags.Public | BindingFlags.Instance);

        // create hooks
        if (linkedZipMoverSequence != null) linkedZipMoverHook = new ILHook(linkedZipMoverSequence, LinkedZipMover_Sequence);
        if (linkedZipMoverNoReturnSequence != null) linkedZipMoverNoReturnHook = new ILHook(linkedZipMoverNoReturnSequence, LinkedZipMoverNoReturn_Sequence);
    }

    public static void Unload()
    {
        linkedZipMoverHook?.Dispose();
        linkedZipMoverHook = null;
        linkedZipMoverNoReturnHook?.Dispose();
        linkedZipMoverNoReturnHook = null;
    }

    private static void LinkedZipMover_Sequence(ILContext il)
    {
        var cursor = new ILCursor(il);

        // find the HasPlayerRider check
        cursor.GotoNext(instr => instr.MatchCallvirt<Solid>(nameof(Solid.HasPlayerRider)));

        // emit flag check
        cursor.EmitDelegate<Func<Solid, bool>>(self =>
            self.HasPlayerRider() || getLaserSyncFlag((string) linkedZipMoverColorCode.GetValue(self)));
        cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

        // find code near the end of the loop
        cursor.GotoNext(instr => instr.MatchCall(zipMoverSoundControllerTypeName, "StopSound"));
        cursor.GotoPrev(MoveType.After, instr => instr.MatchCallvirt(linkedZipMoverTypeName, "get_ColorCode"));

        // emit clear flag
        cursor.EmitDelegate<Func<string, string>>(colorCode =>
        {
            setLaserSyncFlag(colorCode, false);
            return colorCode;
        });
    }

    private static void LinkedZipMoverNoReturn_Sequence(ILContext il)
    {
        var cursor = new ILCursor(il);

        // find the HasPlayerRider check
        cursor.GotoNext(instr => instr.MatchCallvirt<Solid>(nameof(Solid.HasPlayerRider)));

        // emit flag check
        cursor.EmitDelegate<Func<Solid, bool>>(self =>
            self.HasPlayerRider() || getLaserSyncFlag((string) linkedZipMoverNoReturnColorCode.GetValue(self)));
        cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

        // find code near the end of the loop
        cursor.GotoNext(instr => instr.MatchCall(zipMoverSoundControllerTypeName, "StopSound"));
        cursor.GotoPrev(MoveType.After, instr => instr.MatchCallvirt(linkedZipMoverNoReturnTypeName, "get_ColorCode"));

        // emit clear flag
        cursor.EmitDelegate<Func<string, string>>(colorCode =>
        {
            setLaserSyncFlag(colorCode, false);
            return colorCode;
        });
    }

    #endregion
}
