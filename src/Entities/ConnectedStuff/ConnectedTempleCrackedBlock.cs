using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper.Entities.ConnectedStuff {

    [Tracked]
    [CustomEntity("CommunalHelper/ConnectedTempleCrackedBlock")]
    public class ConnectedTempleCrackedBlock : ConnectedSolid {

        protected static MTexture[,] masterInnerCorners = new MTexture[2, 2];
        protected static List<MTexture> atlasSubtextures;

        // Hooks & stuff

        private static FieldInfo Fieldinfo_SeekerRegen_this;
        private static ILHook Seeker_Regen_Hook;

        public static void Load() {
            HookSeekerRegen();
            On.Celeste.Seeker.SlammedIntoWall += OnSeekerBonk;
            On.Celeste.Puffer.Explode += OnPufferExplode;
            On.Celeste.PlayerSeeker.OnCollide += OnPlayerSeekerCollide;
        }

        public static void Unload() {
            Seeker_Regen_Hook?.Dispose();
            Seeker_Regen_Hook = null;
            On.Celeste.Seeker.SlammedIntoWall -= OnSeekerBonk;
            On.Celeste.Puffer.Explode -= OnPufferExplode;
            On.Celeste.PlayerSeeker.OnCollide -= OnPlayerSeekerCollide;
        }

        private static void HookSeekerRegen() {
            MethodInfo minfo = typeof(Seeker).GetMethod("RegenerateCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
            Fieldinfo_SeekerRegen_this = minfo.GetStateMachineTarget().DeclaringType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
            Seeker_Regen_Hook = new ILHook(minfo.GetStateMachineTarget(), ILSetupSeekerRegen);
        }

        private static void ILSetupSeekerRegen(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(instr => instr.Next?.MatchLdfld<Seeker>("physicsHitbox") ?? false)) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, Fieldinfo_SeekerRegen_this);
                cursor.Emit(OpCodes.Call, typeof(ConnectedTempleCrackedBlock).GetMethod("OnSeekerRegen"));
            }
        }

        public static void OnSeekerRegen(Seeker self) {
            foreach (ConnectedTempleCrackedBlock entity in self.Scene.Tracker.GetEntities<ConnectedTempleCrackedBlock>()) {
                if (self.CollideCheck(entity)) {
                    entity.Break(self.Position);
                }
            }
        }

        private static void OnSeekerBonk(On.Celeste.Seeker.orig_SlammedIntoWall orig, Seeker self, CollisionData data) {
            foreach (ConnectedTempleCrackedBlock entity in self.Scene.Tracker.GetEntities<ConnectedTempleCrackedBlock>()) {
                if (self.CollideCheck(entity, self.Position + Vector2.UnitX * Math.Sign(self.Speed.X))) {
                    entity.Break(self.Center);
                }
            }
            orig(self, data);
        }

        private static void OnPufferExplode(On.Celeste.Puffer.orig_Explode orig, Puffer self) {
            DynamicData dd = new DynamicData(self);
            Collider collider = self.Collider;
            self.Collider = dd.Get<Collider>("pushRadius");
            foreach (ConnectedTempleCrackedBlock entity in self.Scene.Tracker.GetEntities<ConnectedTempleCrackedBlock>()) {
                if (self.CollideCheck(entity)) {
                    entity.Break(self.Position);
                }
            }
            self.Collider = collider;
            orig(self);
        }

        private static void OnPlayerSeekerCollide(On.Celeste.PlayerSeeker.orig_OnCollide orig, PlayerSeeker self, CollisionData data) {
            orig(self, data);
            DynamicData dd = new DynamicData(self);
            if (dd.Get<float>("dashTimer") > 0f && data.Hit is ConnectedTempleCrackedBlock) {
                Celeste.Freeze(0.15f);
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
                (data.Hit as ConnectedTempleCrackedBlock).Break(self.Position);
            }
        }

        // Grouping stuff

#pragma warning disable CS0649 // Remove "Field is never assigned to"
        // private variable is never assigned to warning is a false positive. It is assigned to in DoGrouping.
        private ConnectedTempleCrackedBlock master;
        private List<ConnectedTempleCrackedBlock> group;
