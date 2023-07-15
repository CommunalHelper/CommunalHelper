using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ResetStateCrystal")]
public class ResetStateCrystal : Refill
{
    private readonly DynamicData baseData;

    public ResetStateCrystal(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        baseData = new(typeof(Refill), this);

        Remove(baseData.Get<Sprite>("sprite"));
        Sprite sprite = new(GFX.Game, "objects/CommunalHelper/resetStateCrystal/");
        sprite.AddLoop("idle", "ghostIdle", 0.1f);
        sprite.Play("idle");
        sprite.CenterOrigin();
        sprite.Color = Calc.HexToColor("676767");
        Add(sprite);
        baseData.Set("sprite", sprite);
        Remove(Get<PlayerCollider>());

        Add(new PlayerCollider(OnCollide));
    }

    public void OnCollide(Player player)
    {
        Audio.Play("event:/game/general/diamond_touch", Position);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        Collidable = false;
        Add(new Coroutine(RefillRoutine(player)));
        baseData.Set("respawnTimer", 2.5f);
    }

    public IEnumerator RefillRoutine(Player player)
    {
        Celeste.Freeze(0.025f);

        baseData.Get<Sprite>("sprite").Visible = baseData.Get<Sprite>("flash").Visible = false;
        bool oneUse = baseData.Get<bool>("oneUse");
        if (!oneUse)
        {
            baseData.Get<Image>("outline").Visible = true;
        }
        yield return 0.05;

        player.StateMachine.State = 0;
        float angle = player.Speed.Angle();
        Level level = baseData.Get<Level>("level");
        level.ParticlesFG.Emit(P_Shatter, 5, Position, Vector2.One * 4f, angle - ((float) Math.PI / 2f));
        level.ParticlesFG.Emit(P_Shatter, 5, Position, Vector2.One * 4f, angle + ((float) Math.PI / 2f));
        SlashFx.Burst(Position, angle);
        if (oneUse)
        {
            RemoveSelf();
        }
    }
}
