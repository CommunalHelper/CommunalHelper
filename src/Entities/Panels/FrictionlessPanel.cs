using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;
using Directions = Celeste.Spikes.Directions;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/FrictionlessPanel")]
[Tracked]
public class FrictionlessPanel : AbstractPanel
{
    public FrictionlessPanel(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        Depth = Depths.FakeWalls - 10;
    }

    #region Hooks

    internal static new void Load()
    {
        IL.Celeste.Solid.MoveHExact += Solid_MoveHExact;
        IL.Celeste.Solid.MoveVExact += Solid_MoveVExact;
    }

    internal static new void Unload()
    {
        IL.Celeste.Solid.MoveHExact -= Solid_MoveHExact;
        IL.Celeste.Solid.MoveVExact -= Solid_MoveVExact;
    }

    private static void Solid_MoveHExact(ILContext il)
    {
        ILCursor cursor = new(il)
        {
            Index = -1
        };
        if (cursor.TryGotoPrev(MoveType.After, instr => instr.MatchCallvirt(out MethodReference method) && method.Name == "Contains"))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, il.Body.Variables.First(v => v.VariableType.Name == "Actor"));
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<bool, Solid, Actor, int, bool>>((v, solid, actor, move) =>
            {
                if (v && actor is Player player && !(player.StateMachine.State == Player.StClimb))
                {
                    DynamicData solidData = DynamicData.For(solid);
                    List<StaticMover> staticMovers = solidData.Get<List<StaticMover>>("staticMovers");
                    foreach (StaticMover mover in staticMovers)
                    {
                        if (mover.Entity is FrictionlessPanel panel &&
                            panel.Orientation == Directions.Up &&
                            player.CollideCheck(mover.Entity, player.Position + new Vector2(move, 1)))
                        {
                            return false;
                        }
                    }
                }
                return v;
            });
        }
    }

    private static void Solid_MoveVExact(ILContext il)
    {
        ILCursor cursor = new(il)
        {
            Index = -1
        };
        if (cursor.TryGotoPrev(MoveType.After, instr => instr.MatchCallvirt(out MethodReference method) && method.Name == "Contains"))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, il.Body.Variables.First(v => v.VariableType.Name == "Actor"));
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<bool, Solid, Actor, int, bool>>((v, solid, actor, move) =>
            {
                if (v && actor is Player player && player.StateMachine.State == Player.StClimb)
                {
                    DynamicData solidData = DynamicData.For(solid);
                    List<StaticMover> staticMovers = solidData.Get<List<StaticMover>>("staticMovers");
                    foreach (StaticMover mover in staticMovers)
                    {
                        if (mover.Entity is FrictionlessPanel panel &&
                            panel.Orientation is Directions.Left or Directions.Right &&
                            player.CollideCheck(mover.Entity, player.Position + new Vector2((int) player.Facing, move)))
                        {
                            return false;
                        }
                    }
                }
                return v;
            });
        }
    }

    #endregion

}
