using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamBooster")]
    public class DreamBooster : CustomBooster {

        public float Length;
        public Vector2 Start, Target, Dir;

        public DreamBooster(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Nodes[0] + offset) { }

        public DreamBooster(Vector2 position, Vector2 node) 
            : base(position, redBoost: true) {

            Target = node;
            Dir = Calc.SafeNormalize(Target - Position);
            Length = Vector2.Distance(position, Target);
            Start = position;

            ReplaceSprite(CommunalHelperModule.SpriteBank.Create("dreamBooster"));
        }

        public override void Render() {
            base.Render();
            //Draw.Line(Position, Target, Color.White);
        }
    }

    public class DreamBoosterHooks {
        public static void Unhook() {
            On.Celeste.Player.RedDashCoroutine -= Player_RedDashCoroutine;
            On.Celeste.Player.RedDashUpdate -= Player_RedDashUpdate;
        }

        public static void Hook() {
            On.Celeste.Player.RedDashCoroutine += Player_RedDashCoroutine;
            On.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
        }

        private static int Player_RedDashUpdate(On.Celeste.Player.orig_RedDashUpdate orig, Player self) {
            int result = orig(self);

            if (self.LastBooster is DreamBooster booster) {
                if (Vector2.Distance(self.Center, booster.Start) >= booster.Length) {
                    self.Position = booster.Target;
                    return 0;
                }
            }

            return result;
        }

        private static IEnumerator Player_RedDashCoroutine(On.Celeste.Player.orig_RedDashCoroutine orig, Player self) {
            // get the booster now, it'll be set to null in the coroutine
            Booster currentBooster = self.CurrentBooster;

            // do the entire coroutine, thanks max480 :)
            IEnumerator origEnum = orig(self);
            while (origEnum.MoveNext())
                yield return origEnum.Current;

            if (currentBooster is DreamBooster booster) {
                DynData<Player> playerData = new DynData<Player>(self);
                self.Speed = ((Vector2)(playerData["gliderBoostDir"] = self.DashDir = booster.Dir)) * 240f;
                self.SceneAs<Level>().DirectionalShake(self.DashDir, 0.2f);
                if (self.DashDir.X != 0f) {
                    self.Facing = (Facings) Math.Sign(self.DashDir.X);
                }
            }
        }
    }
}
