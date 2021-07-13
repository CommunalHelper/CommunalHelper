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

        protected bool Dream = false;

        public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new DashThroughSpikes(entityData, offset, Directions.Up);
        public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new DashThroughSpikes(entityData, offset, Directions.Down);
        public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new DashThroughSpikes(entityData, offset, Directions.Left);
        public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new DashThroughSpikes(entityData, offset, Directions.Right);

        public DashThroughSpikes(EntityData data, Vector2 offset, Directions dir) : base(data, offset, dir) {
            Dream = data.Bool("dream", false);

            DynData<Spikes> baseData = new DynData<Spikes>(this);
            Remove(baseData.Get<PlayerCollider>("pc"));
            Add(new PlayerCollider(OnCollide));
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
    }
}