using Celeste.Mod.CommunalHelper.DashStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DreamBoosterAny")]
public class DreamBooster : CustomBooster
{
    public static ParticleType P_BurstExplode { get; private set; }

    public static readonly Color BurstColor = Calc.HexToColor("19233b");
    public static readonly Color AppearColor = Calc.HexToColor("4d5f6e");

    // red, orange, yellow, green, cyan, blue, purple, pink.
    public static readonly Color[] DreamColors = new Color[8] {
        Calc.HexToColor("ee3566"),
        Calc.HexToColor("ff7b3d"),
        Calc.HexToColor("efdc65"),
        Calc.HexToColor("44bd4c"),
        Calc.HexToColor("3b9c8a"),
        Calc.HexToColor("30a0e6"),
        Calc.HexToColor("af7fc9"),
        Calc.HexToColor("df6da2")
    };
    public static readonly ParticleType[] DreamParticles = new ParticleType[8];

    public DreamBooster(EntityData data, Vector2 offset)
        : this(data.Position + offset)
    {
        ReplaceSprite(CommunalHelperGFX.SpriteBank.Create("dreamBooster"));
        SetSoundEvent(CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter, CustomSFX.game_customBoosters_dreamBooster_dreambooster_move, false);
    }

    public DreamBooster(Vector2 position)
        : base(position, redBoost: true)
    {
        Depth = Depths.DreamBlocks;

        SetParticleColors(BurstColor, AppearColor);
    }

    protected override int? RedDashUpdateBefore(Player player)
    {
        bool inSolid = player.CollideCheck<Solid>();

        // Prevent the player from jumping or dashing out of the DreamBooster. May be reset in IL hook below.
        // If for whatever reason this becomes an actual option for DreamBoosters, this will need to be changed.
        if (inSolid)
            Ch9HubTransition = true;

        LoopingSfxParam("dream_tunnel", Util.ToInt(inSolid));

        return null;
    }

    protected override int? RedDashUpdateAfter(Player player)
    {
        Ch9HubTransition = false;
        return null;
    }

    internal static void InitializeParticles()
    {
        P_BurstExplode = new ParticleType(P_Burst)
        {
            Color = BurstColor,
            SpeedMax = 250
        };
        for (int i = 0; i < 8; i++)
        {
            DreamParticles[i] = new ParticleType(P_Appear)
            {
                Color = DreamColors[i],
                SpeedMax = 60
            };
        }
    }
}

public class DreamBoosterHooks
{
    public static void Hook()
    {
        IL.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
        On.Celeste.Actor.MoveH += Actor_MoveH;
        On.Celeste.Actor.MoveV += Actor_MoveV;
        On.Celeste.Player.OnCollideH += Player_OnCollideH;
        On.Celeste.Player.OnCollideV += Player_OnCollideV;
    }

    public static void Unhook()
    {
        IL.Celeste.Player.RedDashUpdate -= Player_RedDashUpdate;
        On.Celeste.Actor.MoveH -= Actor_MoveH;
        On.Celeste.Actor.MoveV -= Actor_MoveV;
        On.Celeste.Player.OnCollideH -= Player_OnCollideH;
        On.Celeste.Player.OnCollideV -= Player_OnCollideV;
    }

    private static void Player_RedDashUpdate(ILContext il)
    {
        ILCursor cursor = new(il);
        // We want to reset this *only* if the player has DreamTunnelDash, since will then allow them to dash.
        // The check for whether the player can jump *just* happened, so that is no longer possible.
        cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Booster>("Ch9HubTransition"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<Player>>(player =>
        {
            if (player.LastBooster is DreamBooster && DreamTunnelDash.HasDreamTunnelDash)
                player.LastBooster.Ch9HubTransition = false;
        });
    }

    // A little bit of jank to make use of collision results
    private static bool dreamBoostMove = false;
    // More jank to indicate an actual collision (disabled DreamBlock)
    private static bool dreamBoostStop = false;

    private static bool Actor_MoveH(On.Celeste.Actor.orig_MoveH orig, Actor self, float moveH, Collision onCollide, Solid pusher)
    {
        if (self is Player player && player.StateMachine.State == Player.StRedDash && player.LastBooster is DreamBooster booster)
        {
            DynamicData playerData = new(typeof(Actor), self);
            float pos = player.X;
            Vector2 counter = playerData.Get<Vector2>("movementCounter");
            dreamBoostMove = true;
            if (orig(self, moveH, onCollide, pusher) && !dreamBoostStop)
            {
                player.X = pos;
                playerData.Set("movementCounter", counter);
                player.NaiveMove(Vector2.UnitX * moveH);
            }
            dreamBoostStop = false;
            dreamBoostMove = false;
            return false;
        }
        return orig(self, moveH, onCollide, pusher);
    }

    private static bool Actor_MoveV(On.Celeste.Actor.orig_MoveV orig, Actor self, float moveV, Collision onCollide, Solid pusher)
    {
        if (self is Player player && player.StateMachine.State == Player.StRedDash && player.LastBooster is DreamBooster booster)
        {
            DynamicData playerData = new(typeof(Actor), self);
            float pos = player.Y;
            Vector2 counter = playerData.Get<Vector2>("movementCounter");
            dreamBoostMove = true;
            if (orig(self, moveV, onCollide, pusher) && !dreamBoostStop)
            {
                player.Y = pos;
                playerData.Set("movementCounter", counter);
                player.NaiveMove(Vector2.UnitY * moveV);
            }
            dreamBoostStop = false;
            dreamBoostMove = false;
            return false;
        }
        return orig(self, moveV, onCollide, pusher);
    }

    private static void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
    {
        Player_OnCollide(new Action<Player, CollisionData>(orig), self, data);
    }

    private static void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
    {
        Player_OnCollide(new Action<Player, CollisionData>(orig), self, data);
    }

    private static void Player_OnCollide(Action<Player, CollisionData> orig, Player self, CollisionData data)
    {
        if (dreamBoostMove)
        {
            if (data.Hit is not DreamBlock block)
            {
                EmitDreamBurst(self, data.Hit.Collider);
                return;
            }

            if (DynamicData.For(block).Get<bool>("playerHasDreamDash"))
            {
                self.Die(-data.Moved);
                return;
            }
            else
                dreamBoostStop = true;
        }
        orig(self, data);
    }

    private static void EmitDreamBurst(Player player, Collider worldClipCollider)
    {
        Level level = player.SceneAs<Level>();
        if (level.OnInterval(0.04f))
        {
            DisplacementRenderer.Burst burst = level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f);
            burst.WorldClipCollider = worldClipCollider;
            burst.WorldClipPadding = 2;
        }
    }
}
