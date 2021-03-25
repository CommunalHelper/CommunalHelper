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

/*
* Slow routine: Particles spray out from each end diagonally, moving inwards
* Fast routine: Particles spray outwards + diagonally from the ends
* Try to keep the timing on these the same as for DreamBlocks
* 
* Todo:
* Add Feather particles/functionality
* Add Dreamblock activate/deactivate routines
* Two modes, one uses deactivated texture and blocks dashcollides, other fades away and does not block
* Add support for PandorasBox DreamDash controller
* Add OneUse mode
* Add custom Depth support
*/

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamTunnelEntry = LoadDreamTunnelEntry")]
    [Tracked]
    public class FrictionlessPanel : Entity {

        #region Loading

        public static Entity LoadDreamTunnelEntry(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            Directions orientation = entityData.Enum<Directions>("orientation");
            return new FrictionlessPanel(entityData.Position + offset, GetSize(entityData, orientation), orientation);
        }

        private static int GetSize(EntityData data, Directions dir) {
            if (dir <= Directions.Down) {
                return data.Width;
            }
            return data.Height;
        }

        #endregion

        public Directions Orientation;

        private StaticMover staticMover;

        public Vector2 Start => new Vector2(
                X + (Orientation is Directions.Right or Directions.Down ? Width : 0),
                Y + (Orientation is Directions.Left or Directions.Down ? Height : 0));
        public Vector2 End => new Vector2(
                X + (Orientation is Directions.Up or Directions.Right ? Width : 0),
                Y + (Orientation is Directions.Right or Directions.Down ? Height : 0));

        public Vector2 Shake => shake + platformShake;
        private Vector2 shake; // For use in Activation/Deactivation routines
        private Vector2 platformShake;

        public float Alpha = 1f;

        public FrictionlessPanel(Vector2 position, float size, Directions orientation)
            : base(position) {
            Depth = Depths.FakeWalls - 10;
            Orientation = orientation;

            Collider = orientation switch {
                Directions.Up => new Hitbox(size, 8f),
                Directions.Down => new Hitbox(size, 8f),
                Directions.Left => new Hitbox(8f, size),
                Directions.Right => new Hitbox(8f, size),
                _ => null
            };

            Add(staticMover = new StaticMover {
                OnShake = v => platformShake += v,
                SolidChecker = IsRiding,
                OnEnable = () => Active = Visible = Collidable = true,
                OnDisable = () => Active = Visible = Collidable = false
            });
        }

        // Make sure at least one side aligns, and the rest are contained within the solid
        private bool IsRiding(Solid solid) {
            return Orientation switch {
                Directions.Up => this.CollideCheckOutsideInside(solid, TopCenter - Vector2.UnitY * Height),
                Directions.Down => this.CollideCheckOutsideInside(solid, BottomCenter + Vector2.UnitY),
                Directions.Left => this.CollideCheckOutsideInside(solid, CenterLeft - Vector2.UnitX * Width),
                Directions.Right => this.CollideCheckOutsideInside(solid, CenterRight + Vector2.UnitX),
                _ => false,
            };
        }

        public override void Update() {
            if (staticMover.Platform == null) {
                RemoveSelf();
                return;
            }

            base.Update();
        }

        #region Hooks

        internal static void Load() {
            IL.Celeste.Solid.MoveHExact += Solid_MoveHExact;
            IL.Celeste.Solid.MoveVExact += Solid_MoveVExact;
        }

        internal static void Unload() {
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
                if (!(actor is Player player && player.StateMachine.State == Player.StClimb)) { 
                    DynData<Solid> solidData = new DynData<Solid>(solid);
                    List<StaticMover> staticMovers = solidData.Get<List<StaticMover>>("staticMovers");
                    foreach (StaticMover mover in staticMovers) {
                        if (mover.Entity is FrictionlessPanel panel && panel.Orientation is Directions.Up or Directions.Down) {
                            if (actor.CollideCheck(mover.Entity)) {
                                return false;
                            }
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
                if (actor is Player player && player.StateMachine.State == Player.StClimb) {
                    DynData<Solid> solidData = new DynData<Solid>(solid);
                    List<StaticMover> staticMovers = solidData.Get<List<StaticMover>>("staticMovers");
                    foreach (StaticMover mover in staticMovers) {
                        if (mover.Entity is FrictionlessPanel panel && panel.Orientation is Directions.Left or Directions.Right) {
                            if (player.CollideCheck(mover.Entity, player.Position + new Vector2((int) player.Facing, 4f)) ||
                                player.CollideCheck(mover.Entity, player.Position + new Vector2((int) player.Facing, -4f))) {
                                return false;
                            }
                        }
                    }
                }
                return v;
            });
        }

        #endregion

    }
}