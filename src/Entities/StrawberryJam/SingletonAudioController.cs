using FMOD.Studio;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam {
    [Tracked]
    public class SingletonAudioController : Entity {
        public static SingletonAudioController Ensure(Scene scene) {
            if (scene.Entities.Concat(scene.Entities.ToAdd).OfType<SingletonAudioController>().FirstOrDefault() is not { } sac) {
                scene.Add(sac = new SingletonAudioController());
            }

            return sac;
        }
        
        private const float muffle_time_seconds = 0.1f;
        
        public SingletonAudioController() {
            Collidable = Visible = false;
            Active = true;
            AddTag(Tags.Persistent);
        }

        public void Play(string path, Vector2 origin, float muffleTime = muffle_time_seconds, float playerRange = 320) =>
            Play(path, origin, null, muffleTime, playerRange);
        
        public void Play(string path, Entity entity, float muffleTime = muffle_time_seconds, float playerRange = 320) =>
            Play(path, Vector2.Zero, entity, muffleTime, playerRange);
        
        private void Play(string path, Vector2 origin, Entity entity, float muffleTime = muffle_time_seconds, float playerRange = 320) {
            // try to get an existing component
            if (Components.GetAll<SingletonComponent>().FirstOrDefault(c => c.AudioPath == path) is not { } comp) {
                Add(comp = new SingletonComponent(path));
            }

            if (!comp.Active && Scene is Level level && (
                entity != null && level.Camera.Collides(entity) ||
                entity == null && level.Camera.Contains(origin))) {
                comp.Play(origin, muffleTime);
            }

            if (comp.Active) {
                comp.Move(entity?.Center ?? origin);
            }
        }

        public void SetParam(string path, string param, float value) {
            if (Components.GetAll<SingletonComponent>().FirstOrDefault(c => c.AudioPath == path) is { } comp) {
                comp.SetParam(param, value);
            }
        }
        
        public void Stop(string path) {
            if (Components.GetAll<SingletonComponent>().FirstOrDefault(c => c.AudioPath == path) is { } comp) {
                comp.Stop();
            }
        }
        
        private class SingletonComponent : Component {
            public string AudioPath { get; }
            
            private float muffleTimeRemaining;
            private EventInstance instance;
            private Vector2 lastOrigin;

            public SingletonComponent(string path) : base(false, false) {
                AudioPath = path;
            }

            public override void Update() {
                muffleTimeRemaining -= Engine.DeltaTime;
                if (muffleTimeRemaining <= 0) {
                    Active = false;
                }
            }

            public void Play(Vector2 origin, float muffleTime) {
                muffleTimeRemaining = muffleTime;
                lastOrigin = origin;
                Active = true;
                instance = Audio.Play(AudioPath, origin);
            }

            public void SetParam(string param, float value) {
                if (instance != null) {
                    Audio.SetParameter(instance, param, value);
                }
            }

            public void Stop() {
                if (instance != null) {
                    Audio.Stop(instance);
                }
            }

            public void Move(Vector2 target) {
                if (instance == null || Scene?.Tracker.GetEntity<Player>() is not { } player) return;
                if ((lastOrigin - player.Center).LengthSquared() > (target - player.Center).LengthSquared()) {
                    Audio.Position(instance, target);
                    lastOrigin = target;
                }
            }
        }
    }
}
