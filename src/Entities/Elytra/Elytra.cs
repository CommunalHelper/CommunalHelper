using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/Elytra")]
    public class Elytra : Entity {
        private readonly Sprite sprite;
        private readonly Image outline;

        private readonly SineWave sine;
        private readonly BloomPoint bloom;
        private readonly VertexLight light;

        public Elytra(EntityData data, Vector2 offset)
            : this(data.Position + offset) { }

        public Elytra(Vector2 position)
            : base(position) {

            Collider = new Hitbox(20f, 20f, -10f, -10f);
            Add(new PlayerCollider(OnPlayer));

            Add(bloom = new BloomPoint(0.5f, 20f));
            Add(light = new VertexLight(Color.White, 1f, 16, 48));

            Add(sine = new SineWave(0.6f, 0f).Randomize());

            Add(sprite = GFX.SpriteBank.Create("flyFeather"));
            Add(outline = new Image(GFX.Game["objects/flyFeather/outline"]));
            outline.CenterOrigin();
            outline.Visible = false;

            UpdateY();
        }

        private void OnPlayer(Player player) {
            if (player.StateMachine.State == ElytraState.St)
                return;

            bool wasInFeatherState = player.StateMachine.State == Player.StStarFly;
            Audio.Play(wasInFeatherState ? SFX.game_06_feather_renew : SFX.game_06_feather_get, Position);
            player.StateMachine.State = ElytraState.St;
        }

        private void UpdateY() {
            sprite.X = 0f;
            sprite.Y = bloom.Y = sine.Value * 2f;
        }

        public override void Update() {
            base.Update();

            UpdateY();
            light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;
        }
    }
}
