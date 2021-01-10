using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;

namespace Celeste.Mod.CommunalHelper.Entities.Misc {

    [CustomEntity(new string[]
    {
        "CommunalHelper/TimedTriggerSpikesUp = LoadUp",
        "CommunalHelper/TimedTriggerSpikesDown = LoadDown",
        "CommunalHelper/TimedTriggerSpikesLeft = LoadLeft",
        "CommunalHelper/TimedTriggerSpikesRight = LoadRight"
    })]
    public class TimedTriggerSpikes : Entity {
        public enum Directions {
            Up,
            Down,
            Left,
            Right
        }

        protected struct SpikeInfo {
            public TimedTriggerSpikes Parent;

            public int Index;

            public int TextureIndex;

            public Vector2 Position;

            public bool Triggered;

            public float RetractTimer;

            public float DelayTimer;

            public float Lerp;

            public void Update() {
                if (Parent.Grouped ? Parent.Triggered : Triggered) {
                    if (DelayTimer > 0f) {
                        DelayTimer -= Engine.DeltaTime;
                        if (DelayTimer <= 0f) {
                            if (PlayerCheck()) {
                                DelayTimer = 0.05f;
                            } else {
                                Audio.Play("event:/game/03_resort/fluff_tendril_emerge", Parent.Position + Position);
                            }
                        }
                    } else {
                        Lerp = Calc.Approach(Lerp, 1f, 8f * Engine.DeltaTime);
                    }
                } else {
                    Lerp = Calc.Approach(Lerp, 0f, 4f * Engine.DeltaTime);
                    if (Lerp <= 0f) {
                        Triggered = false;
                    }
                }
            }

            public bool PlayerCheck() {
                return Parent.PlayerCheck(Index);
            }

            public bool OnPlayer(Player player, Vector2 outwards) {
                if (Parent.Grouped ? !Parent.Triggered : !Triggered) {
                    Audio.Play("event:/game/03_resort/fluff_tendril_touch", Parent.Position + Position);
                    if (Parent.Grouped) { Parent.Triggered = true; } else { Triggered = true; }
                    RetractTimer = 6f;
                    return false;
                }
                if (Lerp >= 1f) {
                    player.Die(outwards);
                    return true;
                }
                return false;
            }
        }

        private const float RetractTime = 6f;

        protected float Delay = 0.4f;

        private bool waitForPlayer;

        private int size;

        private Directions direction;

        private string overrideType;

        private PlayerCollider pc;

        private Vector2 outwards;

        private Vector2 shakeOffset;

        private string spikeType;

        private SpikeInfo[] spikes;

        private List<MTexture> spikeTextures;

        

