namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraNoteRing")]
public class ElytraNoteRing : ElytraRing
{
    public override bool PreserveTraversalOrder => false;

    private readonly float pitch;

    public ElytraNoteRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset,
            data.Int("semitone", 12)
        )
    { }

    public ElytraNoteRing(Vector2 a, Vector2 b, int semitone)
        : base(a, b, Color.White)
    {
        pitch = (float) Math.Pow(2, semitone / 12.0f);
    }

    public override void OnPlayerTraversal(Player player, int sign, bool shake = true)
    {
        base.OnPlayerTraversal(player, sign, false);
        Audio.Play(CustomSFX.game_elytra_rings_note, player.Center)
             .setPitch(pitch / 2.0f);
    }
}