#pragma warning restore CS0649

        private void DoGrouping(ConnectedTempleCrackedBlock parent) {
            if (master != null && master == parent.master) {  // Already in this group
                return;
            }

            // Set the master block
            master = parent.master ?? this;
            if (master.group == null) {
                master.group = new List<ConnectedTempleCrackedBlock>();
            }
            if (!master.group.Contains(this)) {
                master.group.Add(this);
            }
            // If this used to be a master, merge into the new one
            if (group != null && master != this) {
                foreach (ConnectedTempleCrackedBlock blk in group) {
                    if (!master.group.Contains(blk)) {
                        master.group.Add(blk);
                    }
                }
                group = null;
            }

            // handle group bounds shenanigans
            if (X < master.GroupBoundsMin.X) {
                master.GroupBoundsMin.X = (int)X;
            }
            if (Y < master.GroupBoundsMin.Y) {
                master.GroupBoundsMin.Y = (int)Y;
            }
            if (Right > master.GroupBoundsMax.X) {
                master.GroupBoundsMax.X = (int)Right;
            }
            if (Bottom > master.GroupBoundsMax.Y) {
                master.GroupBoundsMax.Y = (int)Bottom;
            }

            // propagate to new blocks
            foreach (Entity entity in Scene.Tracker.GetEntities<ConnectedTempleCrackedBlock>()) {
                ConnectedTempleCrackedBlock connectedBlock = (ConnectedTempleCrackedBlock) entity;
                if (connectedBlock.persistent == persistent &&
                    Scene.CollideCheck(new Rectangle((int) X, (int) Y, (int) Width, (int) Height), connectedBlock)) {
                    connectedBlock.DoGrouping(this);
                }
            }
        }

        public void Break(Vector2 from) {
            Audio.Play("event:/game/05_mirror_temple/crackedwall_vanish", base.Center);
            // Propagate to all in group
            foreach (ConnectedTempleCrackedBlock block in master.group) {
                block.IndividualBreak(from);
            }
        }

        // Base Entity

        private MTexture[,,] texture;
        private bool autoTiled = false;
        private bool[,] groupGrid;

        private EntityID eid;
        private bool persistent;
        private Tuple<int,int>[,] tiles;
        private float frame;
        private bool broken;
        private int frames;

        public ConnectedTempleCrackedBlock(EntityData data, Vector2 offset)
            : this(new EntityID(data.Level.Name, data.ID), data, offset) {
        }

        public ConnectedTempleCrackedBlock(EntityID eid, EntityData data, Vector2 offset)
            : this(eid, data.Position + offset, data.Width, data.Height, data.Bool("persistent")) {
        }

        public ConnectedTempleCrackedBlock(EntityID eid, Vector2 position, int width, int height, bool persistent)
            : base(position, width, height, safe: true) {
            const int tilesetW = 7, tilesetH = 6;

            this.eid = eid;
            this.persistent = persistent;
            group = null;
            Collidable = (Visible = false);
            int tilesW = (int) (width / 8f);
            int tilesH = (int) (height / 8f);
            List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/connectedTempleCrackedBlock/breakBlock");
            tiles = new Tuple<int, int>[tilesW, tilesH];
            frames = atlasSubtextures.Count;
            texture = new MTexture[tilesetW, tilesetH, frames];
            for (int tx = 0; tx < tilesetW; tx++) {
                for (int ty = 0; ty < tilesetH; ty++) {
                    for (int k = 0; k < frames; k++) {
                        texture[tx, ty, k] = atlasSubtextures[k].GetSubtexture(tx * 8, ty * 8, 8, 8);
                    }
                }
            }
            Add(new LightOcclude(0.5f));
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (CollideCheck<Player>()) {
                if (persistent) {
                    SceneAs<Level>().Session.DoNotLoad.Add(eid);
                }
                RemoveSelf();
            } else {
                Collidable = (Visible = true);
            }

            DoGrouping(this);
        }

        public override void Update() {
            base.Update();
            if (broken) {
                frame += Engine.DeltaTime * 15f;
                if (frame >= (float) frames) {
                    RemoveSelf();
                }
            }
        }

        public override void Render() {
            if (!autoTiled) {
                AutoTile(texture);
            }
            int num = (int) frame;
            if (num >= frames) {
                return;
            }
            for (int i = 0; (float) i < Width / 8f; i++) {
                for (int j = 0; (float) j < Height / 8f; j++) {
                    Tuple<int, int> tile = tiles[i, j];
                    if (tile != null) {
                        texture[tile.Item1, tile.Item2, num].Draw(Position + new Vector2(i, j) * 8f);
                    }
                }
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (master == this) {
                int tWidth = (int) (GroupBoundsMax.X - GroupBoundsMin.X) / 8;
                int tHeight = (int) (GroupBoundsMax.Y - GroupBoundsMin.Y) / 8;
                groupGrid = new bool[tWidth, tHeight];
                foreach (ConnectedTempleCrackedBlock blk in group) {
                    int offsX = (int) (blk.X - GroupBoundsMin.X) / 8;
                    int offsY = (int) (blk.Y - GroupBoundsMin.Y) / 8;
                    for (int x = 0; x < blk.Width / 8; x++) {
                        for (int y = 0; y < blk.Height / 8; y++) {
                            groupGrid[x + offsX, y + offsY] = true;
                        }
                    }
                }
            }
        }

        private void IndividualBreak(Vector2 from) {
            if (persistent) {
                SceneAs<Level>().Session.DoNotLoad.Add(eid);
            }
            broken = true;
            Collidable = false;
            for (int i = 0; (float) i < base.Width / 8f; i++) {
                for (int j = 0; (float) j < base.Height / 8f; j++) {
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(Position + new Vector2(i * 8 + 4, j * 8 + 4), '1', playSound: true).BlastFrom(from));
                }
            }
        }

        // Tiling

        private void AutoTile(MTexture[,,] tex) {
            //int tWidth = (int) (Width / 8);
            //int tHeight = (int) (Height / 8);
            int tWidth = (int) ((master.GroupBoundsMax.X - master.GroupBoundsMin.X) / 8);
            int tHeight = (int) ((master.GroupBoundsMax.Y - master.GroupBoundsMin.Y) / 8);

            Tuple<int, int>[,] res = new Tuple<int, int>[tWidth, tHeight];
            int offsX = (int)(X - master.GroupBoundsMin.X) / 8;
            int offsY = (int)(Y - master.GroupBoundsMin.Y) / 8;
            for (int x = 0; x < tWidth; x++) {
                for (int y = 0; y < tHeight; y++) {
                    if (GetGridSafe(master.groupGrid, x + offsX, y + offsY)) {
                        res[x, y] = GetTile(master.groupGrid, x + offsX, y + offsY, tex);
                    }
                }
            }

            tiles = res;
            autoTiled = true;
        }

        private Tuple<int, int> GetTile(bool[,] grid, int x, int y, MTexture[,,] tex) {
            bool up = GetGridSafe(grid, x, y-1), down = GetGridSafe(grid, x, y+1),
                left = GetGridSafe(grid, x-1, y), right = GetGridSafe(grid, x+1, y),
                upleft = GetGridSafe(grid, x-1, y-1), upright = GetGridSafe(grid, x+1, y-1),
                downleft = GetGridSafe(grid, x-1, y+1), downright = GetGridSafe(grid, x+1, y+1),
                farUp = GetGridSafe(grid, x, y-2), farDown = GetGridSafe(grid, x, y+2),
                farLeft = GetGridSafe(grid, x-2, y), farRight = GetGridSafe(grid, x+2, y);

            // Check for inner corners
            if (up && down && left && right) {
                if (!upleft) {
                    if (downright)
                        return new Tuple<int, int>(6, 1);  // inner corner upleft
                    return new Tuple<int, int>(6, 4);  // double inner corner ul/dr
                }
                if (!upright) {
                    if (downleft)
                        return new Tuple<int, int>(6, 0);  // inner corner upright
                    return new Tuple<int, int>(6, 5);  // double inner corner ur/dl
                }
                if (!downleft)
                    return new Tuple<int, int>(6, 2);  // inner corner downleft
                if (!downright)
                    return new Tuple<int, int>(6, 3);  // inner corner downright
            }

            int tx, ty;

            // Get X coord
            if (!left)
                tx = 0;
            else if (!right)
                tx = 5;
            else if (!farLeft)
                tx = 1;
            else if (!farRight)
                tx = 4;
            else
                tx = 2 + x % 2;

            // Get Y coord
            if (!up)
                ty = 0;
            else if (!down)
                ty = 5;
            else if (!farUp)
                ty = 1;
            else if (!farDown)
                ty = 4;
            else
                ty = 2 + y % 2;

            return new Tuple<int, int>(tx, ty);
        }

        private bool GetGridSafe(bool[,] grid, int x, int y) {
            if (grid == null)
                return false;
            if (x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1)) {
                return false;
            }
            return grid[x, y];
        }
    }
}
