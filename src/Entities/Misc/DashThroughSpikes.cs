using Celeste.Mod.CommunalHelper.Entities.DreamStuff;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper.Entities.Misc {

    [CustomEntity(new string[]
    {
        "CommunalHelper/DashThroughSpikesUp = LoadUp",
        "CommunalHelper/DashThroughSpikesDown = LoadDown",
        "CommunalHelper/DashThroughSpikesLeft = LoadLeft",
        "CommunalHelper/DashThroughSpikesRight = LoadRight"
    })]
    class DashThroughSpikes : Spikes {

        protected bool DreamDash = false;
        protected bool DreamVisuals = false;

        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new DashThroughSpikes(entityData, offset, Directions.Up);
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new DashThroughSpikes(entityData, offset, Directions.Down);
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new DashThroughSpikes(entityData, offset, Directions.Left);
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new DashThroughSpikes(entityData, offset, Directions.Right);

        public DashThroughSpikes(EntityData data, Vector2 offset, Directions dir) : base(data, offset, dir) {
            DreamDash = data.Bool("dreamDash", false);
            DreamVisuals = data.Bool("dreamVisuals", false);

            DynData<DashThroughSpikes> baseData = new DynData<DashThroughSpikes>(this);
            Remove(baseData.Get<PlayerCollider>("pc"));
            Add(new PlayerCollider(OnCollide));
            if (DreamDash) {
                Add(new DreamDashCollider(Collider));
            }
            if (DreamVisuals) {
                string type = data.Attr("type", "default");
                for (float i = 0; i < GetSize(data, dir) / 8; i++) {
                    Sprite sprite = new Sprite(GFX.Game, /*"danger/spikes/" + type*/"mask/default" + "_" + Direction.ToString().ToLower());
                    sprite.AddLoop("main", "", 0.1f, 0);
                    sprite.Play("main");

                    switch (Direction) {
                        case Directions.Up:
                            sprite.JustifyOrigin(0, 0);
                            sprite.Position = Vector2.UnitX * (i + 0.5f) * 8f + Vector2.UnitY;
                            break;
                        case Directions.Down:
                            sprite.JustifyOrigin(0, 1);
                            sprite.Position = Vector2.UnitX * (i + 0.5f) * 8f - Vector2.UnitY;
                            break;
                        case Directions.Right:
                            sprite.JustifyOrigin(.5f, .5f);
                            sprite.Position = Vector2.UnitY * (i + 0.5f) * 8f - Vector2.UnitX;
                            break;
                        case Directions.Left:
                            sprite.JustifyOrigin(-.5f, .5f);
                            sprite.Position = Vector2.UnitY * (i + 0.5f) * 8f + Vector2.UnitX;
                            break;
                    }
                    sprite.Position += sprite.Origin;

                    var mask = new DreamMaskComponent(sprite, new Rectangle(-4, -8, 7, 7), jellyRendering: false);
                    Add(mask);
                    Add(sprite);
                }
                Visible = false;
            }
        }

        private void OnCollide(Player player) {
            if (!player.DashAttacking && !(player.StateMachine.State == Player.StDreamDash)) {
                switch (Direction) {
                    case Directions.Up:
                        if (player.Speed.Y >= 0f && player.Bottom <= base.Bottom) {
                            player.Die(new Vector2(0f, -1f));
                        }
    
                       break;
                    case Directions.Down:
                        if (player.Speed.Y <= 0f) {
                            player.Die(new Vector2(0f, 1f));
                        }
    
                        break;
                    case Directions.Left:
                        if (player.Speed.X >= 0f) {
                            player.Die(new Vector2(-1f, 0f));
                        }

                        break;
                    case Directions.Right:
                        if (player.Speed.X <= 0f) {
                            player.Die(new Vector2(1f, 0f));
                        }
    
                        break;
                }
            }
        }

        private static int GetSize(EntityData data, Directions dir) {
            if ((uint) dir > 1u) {
                _ = dir - 2;
                _ = 1;
                return data.Height;
            }

            return data.Width;
        }
    }
}