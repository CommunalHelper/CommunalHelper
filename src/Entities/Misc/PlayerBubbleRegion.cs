using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/PlayerBubbleRegion")]
internal class PlayerBubbleRegion : Entity
{
    private Vector2 control, end;

    public PlayerBubbleRegion(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.NodesOffset(offset)) { }

    public PlayerBubbleRegion(Vector2 position, int width, int height, Vector2[] nodes)
        : base(position)
    {
        control = nodes[0];
        end = nodes[1];

        Depth = Depths.DreamBlocks - 1;

        Collider = new Hitbox(width, height);
        Add(new PlayerCollider(OnPlayer));
    }

    private void OnPlayer(Player player)
    {
        if (!player.Dead && player.StateMachine.State != Player.StCassetteFly)
        {
            Audio.Play(SFX.game_gen_cassette_bubblereturn, SceneAs<Level>().Camera.Position + new Vector2(160f, 90f));
            player.StartCassetteFly(end, control);
        }
    }
}
