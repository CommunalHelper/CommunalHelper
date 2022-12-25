using Celeste.Mod.CommunalHelper.Imports;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities.BoosterStuff {
    [CustomEntity("CommunalHelper/HeldBooster")]
    public class HeldBooster : CustomBooster {
        public class PathRenderer : PathRendererBase<HeldBooster> {
            private readonly Vector2 dir, perp;

            public PathRenderer(float alpha, Vector2 direction, HeldBooster booster)
                : base(alpha, PathStyle.Arrow, PathColors, booster) {
                dir = direction;
                perp = direction.Perpendicular();
                Depth = booster.Depth + 1;
            }

            public override void Render() {
                base.Render();

                if (Alpha <= 0f)
                    return;

                Color color = Booster.BoostingPlayer ? Color : Color.White;

                Util.TryGetPlayer(out Player player);

                float length = 128 * Percent;
                for (float f = 0f; f < length; f += 6f) {
                    float t = f / length;
                    float opacity = 1 - Ease.QuadOut(t);
                    DrawPathLine(Calc.Round(Booster.Start + dir * f), dir, perp, f, player, color, opacity);
                }
            }
        }

        public static readonly Color[] PathColors = new[] {
            Calc.HexToColor("0abd32"),
            Calc.HexToColor("0df235"),
            Calc.HexToColor("32623a"),
            Calc.HexToColor("6ef7ad"),
        };

        public static readonly Color PurpleBurstColor = Calc.HexToColor("7a1053");
        public static readonly Color PurpleAppearColor = Calc.HexToColor("e619c3");
        public static readonly Color GreenBurstColor = Calc.HexToColor("174f21");
        public static readonly Color GreenAppearColor = Calc.HexToColor("0df235");

        private readonly bool green;

        public Vector2 Start { get; }
        private readonly Vector2 dir;

        private PathRenderer pathRenderer;

        public HeldBooster(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.FirstNodeNullable(offset)) { }

        public HeldBooster(Vector2 position, Vector2? node = null)
            : base(position, redBoost: true) {
            green = node is not null && node.Value != position;

            ReplaceSprite(CommunalHelperModule.SpriteBank.Create(green ? "greenHeldBooster" : "purpleHeldBooster"));
            SetParticleColors(
                green ? GreenBurstColor : PurpleBurstColor,
                green ? GreenAppearColor : PurpleAppearColor
            );

            Start = position;
            dir = ((node ?? Vector2.Zero) - Start).SafeNormalize();
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (green) scene.Add(pathRenderer = new PathRenderer(1f, dir, this));
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            if (green) scene.Remove(pathRenderer);
            pathRenderer = null;
        }

        protected override void OnPlayerEnter(Player player) {
            base.OnPlayerEnter(player);
            Collidable = false;
        }

        protected override void OnPlayerExit(Player player) {
            base.OnPlayerExit(player);
            Collidable = true;
        }
        
        // prevents held boosters from starting automatically (which red boosters do after 0.25 seconds).
        protected override IEnumerator BoostRoutine(Player player) {
            while (true)
                yield return null;
        }

        protected override int? RedDashUpdateAfter(Player player) {
            if (!Input.Dash.Check)
                return Player.StNormal;

            return null;
        }

        protected override IEnumerator RedDashCoroutineAfter(Player player) {
            if (!green)
                yield break;

            DynamicData data = new(player);

            player.DashDir = dir;
            data.Set("gliderBoostDir", dir);
            player.Speed = dir * 240f;

            // If the player is inverted, invert its vertical speed so that it moves in the same direction no matter what.
            if (GravityHelper.IsPlayerInverted?.Invoke() ?? false)
                player.Speed.Y *= -1f;

            player.SceneAs<Level>().DirectionalShake(player.DashDir, 0.2f);
            if (player.DashDir.X != 0f)
                player.Facing = (Facings) Math.Sign(player.DashDir.X);
        }
    }
}
