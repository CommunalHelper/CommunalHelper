using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities.ChainStuff {

    [CustomEntity("CommunalHelper/HardChain")]
    class HardChain : Chain {

        List<Entity> TargetedSolids = new List<Entity>();

        public HardChain(EntityData data, Vector2 offset) : base(data, offset) { }

        public override void Update() {
            CleanUpSolids();
            base.Update();
        }

        protected override void BreakInHalf() {
            // if we're too far, just break
            if (Vector2.Distance(Nodes[0].Position, Nodes[Nodes.Length - 1].Position) > ((Nodes.Length + 1) * distanceConstraint) + 20) {
                CleanUpSolids();
                base.BreakInHalf();
            } else {
                // try to stop both objects by adding targetted solids
                if (StartSolid != null)
                    Constrain(StartSolid, true);
                if (EndSolid != null)
                    Constrain(EndSolid, false);
            }
        }

        private void CleanUpSolids() {
            foreach (Entity item in TargetedSolids)
                item.RemoveSelf();
        }

        // TODO: non-Solid Entities?

        private void Constrain(Solid solid, bool start) {
            Func<Vector2> pos = start ? attachedEndGetter : attachedStartGetter;
            // compare corners to decide where to add boxes
            if (solid.Top < pos.Invoke().Y) {
                // add top solid
                AddSolid(solid.Position - new Vector2(0, 20), solid.Width, 20, solid);
            }
            if (solid.Bottom > pos.Invoke().Y) {
                // add bottom solid
                AddSolid(solid.Position + new Vector2(0, solid.Height), solid.Width, 20, solid);
            }
            if (solid.Left < pos.Invoke().X) {
                // add left solid
                AddSolid(solid.Position - new Vector2(20, 0), 20, solid.Height, solid);
            }
            if (solid.Right > pos.Invoke().X) {
                // add right solid
                AddSolid(solid.Position + new Vector2(solid.Width, 0), 20, solid.Height, solid);
            }
        }

        private void AddSolid(Vector2 position, float width, float height, Entity target) {
            TargetedSolid solid = new TargetedSolid(position, width, height, target);
            Scene.Add(solid);
            TargetedSolids.Add(solid);
        }

        #region Targeted Solid

        private static readonly Dictionary<Entity, List<TargetedSolid>> Solids = new Dictionary<Entity, List<TargetedSolid>>();

        class TargetedSolid : Solid {

            public Entity target;

            public TargetedSolid(Vector2 position, float width, float height, Entity target) : base(position, width, height, false) {
                this.target = target;
                Visible = Collidable = false;
                if (!Solids.ContainsKey(target))
                    Solids[target] = new List<TargetedSolid>();
                Solids[target].Add(this);
            }

            public override void Removed(Scene scene) {
                base.Removed(scene);
                Solids[target].Remove(this);
                if (Solids[target].Count == 0)
                    Solids.Remove(target);
            }
        }

        #endregion
        #region Hooks

        private static Entity current = null;

        internal static void Load() {
            On.Monocle.Entity.Update += MakeTargetedSolidsCollidable;
            On.Celeste.Actor.MoveHExact += RecheckTargetedSolidsH;
            On.Celeste.Actor.MoveVExact += RecheckTargetedSolidsV;
        }

        internal static void Unload() {
            On.Monocle.Entity.Update -= MakeTargetedSolidsCollidable;
            On.Celeste.Actor.MoveHExact -= RecheckTargetedSolidsH;
            On.Celeste.Actor.MoveVExact -= RecheckTargetedSolidsV;
        }

        private static void MakeTargetedSolidsCollidable(On.Monocle.Entity.orig_Update orig, Entity self) {
            List<TargetedSolid> ts = Solids.ContainsKey(self) ? Solids[self] : null;
            current = self;
            if (ts != null)
                foreach (TargetedSolid item in ts)
                    item.Collidable = true;
            orig(self);
            if (ts != null)
                foreach (TargetedSolid item in ts)
                    item.Collidable = false;
        }

        private static bool RecheckTargetedSolidsV(On.Celeste.Actor.orig_MoveVExact orig, Actor self, int moveV, Collision onCollide, Solid pusher) {
            List<TargetedSolid> ats = Solids.ContainsKey(self) ? Solids[self] : null;
            List<TargetedSolid> cts = Solids.ContainsKey(current) ? Solids[current] : null;
            if (cts != null)
                foreach (TargetedSolid item in cts)
                    item.Collidable = false;
            if (ats != null)
                foreach (TargetedSolid item in ats)
                    item.Collidable = true;
            bool ret = orig(self, moveV, onCollide, pusher);
            if (ats != null)
                foreach (TargetedSolid item in ats)
                    item.Collidable = false;
            if (cts != null)
                foreach (TargetedSolid item in cts)
                    item.Collidable = true;
            return ret;
        }

        private static bool RecheckTargetedSolidsH(On.Celeste.Actor.orig_MoveHExact orig, Actor self, int moveH, Collision onCollide, Solid pusher) {
            List<TargetedSolid> ats = Solids.ContainsKey(self) ? Solids[self] : null;
            List<TargetedSolid> cts = Solids.ContainsKey(current) ? Solids[current] : null;
            if (cts != null)
                foreach (TargetedSolid item in cts)
                    item.Collidable = false;
            if (ats != null)
                foreach (TargetedSolid item in ats)
                    item.Collidable = true;
            bool ret = orig(self, moveH, onCollide, pusher);
            if (ats != null)
                foreach (TargetedSolid item in ats)
                    item.Collidable = false;
            if (cts != null)
                foreach (TargetedSolid item in cts)
                    item.Collidable = true;
            return ret;
        }
        #endregion
    }
}