        private bool grouped = false;
        protected bool Grouped {
            get {
                return grouped && CommunalHelperModule.MaxHelpingHandLoaded;
            }
        }
        protected bool Triggered = false;

        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            return new TimedTriggerSpikes(entityData, offset, Directions.Up);
        }

        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            return new TimedTriggerSpikes(entityData, offset, Directions.Down);
        }

        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            return new TimedTriggerSpikes(entityData, offset, Directions.Left);
        }

        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            return new TimedTriggerSpikes(entityData, offset, Directions.Right);
        }

        public TimedTriggerSpikes(EntityData data, Vector2 offset, Directions dir)
            : this(data.Position + offset, GetSize(data, dir), dir, data.Attr("type", "default"), data.Float("Delay", 0.4f), data.Bool("WaitForPlayer", false), data.Bool("Grouped", false)) {
        }

        public TimedTriggerSpikes(Vector2 position, int size, Directions direction, string overrideType, float Delay, bool waitForPlayer, bool grouped)
            : base(position) {
            if (grouped && !CommunalHelperModule.MaxHelpingHandLoaded) {
                throw new Exception("Grouped Timed Trigger Spikes attempted to load without Max's Helping Hand as a dependency.");
            }
            this.size = size;
            this.direction = direction;
            this.overrideType = overrideType;
            this.Delay = Delay;
            this.waitForPlayer = waitForPlayer;
            switch (direction) {
                case Directions.Up:
                    outwards = new Vector2(0f, -1f);
                    base.Collider = new Hitbox(size, 3f, 0f, -3f);
                    Add(new SafeGroundBlocker());
                    Add(new LedgeBlocker(UpSafeBlockCheck));
                    break;
                case Directions.Down:
                    outwards = new Vector2(0f, 1f);
                    base.Collider = new Hitbox(size, 3f);
                    break;
                case Directions.Left:
                    outwards = new Vector2(-1f, 0f);
                    base.Collider = new Hitbox(3f, size, -3f);
                    Add(new SafeGroundBlocker());
                    Add(new LedgeBlocker(SideSafeBlockCheck));
                    break;
                case Directions.Right:
                    outwards = new Vector2(1f, 0f);
                    base.Collider = new Hitbox(3f, size);
                    Add(new SafeGroundBlocker());
                    Add(new LedgeBlocker(SideSafeBlockCheck));
                    break;
            }
            Add(pc = new PlayerCollider(OnCollide));
            Add(new StaticMover {
                OnShake = OnShake,
                SolidChecker = IsRiding,
                JumpThruChecker = IsRiding
            });
            base.Depth = -50;
            this.grouped = grouped;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            AreaData areaData = AreaData.Get(scene);
            spikeType = areaData.Spike;
            if (!string.IsNullOrEmpty(overrideType) && overrideType != "default") {
                spikeType = overrideType;
            }
            string str = direction.ToString().ToLower();
            if (spikeType == "tentacles") {
                throw new NotSupportedException("Trigger tentacles currently not supported");
            }
            spikes = new SpikeInfo[size / 8];
            spikeTextures = GFX.Game.GetAtlasSubtextures("danger/spikes/" + spikeType + "_" + str);
            for (int i = 0; i < spikes.Length; i++) {
                spikes[i].Parent = this;
                spikes[i].Index = i;
                switch (direction) {
                    case Directions.Up:
                        spikes[i].Position = Vector2.UnitX * ((float) i + 0.5f) * 8f + Vector2.UnitY;
                        break;
                    case Directions.Down:
                        spikes[i].Position = Vector2.UnitX * ((float) i + 0.5f) * 8f - Vector2.UnitY;
                        break;
                    case Directions.Left:
                        spikes[i].Position = Vector2.UnitY * ((float) i + 0.5f) * 8f + Vector2.UnitX;
                        break;
                    case Directions.Right:
                        spikes[i].Position = Vector2.UnitY * ((float) i + 0.5f) * 8f - Vector2.UnitX;
                        break;
                }
                spikes[i].DelayTimer = Delay;
            }
        }

        private void OnShake(Vector2 amount) {
            shakeOffset += amount;
        }

        private bool UpSafeBlockCheck(Player player) {
            int num = 8 * (int) player.Facing;
            int num2 = (int) ((player.Left + (float) num - base.Left) / 4f);
            int num3 = (int) ((player.Right + (float) num - base.Left) / 4f);
            if (num3 < 0 || num2 >= spikes.Length) {
                return false;
            }
            num2 = Math.Max(num2, 0);
            num3 = Math.Min(num3, spikes.Length - 1);
            for (int i = num2; i <= num3; i++) {
                if (spikes[i].Lerp >= 1f) {
                    return true;
                }
            }
            return false;
        }

        private bool SideSafeBlockCheck(Player player) {
            int num = (int) ((player.Top - base.Top) / 4f);
            int num2 = (int) ((player.Bottom - base.Top) / 4f);
            if (num2 < 0 || num >= spikes.Length) {
                return false;
            }
            num = Math.Max(num, 0);
            num2 = Math.Min(num2, spikes.Length - 1);
            for (int i = num; i <= num2; i++) {
                if (spikes[i].Lerp >= 1f) {
                    return true;
                }
            }
            return false;
        }

        private void OnCollide(Player player) {
            GetPlayerCollideIndex(player, out int minIndex, out int maxIndex);
            if (maxIndex >= 0 && minIndex < spikes.Length) {
                minIndex = Math.Max(minIndex, 0);
                maxIndex = Math.Min(maxIndex, spikes.Length - 1);
                for (int i = minIndex; i <= maxIndex && !spikes[i].OnPlayer(player, outwards); i++) {
                }
            }
        }

        private void GetPlayerCollideIndex(Player player, out int minIndex, out int maxIndex) {
            minIndex = (maxIndex = -1);
            switch (direction) {
                case Directions.Up:
                    if (player.Speed.Y >= 0f) {
                        minIndex = (int) ((player.Left - base.Left) / 8f);
                        maxIndex = (int) ((player.Right - base.Left) / 8f);
                    }
                    break;
                case Directions.Down:
                    if (player.Speed.Y <= 0f) {
                        minIndex = (int) ((player.Left - base.Left) / 8f);
                        maxIndex = (int) ((player.Right - base.Left) / 8f);
                    }
                    break;
                case Directions.Left:
                    if (player.Speed.X >= 0f) {
                        minIndex = (int) ((player.Top - base.Top) / 8f);
                        maxIndex = (int) ((player.Bottom - base.Top) / 8f);
                    }
                    break;
                case Directions.Right:
                    if (player.Speed.X <= 0f) {
                        minIndex = (int) ((player.Top - base.Top) / 8f);
                        maxIndex = (int) ((player.Bottom - base.Top) / 8f);
                    }
                    break;
            }
        }

        private bool PlayerCheck(int spikeIndex) {
            Player player = CollideFirst<Player>();
            if (player == null || !waitForPlayer) {
                return false;
            }
            GetPlayerCollideIndex(player, out int minIndex, out int maxIndex);
            if (minIndex <= spikeIndex + 1) {
                return maxIndex >= spikeIndex - 1;
            }
            return false;
        }

        private static int GetSize(EntityData data, Directions dir) {
            if (dir <= Directions.Down) {
                return data.Width;
            }
            return data.Height;
        }

        public override void Update() {
            base.Update();
            for (int i = 0; i < spikes.Length; i++) {
                spikes[i].Update();
            }
        }

        public override void Render() {
            base.Render();
            Vector2 justify = Vector2.One * 0.5f;
            switch (direction) {
                case Directions.Up:
                    justify = new Vector2(0.5f, 1f);
                    break;
                case Directions.Down:
                    justify = new Vector2(0.5f, 0f);
                    break;
                case Directions.Left:
                    justify = new Vector2(1f, 0.5f);
                    break;
                case Directions.Right:
                    justify = new Vector2(0f, 0.5f);
                    break;
            }
            for (int i = 0; i < spikes.Length; i++) {
                MTexture mTexture = spikeTextures[spikes[i].TextureIndex];
                Vector2 position = Position + shakeOffset + spikes[i].Position + outwards * (-4f + spikes[i].Lerp * 4f);
                mTexture.DrawJustified(position, justify);
            }
        }

        private bool IsRiding(Solid solid) {
            switch (direction) {
                case Directions.Up:
                    return CollideCheckOutside(solid, Position + Vector2.UnitY);
                case Directions.Down:
                    return CollideCheckOutside(solid, Position - Vector2.UnitY);
                case Directions.Left:
                    return CollideCheckOutside(solid, Position + Vector2.UnitX);
                case Directions.Right:
                    return CollideCheckOutside(solid, Position - Vector2.UnitX);
                default:
                    return false;
            }
        }

        private bool IsRiding(JumpThru jumpThru) {
            if (direction == Directions.Up) {
                return CollideCheck(jumpThru, Position + Vector2.UnitY);
            }
            return false;
        }
    }
}
