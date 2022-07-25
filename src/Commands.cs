using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper {
    /// <summary>
    /// Communal Helper commands. They all start with the CH prefix.
    /// </summary>
    public static class Commands {
        /// <summary>
        /// Gives the player a persisent <see cref="RedlessBerry"/>.
        /// </summary>
        [Command("ch_give_redlessberry", help: "gives the player a persisent redless strawberry.")]
        public static void CH_GiveRedlessBery() {
            if (Engine.Scene is not Level level) {
                Engine.Commands.Log("Cannot execute this command outside of a level!", Color.Red);
                return;
            }

            if (!Util.TryGetPlayer(out Player player)) {
                Engine.Commands.Log("Player couldn't be found!", Color.Red);
                return;
            }

            EntityID id = new EntityID("ch-unknown-redlessberry", 1073741823 + Calc.Random.Next(10000));
            RedlessBerry berry = new(player, new(id, player.Center)) { Given = true, };
            level.Add(berry);
        }
    }
}
