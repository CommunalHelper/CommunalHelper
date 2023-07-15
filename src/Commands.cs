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

    [Command("ch_fill_voxel", help: "opens the voxel editor to create a new voxel of given size sx * sy * sz, which will be entirely filled with the specified tile ID")]
    public static void CH_FillVoxel(int sx, int sy, int sz, string c)
    {
        if (string.IsNullOrWhiteSpace(c) || c.Length != 1)
        {
            Engine.Commands.Log("The provided filler character is invalid or missing. It must be a 1-length character", Color.Red);
            return;
        }

        CH_MakeVoxel(sx, sy, sz, new string(c[0], sx * sy * sz));
    }

    [Command("ch_make_voxel", help: "opens the voxel editor to create a new voxel of given size sx * sy * sz, and optionally a voxel string model to load")]
    public static void CH_MakeVoxel(int sx, int sy, int sz, string model = null)
    {
        if (sx <= 0 || sy <= 0 || sz <= 0)
        {
            Engine.Commands.Log($"ERROR: The provided dimensions of the voxel ({sx} * {sy} * {sz}) are invalid. They must all be strictly positive integers.", Color.Red);
            Engine.Commands.Log("Make sure you provide all three dimensions when executing the command.", Color.Red);
            Engine.Commands.Log("USAGE: ch_make_voxel <sx> <sy> <sz>", Color.Gold);
            return;
        }

        if (model is not null && model.Length > sx * sy * sz)
        {
            Engine.Commands.Log("ERROR: The optional <model> argument was provided but its length is too long!", Color.Red);
            Engine.Commands.Log("Please make sure that the length of <model> is less than or equal to <sx> * <sy> * <sz>", Color.Red);
            return;
        }

        Scene next = new VoxelEditor(sx, sy, sz, model);

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
