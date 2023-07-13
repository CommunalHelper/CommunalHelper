using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.CommunalHelper.Scenes;

namespace Celeste.Mod.CommunalHelper;

/// <summary>
/// Communal Helper commands. They all start with the CH prefix.
/// </summary>
public static class Commands
{
    /// <summary>
    /// Gives the player a persisent <see cref="RedlessBerry"/>.
    /// </summary>
    [Command("ch_give_redlessberry", help: "gives the player a persisent redless strawberry.")]
    public static void CH_GiveRedlessBery()
    {
        if (Engine.Scene is not Level level)
        {
            Engine.Commands.Log("Cannot execute this command outside of a level!", Color.Red);
            return;
        }

        if (!Util.TryGetPlayer(out Player player))
        {
            Engine.Commands.Log("Player couldn't be found!", Color.Red);
            return;
        }

        EntityID id = new("ch-unknown-redlessberry", -1);

        // Let's not allow the player giving themselves two redless berries with this command.
        foreach (Follower follower in player.Leader.Followers)
        {
            if (follower.Entity is RedlessBerry e && e.ID.Level == id.Level && e.ID.ID == id.ID)
            {
                Engine.Commands.Log("Player was already given a Redless Strawberry!", Color.Red);
                return;
            }
        }

        RedlessBerry berry = new(player, new(id, player.Center)) { Given = true, };
        level.Add(berry);
    }

    [Command("ch_make_voxel", help: "opens the voxel editor to create a new voxel of given size sx * sy * sz")]
    public static void CH_MakeVoxel(int sx, int sy, int sz)
    {
        if (sx <= 0 || sy <= 0 || sz <= 0)
        {
            Engine.Commands.Log($"ERROR: The provided dimensions of the voxel ({sx} * {sy} * {sz}) are invalid. They must all be strictly positive integers.", Color.Red);
            Engine.Commands.Log("Make sure you provide all three dimensions when executing the command.", Color.Red);
            Engine.Commands.Log("USAGE: ch_make_voxel <sx> <sy> <sz>", Color.Gold);
            return;
        }

        Scene next = new VoxelEditor(sx, sy, sz);

        Engine.Commands.Open = false;
        if (Engine.Scene is Level or Overworld)
            new FadeWipe(Engine.Scene, false, () =>
            {
                Engine.Scene = next;
            });
        else
            Engine.Scene = next;

        Audio.Stop(Audio.CurrentMusicEventInstance);
        Audio.Stop(Audio.CurrentAmbienceEventInstance);
    }
}
