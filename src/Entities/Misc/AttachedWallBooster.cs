using Mono.Cecil.Cil;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/AttachedWallBooster")]
[TrackedAs(typeof(WallBooster))]
public class AttachedWallBooster : WallBooster
{
    private enum CoreModeBehavior
    {
        ToggleDefaultHot,
        ToggleDefaultCold,
        AlwaysHot,
        AlwaysCold
    }

    public Vector2 Shake = Vector2.Zero;

    private readonly DynamicData baseData;

    private bool legacyBoost;

    private readonly CoreModeBehavior coreModeBehavior;

    public AttachedWallBooster(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        baseData = DynamicData.For(this);

        Remove(Get<StaticMover>());
        Add(new StaticMover
        {
            OnShake = OnShake,
            SolidChecker = IsRiding,
            OnEnable = OnEnable,
            OnDisable = OnDisable
        });

        coreModeBehavior = data.Enum("coreModeBehavior", CoreModeBehavior.ToggleDefaultHot);
        if (coreModeBehavior is CoreModeBehavior.AlwaysHot or CoreModeBehavior.AlwaysCold)
        {
            Remove(Get<CoreModeListener>());
        }
    }

    public void SetColor(Color color)
    {
        foreach (Image img in baseData.Get<List<Sprite>>("tiles"))
        {
            img.Color = color;
        }
    }

    private void OnDisable()
    {
        SetColor(Color.Gray);
        Collidable = false;
    }

    private void OnEnable()
    {
        SetColor(Color.White);
        Collidable = true;
    }

    private bool IsRiding(Solid solid)
    {
        return Facing switch
        {
            Facings.Right => CollideCheckOutside(solid, Position + Vector2.UnitX),
            Facings.Left => CollideCheckOutside(solid, Position - Vector2.UnitX),
            _ => false,
        };
    }

    private void OnShake(Vector2 amount)
    {
        Shake += amount;
    }

    public override void Render()
    {
        Vector2 p = Position;
        Position += Shake;
        base.Render();
        Position = p;
    }

    [MonoModLinkTo("Monocle.Entity", "System.Void Added(Monocle.Scene)")]
    public void base_Added(Scene scene)
    {
        base.Added(scene);
    }

    public override void Added(Scene scene)
    {
        base_Added(scene);
        Session.CoreModes currentCoreMode = (scene as Level).CoreMode;
        bool notCoreMode = baseData.Get<bool>("notCoreMode");
        Session.CoreModes mode = coreModeBehavior switch
        {
            _ when notCoreMode => Session.CoreModes.Cold,
            CoreModeBehavior.ToggleDefaultHot => currentCoreMode switch { Session.CoreModes.Cold => Session.CoreModes.Cold, _ => Session.CoreModes.Hot },
            CoreModeBehavior.ToggleDefaultCold => currentCoreMode switch { Session.CoreModes.Hot => Session.CoreModes.Hot, _ => Session.CoreModes.Cold },
            CoreModeBehavior.AlwaysHot => Session.CoreModes.Hot,
            CoreModeBehavior.AlwaysCold => Session.CoreModes.Cold,
            _ => throw new ArgumentOutOfRangeException()
        };
        baseData.Invoke("OnChangeMode", mode);
    }

    #region Hooks

    private const string Player_attachedWallBoosterCurrentSpeed = "communalHelperAttachedWallBoosterCurrentSpeed";
    private const string Player_attachedWallBoosterLiftSpeedTimer = "communalHelperAttachedWallBoosterLiftSpeedTimer";
    private const string Player_lastWallBooster = "communalHelperLastWallBooster";

    internal static void Hook()
    {
        On.Celeste.WallBooster.ctor_EntityData_Vector2 += Mod_WallBooster_ctor_EntityData_Vector2;
        IL.Celeste.WallBooster.BuildSprite += Mod_WallBooster_BuildSprite;

        On.Celeste.Player.ClimbBegin += Player_ClimbBegin;
        IL.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
        On.Celeste.Player.ClimbJump += Player_ClimbJump;
        On.Celeste.Player.WallJump += Player_WallJump;
        On.Celeste.Player.WallBoosterCheck += Player_WallBoosterCheck;

        On.Celeste.Player.ctor += Player_ctor;
        On.Celeste.Player.Update += Player_Update;
    }

    internal static void Unhook()
    {
        On.Celeste.WallBooster.ctor_EntityData_Vector2 -= Mod_WallBooster_ctor_EntityData_Vector2;
        IL.Celeste.WallBooster.BuildSprite -= Mod_WallBooster_BuildSprite;

        On.Celeste.Player.ClimbBegin -= Player_ClimbBegin;
        IL.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
        On.Celeste.Player.ClimbJump -= Player_ClimbJump;
        On.Celeste.Player.WallJump -= Player_WallJump;
        On.Celeste.Player.WallBoosterCheck -= Player_WallBoosterCheck;

        On.Celeste.Player.ctor -= Player_ctor;
        On.Celeste.Player.Update -= Player_Update;
    }

