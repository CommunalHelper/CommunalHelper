using FMOD.Studio;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ElytraNoteRing")]
public class ElytraNoteRing : ElytraRing
{
    private struct Param
    {
        internal string name;
        internal float value;
    }
    public override bool PreserveTraversalOrder => false;

    private readonly float pitch;
    private readonly float volume;
    private readonly Param[] parameters;
    private readonly string eventName;

    public ElytraNoteRing(EntityData data, Vector2 offset)
        : this(
            data.Position + offset,
            data.Nodes[0] + offset,
            data.Int("semitone", 12),
            data.Float("volume", 1f),
            data.String("parameters", null),
            data.String("eventName", CustomSFX.game_elytra_rings_note),
            data.HexColor("color", Color.White)
        )
    { }

    public ElytraNoteRing(Vector2 a, Vector2 b, int semitone, float volume, string @params, string eventName, Color color)
        : base(a, b, color)
    {
        pitch = (float) Math.Pow(2, semitone / 12.0f);

        this.volume = volume;

        if (@params is not null && @params.Length > 0)
        {
            parameters = @params.Split(";")
                .Select(s => s.Split("="))
                .Where(parts => parts.Length == 2)
                .Select(parts => new Param() { name = parts[0], value = float.TryParse(parts[1], out float val) ? val : 0f })
                .Where(param => !string.IsNullOrWhiteSpace(param.name))
                .ToArray();
        }

        parameters ??= [];

        this.eventName = eventName;
    }

    public override void OnPlayerTraversal(Player player, int sign, bool shake = true)
    {
        base.OnPlayerTraversal(player, sign, false);

        EventInstance instance = Audio.Play(eventName, player.Center);

        instance.setPitch(pitch / 2f);
        instance.setVolume(volume);

        foreach(var param in parameters)
        {
            instance.setParameterValue(param.name, param.value);
        }
    }
}
