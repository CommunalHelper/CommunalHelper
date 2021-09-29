using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Directions = Celeste.Spikes.Directions;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/FrictionlessPanel")]
    [Tracked]
    public class FrictionlessPanel : AbstractPanel {

        public FrictionlessPanel(EntityData data, Vector2 offset)
            : base(data, offset) {
            Depth = Depths.FakeWalls - 10;
        }

        #region Hooks

        new internal static void Load() {
            IL.Celeste.Solid.MoveHExact += Solid_MoveHExact;
            IL.Celeste.Solid.MoveVExact += Solid_MoveVExact;
        }

        new internal static void Unload() {
            IL.Celeste.Solid.MoveHExact -= Solid_MoveHExact;
            IL.Celeste.Solid.MoveVExact -= Solid_MoveVExact;
        }

        private static void Solid_MoveHExact(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt(out MethodReference method) && method.Name == "Contains"))
                ;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, il.Body.Variables.First(v => v.VariableType.Name == "Actor"));
            cursor.EmitDelegate<Func<bool, Solid, Actor, bool>>((v, solid, actor) => {
                if (v && actor is Player player && !(player.StateMachine.State == Player.StClimb)) {
                    DynData<Solid> solidData = new DynData<Solid>(solid);
                    List<StaticMover> staticMovers = solidData.Get<List<StaticMover>>("staticMovers");
                    foreach (StaticMover mover in staticMovers) {
                        if (mover.Entity is FrictionlessPanel panel && 
                            panel.Orientation == Directions.Up && 
                            player.CollideCheck(mover.Entity, player.Position + Vector2.UnitY)) {
                            return false;
                        }
                    }
                }
                return v;
            });
        }

        private static void Solid_MoveVExact(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt(out MethodReference method) && method.Name == "Contains"))
                ;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, il.Body.Variables.First(v => v.VariableType.Name == "Actor"));
            cursor.EmitDelegate<Func<bool, Solid, Actor, bool>>((v, solid, actor) => {
                if (v && actor is Player player && player.StateMachine.State == Player.StClimb) {
                    DynData<Solid> solidData = new DynData<Solid>(solid);
                    List<StaticMover> staticMovers = solidData.Get<List<StaticMover>>("staticMovers");
                    foreach (StaticMover mover in staticMovers) {
                        if (mover.Entity is FrictionlessPanel panel &&
                            panel.Orientation is Directions.Left or Directions.Right &&
                            player.CollideCheck(mover.Entity, player.Position + Vector2.UnitX * (int) player.Facing)) {
                            return false;
                        }
                    }
                }
                return v;
            });
        }

        #endregion

    }
}