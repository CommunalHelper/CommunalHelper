using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

internal class MultiDashHeartGem : HeartGem
{
    [CustomEvent("CommunalHelper/DemoCutscene/1",
        "CommunalHelper/DemoCutscene/2",
        "CommunalHelper/DemoCutscene/3")]
    private class DemoCutscene : DialogCutscene
    {
        public DemoCutscene(EventTrigger trigger, Player player, string eventID)
            : base(eventID, player, false) { }
    }

    private static readonly MethodInfo m_HeartGem_Collect = typeof(HeartGem).GetMethod("Collect", BindingFlags.NonPublic | BindingFlags.Instance);

    private int health = 3;
    private readonly string[] cutscenes = new string[3];

    private readonly DynamicData baseData;

    public MultiDashHeartGem(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        baseData = new(typeof(HeartGem), this);

        // Load cutscene ids
        string cutsceneList = data.Attr("cutscenes");
        if (!string.IsNullOrWhiteSpace(cutsceneList))
        {
            int idx = 0;
            foreach (string cutscene in cutsceneList.Split(','))
            {
                cutscenes[idx] = cutscene;
                Console.WriteLine(cutscenes[idx]);
                idx++;
            }
        }

    }

    public void Crack(Player player)
    {
        SlashFx.Burst(Center, 3f);
        health--;
        if (health < 1)
            m_HeartGem_Collect.Invoke(this, new object[] { player });
        // Add crack to heart
        // create light rays
    }

    // TODO: Add LuaCutscenes support
    public static void TryStartCutscene(string cutsceneId, Player player)
    {
        player.Scene.Add(EventTrigger.CutsceneLoaders[cutsceneId](null, player, cutsceneId));
    }

    public static void Load()
    {
        On.Celeste.HeartGem.OnPlayer += HeartGem_OnPlayer;
        On.Celeste.HeartGem.OnHoldable += HeartGem_OnHoldable;
    }

    public static void Unload()
    {
        On.Celeste.HeartGem.OnPlayer -= HeartGem_OnPlayer;
        On.Celeste.HeartGem.OnHoldable -= HeartGem_OnHoldable;
    }

    private static void HeartGem_OnPlayer(On.Celeste.HeartGem.orig_OnPlayer orig, HeartGem self, Player player)
    {
        if (self is MultiDashHeartGem gem)
        {
            Console.WriteLine("IsMultiDashHeartGem");
            if (player.DashAttacking)
            {
                if (!gem.baseData.Get<bool>("collected") && !gem.SceneAs<Level>().Frozen)
                {
                    if (gem.health > 1)
                    {
                        if (gem.baseData.Get<float>("bounceSfxDelay") <= 0f)
                        {
                            Audio.Play(SFX.game_gen_crystalheart_bounce, gem.Position);
                            gem.baseData.Set("bounceSfxDelay", 0.1f);
                        }
                        player.PointBounce(self.Center);
                        gem.baseData.Get<Wiggler>("moveWiggler").Start();
                        gem.ScaleWiggler.Start();
                        gem.baseData.Set("moveWiggleDir", (gem.Center - player.Center).SafeNormalize(Vector2.UnitY));
                        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                    }
                    Console.WriteLine(gem.health);
                    TryStartCutscene(gem.cutscenes[3 - gem.health], player);
                    gem.Crack(player);
                }
                return;
            }

        }
        orig(self, player);
    }

    private static void HeartGem_OnHoldable(On.Celeste.HeartGem.orig_OnHoldable orig, HeartGem self, Holdable h)
    {
        if (self is MultiDashHeartGem gem)
        {
            if (h.Dangerous(gem.baseData.Get<HoldableCollider>("holdableCollider")))
            {
                Player player = gem.Scene.Tracker.GetEntity<Player>();
                if (!gem.baseData.Get<bool>("collected") && player != null && gem.health > 1)
                {
                    // Basically just HeartGem.OnPlayer
                    if (gem.baseData.Get<float>("bounceSfxDelay") <= 0f)
                    {
                        Audio.Play(SFX.game_gen_crystalheart_bounce, gem.Position);
                        gem.baseData.Set("bounceSfxDelay", 0.1f);
                    }
                    h.PointBounce(self.Center);
                    gem.baseData.Get<Wiggler>("moveWiggler").Start();
                    gem.ScaleWiggler.Start();
                    gem.baseData.Set("moveWiggleDir", (gem.Center - player.Center).SafeNormalize(Vector2.UnitY));
                    Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                }
                else
                {
                    // Collect normally
                    orig(self, h);
                }
                Console.WriteLine(gem.health);
                TryStartCutscene(gem.cutscenes[3 - gem.health], player);
                gem.Crack(player);
            }
        }
        else
            orig(self, h);
    }

}
