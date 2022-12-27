namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/MusicParamTrigger")]
internal class MusicParamTrigger : Trigger
{
    private readonly float enterValue, exitValue;
    private readonly string[] parameters;

    public MusicParamTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {

        enterValue = data.Float("enterValue", 1f);
        exitValue = data.Float("exitValue", 0f);

        parameters = data.Attr("param").Split(',');
        for (int i = 0; i < parameters.Length; i++)
        {
            parameters[i] = parameters[i].Trim();
        }
    }

    private void SetParameters(float v)
    {
        foreach (string param in parameters)
        {
            Audio.SetMusicParam(param, v);
        }
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        SetParameters(enterValue);
    }

    public override void OnLeave(Player player)
    {
        base.OnEnter(player);
        SetParameters(exitValue);
    }
}