    private static void Mod_WallBooster_ctor_EntityData_Vector2(On.Celeste.WallBooster.orig_ctor_EntityData_Vector2 orig, WallBooster self, EntityData data, Vector2 offset)
    {
        if (self is AttachedWallBooster wb)
            wb.legacyBoost = data.Bool("legacyBoost", true);

        orig(self, data, offset);
    }

    private static void Mod_WallBooster_BuildSprite(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchLdloc(2));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<string, WallBooster, string>>((id, wallBooster) =>
        {
            bool alt = wallBooster is AttachedWallBooster wb && !wb.legacyBoost;

            if (alt)
                id = id switch
                {
                    "WallBoosterTop" => "BadAttachedWallBoosterTop",
                    "WallBoosterBottom" => "BadAttachedWallBoosterBottom",
                    _ => id,
                };

            return id;
        });
    }

    private static WallBooster Player_WallBoosterCheck(On.Celeste.Player.orig_WallBoosterCheck orig, Player self)
    {
        WallBooster entity = orig(self);
        self.GetData().Set(Player_lastWallBooster, entity);
        return entity;
    }

    private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
    {
        orig(self, position, spriteMode);

        DynamicData data = self.GetData();
        data.Set(Player_attachedWallBoosterCurrentSpeed, 0f);
        data.Set(Player_attachedWallBoosterLiftSpeedTimer, 0f);
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        orig(self);

        DynamicData data = self.GetData();
        float timer = data.Get<float>(Player_attachedWallBoosterLiftSpeedTimer);
        if (timer > 0)
            data.Set(Player_attachedWallBoosterLiftSpeedTimer, Calc.Approach(timer, 0f, Engine.DeltaTime));
    }

    private static void Player_WallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir)
    {
        PlayerWallBoost(self);
        orig(self, dir);
    }

    private static void Player_ClimbJump(On.Celeste.Player.orig_ClimbJump orig, Player self)
    {
        PlayerWallBoost(self);
        orig(self);
    }

    private static void PlayerWallBoost(Player player)
    {
        DynamicData data = player.GetData();

        float timer = data.Get<float>(Player_attachedWallBoosterLiftSpeedTimer);
        float currentSpeed = data.Get<float>(Player_attachedWallBoosterCurrentSpeed);

        /*
         * So...
         * We want to apply an additionnal Y liftspeed boost to the player, when it is jumping from an AttachedWallBooster.
         * The issue is that, because AttachedWallBoosters ARE WallBoosters, the boost is already applied like for vanilla WallBoosters.
         * So why would we need an additionnal boost? Well, since AttachedWallBoosters are attached, they can move with the entity they are bound to
         * ... in which case, if the block is moving, the applied liftspeed to the player by the wall booster is completely cancelled.
         * Liftspeed doesn't take into account whatever previous liftspeed the actor had, it just replaces it.
         * When I was applying the liftspeed boost to the player, I wasn't checking if the player had already gotten it's wall booster boost,
         * which caused a double boost effect (indeed, -160px Y speed, instead of -80px).
         * Checking that a player is jumping from an AttachedWallBooster, that its wallbooster speed is still active, that it isn't an Ice Wall,
         * and MOST IMPORTANTLY that its Y liftspeed is positive or zero, is enough to know if we should apply an additional liftspeed Y boost.
         * 
         * Wall booster boosts now work with moving blocks.
         */
        if (timer > 0 && currentSpeed < 0 && data.Get<WallBooster>(Player_lastWallBooster) is AttachedWallBooster wb && !wb.IceMode)
        {
            if (player.LiftSpeed.Y >= 0 || !wb.legacyBoost)
                player.LiftSpeed += Vector2.UnitY * Calc.Max(currentSpeed, -80f);

            data.Set(Player_attachedWallBoosterCurrentSpeed, 0.0f);
            data.Set(Player_attachedWallBoosterLiftSpeedTimer, 0.0f);
        }
    }

    private static void Player_ClimbBegin(On.Celeste.Player.orig_ClimbBegin orig, Player self)
    {
        orig(self);
        self.GetData().Set(Player_attachedWallBoosterLiftSpeedTimer, 0.0f);
    }

    private static void Player_ClimbUpdate(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.MatchLdstr(SFX.char_mad_grab_letgo));
        cursor.GotoNext(MoveType.After, instr => instr.Match(OpCodes.Brfalse_S));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(PlayerWallBoost);

        cursor.GotoNext(instr => instr.MatchLdstr(SFX.game_09_conveyor_activate));
        cursor.GotoNext(instr => instr.MatchMul());
        cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Stfld);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(self =>
        {
            DynamicData data = self.GetData();
            data.Set(Player_attachedWallBoosterCurrentSpeed, self.Speed.Y);
            data.Set(Player_attachedWallBoosterLiftSpeedTimer, 0.25f);
        });
    }

    #endregion

}
