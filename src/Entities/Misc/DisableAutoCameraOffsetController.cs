using Celeste.Mod.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DisableAutoCameraOffsetController")]
[Tracked]
public class DisableAutoCameraOffsetController(EntityData data, Vector2 offset) : Entity(data.Position + offset)
{
    private readonly string flag = data.Attr("flag");

    private bool IsActive()
        => string.IsNullOrEmpty(flag) || SceneAs<Level>().Session.GetFlag(flag);
    
    #region Hooks
    
    private static ILHook ilHook_Player_get_CameraTarget;

    internal static void Load()
    {
        ilHook_Player_get_CameraTarget = new ILHook(typeof(Player).GetMethod("get_CameraTarget", BindingFlags.Public | BindingFlags.Instance)!, Player_get_CameraTarget);
    }

    internal static void Unload()
    {
        ilHook_Player_get_CameraTarget.Dispose();
        ilHook_Player_get_CameraTarget = null;
    }

    private static void Player_get_CameraTarget(ILContext il)
    {
        ILCursor cursor = new(il);
        
        // add check for controller being active to the check for StReflectionFall
        /*
         * IL_0062: ldarg.0
         * IL_0063: ldfld class Monocle.StateMachine Celeste.Player::StateMachine
         * IL_0068: callvirt instance int32 Monocle.StateMachine::get_State()
         * IL_0032: ldc.i4.s 18
         * IL_0034: beq.s IL_0062
         */
        if (!cursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Player>("StateMachine"),
            instr => instr.MatchCallvirt<StateMachine>("get_State"),
            instr => instr.MatchLdcI4(Player.StReflectionFall),
            instr => instr.MatchBeq(out _)))
            return;
        
        ILCursor setCameraOffsetLabelCursor = cursor.Clone();
        ILLabel setCameraOffset = setCameraOffsetLabelCursor.DefineLabel();
        
        /*
         * IL_0036: ldloc.1
         * IL_0037: ldarg.0
         * IL_0038: ldfld class Celeste.Level Celeste.Player::level
         * IL_003d: ldflda valuetype [FNA]Microsoft.Xna.Framework.Vector2 Celeste.Level::CameraOffset
         */
        if (!setCameraOffsetLabelCursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdloc(1),
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Player>("level"),
            instr => instr.MatchLdflda<Level>("CameraOffset")))
            return;
        
        setCameraOffsetLabelCursor.MarkLabel(setCameraOffset);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(IsControllerActive);
        cursor.Emit(OpCodes.Brtrue, setCameraOffset);
        
        // skip all the state-specific camera offsets if the controller is active
        /*
         * IL_0062: ldarg.0
         * IL_0063: ldfld class Monocle.StateMachine Celeste.Player::StateMachine
         * IL_0068: callvirt instance int32 Monocle.StateMachine::get_State()
         * IL_006d: ldc.i4.s 19
         * IL_006f: bne.un.s IL_00ae
         */
        if (!cursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<Player>("StateMachine"),
            instr => instr.MatchCallvirt<StateMachine>("get_State"),
            instr => instr.MatchLdcI4(Player.StStarFly),
            instr => instr.MatchBneUn(out _)))
            return;
        
        ILCursor skipStateOffsetLabelCursor = cursor.Clone();
        ILLabel skipStateOffset = skipStateOffsetLabelCursor.DefineLabel();

        /*
         * IL_013c: ldarg.0
         * IL_013d: ldflda valuetype [FNA]Microsoft.Xna.Framework.Vector2 Celeste.Player::CameraAnchorLerp
         * IL_0142: call instance float32 [FNA]Microsoft.Xna.Framework.Vector2::Length()
         * IL_0147: ldc.r4 0.0
         * IL_014c: ble.un IL_024d
         */
        if (!skipStateOffsetLabelCursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdflda<Player>("CameraAnchorLerp"),
            instr => instr.MatchCall<Vector2>("Length"),
            instr => instr.MatchLdcR4(0f),
            instr => instr.MatchBleUn(out _)))
            return;
        
        skipStateOffsetLabelCursor.MarkLabel(skipStateOffset);
        
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(IsControllerActive);
        cursor.Emit(OpCodes.Brtrue, skipStateOffset);
    }
    
    private static bool IsControllerActive(Player player)
        => player.Scene?.Tracker?.GetEntity<DisableAutoCameraOffsetController>() is { } controller && controller.IsActive();
    
    #endregion
}
