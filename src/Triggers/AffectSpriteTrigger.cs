using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers;

[CustomEntity("CommunalHelper/AffectSpriteTrigger")]
public class AffectSpriteTrigger : Trigger
{
    #region Hooks
    private static ILHook hook_Player_origUpdateSprite;

    public static void Load()
    {
        On.Celeste.Player.Added += Player_Added;
        hook_Player_origUpdateSprite = new ILHook(typeof(Player).GetMethod("orig_UpdateSprite", (System.Reflection.BindingFlags) 36), HookPlayerUpdateSprite);
    }

    public static void Unload()
    {
        On.Celeste.Player.Added -= Player_Added;
        hook_Player_origUpdateSprite?.Dispose();
        hook_Player_origUpdateSprite = null;
    }
    private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
    {
        orig(self, scene);
        DynamicData data = DynamicData.For(self);
        data.Set(PlayerSpriteRateOverride, 1f);
    }
    private static void HookPlayerUpdateSprite(ILContext ctx)
    {
        ILCursor cursor = new(ctx);
        while (cursor.TryGotoNext(i => i.MatchStfld<Sprite>("Rate")))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, Player, float>>((f, p) => f * (p is null ? 1 : (float) DynamicData.For(p).Get(PlayerSpriteRateOverride)));
            cursor.Index++;
        }

    }
    #endregion

    private const string PlayerSpriteRateOverride = "CH_PlayerSpriteRateOverride";

    private Vector2? refPosition;
    private Sprite sprite;
    private readonly bool player;
    private readonly string parameter;
    private readonly object value;

    public AffectSpriteTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        var temp = data.NodesOffset(offset);
        if (temp.Length > 0) refPosition = temp[0];
        player = data.Bool("player");
        parameter = data.Attr("parameter").ToLower();
        value = parameter switch
        {
            "rate" => data.Float("value", 1f),
            "rotation" => data.Float("value", 0f) * Calc.DegToRad,
            "userawdeltatime" => data.Bool("value", false),
            "justify" => data.Vector2Nullable("value"),
            "position" => data.Vector2("value", Vector2.Zero),
            "scale" => data.Vector2("value", Vector2.One),
            "color" => data.HexColor("value", Color.White),
            "active" => data.Bool("value", true),
            _ => null,
        };
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (player)
        {
            return;
        }
        if (refPosition is not null &&
            !scene.Entities.Any(f =>
            {
                if (Collide.CheckPoint(f, refPosition.Value) && f.Get<Sprite>() is Sprite sprite2)
                {
                    sprite = sprite2;
                    return true;
                }
                return false;
            })) { Util.Log("no matches found"); }
        if (sprite is null)
        {
            throw new Exception("AffectSpriteTrigger failed! Either select `player` or add a node to point to the affected entity.");
        }
    }

    public override void OnEnter(Player player)
    {
        if (this.player)
        {
            sprite = player?.Sprite;
            if (sprite is null)
            {
                if (Util.TryGetPlayer(out Player p2))
                    sprite = p2.Sprite;
                else
                {
                    Logger.Log(LogLevel.Error, "CommunalHelper", "No player found to use in AffectSpriteTrigger! This may occur from CrystallineHelper trigger triggers, when a player has died.");
                    return;
                }
            }
        }
        
        switch (parameter)
        {
            case "rate": if (this.player) DynamicData.For(player).Set(PlayerSpriteRateOverride, (float) value); else sprite.Rate = (float) value; break;
            case "rotation": sprite.Rotation = (float) value; break;
            case "userawdeltatime": sprite.UseRawDeltaTime = (bool) value; break;
            case "justify": sprite.Justify = (Vector2?) value; break;
            case "position": sprite.Position = (Vector2) value; break;
            case "scale": sprite.Scale = (Vector2) value; break;
            case "color": sprite.Color = (Color) value; break;
            case "active": sprite.Active = (bool) value; break;
        }
    }
}
