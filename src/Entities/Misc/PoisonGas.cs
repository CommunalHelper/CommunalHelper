using MonoMod.Cil;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

// Ported from xolimono's Midway Contest and the road lightly toasted

[CustomEntity(["CommunalHelper/PoisonGas"])]
[Tracked(false)]
public class PoisonGas : Entity
{
    public static void Load()
    {
        On.Celeste.PlayerHair.GetHairColor += DashHairColor;
        On.Celeste.Player.Update += Player_Update;
        IL.Celeste.Player.Render += Player_Render;
    }
    public static void Unload()
    {
        On.Celeste.PlayerHair.GetHairColor -= DashHairColor;
        On.Celeste.Player.Update -= Player_Update;
        IL.Celeste.Player.Render -= Player_Render;
    }
    private static void Player_Render(ILContext il)
    {
        ILCursor iLCursor = new ILCursor(il);
        if (iLCursor.TryGotoNext(MoveType.Before, i => i.MatchBrfalse(out var _) && i.Previous.MatchCallvirt<Player>("get_IsTired")))
        {
            iLCursor.EmitDelegate<Func<bool, bool>>((bool b) => b || CommunalHelperModule.Session.GasTimer > 2f);
        }
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        orig.Invoke(self);

        if (CommunalHelperModule.Session.PrevGasTimer < 0.5f && CommunalHelperModule.Session.GasTimer >= 0.5f)
        {
            Audio.Play(CustomSFX.game_poisonGas_timer, "timer", 0);
        }
        else if (CommunalHelperModule.Session.PrevGasTimer < 1.5f && CommunalHelperModule.Session.GasTimer >= 1.5f)
        {
            Audio.Play(CustomSFX.game_poisonGas_timer, "timer", 1);
        }
        else if (CommunalHelperModule.Session.PrevGasTimer < 2.5f && CommunalHelperModule.Session.GasTimer >= 2.5f)
        {
            Audio.Play(CustomSFX.game_poisonGas_timer, "timer", 2);
        }

        if (CommunalHelperModule.Session.GasTimer >= 3f)
        {
            self.Die(Vector2.Zero);
        }

        CommunalHelperModule.Session.PrevGasTimer = CommunalHelperModule.Session.GasTimer;
    }

    public static Color DashHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index)
    {
        if (CommunalHelperModule.Session.GasTimer > 0f)
        {
            return Color.Lerp(orig.Invoke(self, index), Color.White, CommunalHelperModule.Session.GasTimer / 3f);
        }
        return orig.Invoke(self, index);
    }

    private readonly Circle c;
    private readonly Image i;
    private readonly SineWave sine;

    private Vector2 anchor;
    private bool kill = false;

    public PoisonGas(Vector2 position, string spritePath, int radius)
        : base(position)
    {
        c = new Circle(radius, position.X, position.Y);
        Collider = c;
        Add(i = new Image(Calc.Random.Choose(GFX.Game.GetAtlasSubtextures(spritePath.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9')))));
        Add(sine = new SineWave(0.33f, 0f).Randomize());
        i.Scale.X = i.Scale.Y = radius / 24f;
        i.CenterOrigin();
        Depth = -101;
        anchor = position;
    }

    public PoisonGas(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Attr("spritePath", "objects/MWC2022/xolimono/badGas/gas"), data.Int("radius", 48))
    { }

    public override void Update()
    {
        kill = true;
        base.Update();
        if (Util.TryGetPlayer(out var player))
        {
            if (Vector2.Distance(player.Center, Position) <= c.Radius)
            {
                player.Sprite.Color = Color.Green;
                CommunalHelperModule.Session.GasTimer += Engine.DeltaTime * Engine.TimeRate;
            }
            else if (Scene is Level)
            {
                foreach (PoisonGas entity in (Scene as Level).Tracker.GetEntities<PoisonGas>().Cast<PoisonGas>())
                {
                    if (Vector2.Distance(player.Center, entity.Position) <= entity.c.Radius)
                        kill = false;
                }
                if (kill) CommunalHelperModule.Session.GasTimer = 0f;
            }
        }
        Position = anchor + new Vector2(sine.Value * 3f, sine.ValueOverTwo * 2f);
    }

    public override void DebugRender(Camera camera)
    {
        base.DebugRender(camera);
        Draw.Circle(Position, c.Radius, Color.Green, (int) (Math.Max(c.Radius, 24) / 2));
    }
}
