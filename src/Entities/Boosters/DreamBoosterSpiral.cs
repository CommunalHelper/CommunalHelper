using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities.Boosters;

[CustomEntity("CommunalHelper/SpiralDreamBooster")]
public class DreamBoosterSpiral : DreamBooster
{
    private readonly Vector2 node;
    private readonly Vector2 dir;

    private readonly bool clockwise;
    private readonly float angle, startAngle;
    private readonly float radius;

    private readonly float speed, prepare, delay;

    private readonly Vector2 start, end;
    private readonly Vector2 finishBoost;

    private Vector2 playerPos;
    private float nearPlayerFade;
    
    public DreamBoosterSpiral(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Nodes[0] + offset, data.Bool("clockwise", true), data.Float("angle", 180f), data.Float("spiralSpeed", 240f), data.Float("beginTime", .75f), data.Float("delay", .2f))
    { }
    
    public DreamBoosterSpiral(Vector2 position, Vector2 node, bool clockwise = true, float angle = 180f, float spiralSpeed = 240f, float beginTime = .75f, float delay = .2f)
        : base(position)
    {
        ReplaceSprite(CommunalHelperGFX.SpriteBank.Create("dreamBooster"));
        SetSoundEvent(CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter, CustomSFX.game_customBoosters_dreamBooster_dreambooster_move, false);

        this.node = node;
        this.dir = (node - Center).SafeNormalize();

        this.clockwise = clockwise;
        this.angle = MathHelper.ToRadians(angle);
        this.startAngle = (node - Center).Angle();
        this.radius = Math.Max(Vector2.Distance(Center, node), 1);

        this.speed = spiralSpeed;
        this.prepare = beginTime;
        this.delay = delay;

        Vector2 endDir = Calc.AngleToVector(startAngle + this.angle * (clockwise ? 1 : -1), 1f);
        this.start = Calc.AngleToVector(startAngle, radius) + Center;
        this.end = endDir * radius + Center;
        this.finishBoost = endDir.Perpendicular() * (clockwise ? 1 : -1) * speed;
    }

    protected override IEnumerator RedDashCoroutineAfter(Player player)
    {
        DynamicData data = DynamicData.For(player);
        Collision onCollideH = data.Get<Collision>("onCollideH");
        Collision onCollideV = data.Get<Collision>("onCollideV");

        player.Speed = Vector2.Zero;

        float t = 0f;
        while (t < prepare)
        {
            float percent = Ease.QuadOut(t / prepare);
            Vector2 target = Vector2.Lerp(Center, node, percent);
            player.MoveToX(target.X, onCollideH);
            player.MoveToY(target.Y + 8, onCollideV);

            t += Engine.DeltaTime;
            yield return null;
        }
        player.MoveToX(node.X, onCollideH);
        player.MoveToY(node.Y + 8, onCollideV);

        yield return delay;

        float f = clockwise ? 1 : -1;
        float th = 0f;
        while (th < angle)
        {
            Vector2 target = Calc.AngleToVector(startAngle + th * f, radius) + Center;
            player.MoveToX(target.X, onCollideH);
            player.MoveToY(target.Y + 8, onCollideV);

            float arcAngle = speed * Engine.DeltaTime / radius;
            th = Calc.Approach(th, angle, arcAngle);
            yield return null;
        }
        player.MoveToX(end.X, onCollideH);
        player.MoveToY(end.Y + 8, onCollideV);

        player.StateMachine.State = Player.StNormal;
        player.Speed = finishBoost;
        yield break;
    }

    public override void Update()
    {
        base.Update();

        Player player = Scene.Tracker.GetEntity<Player>();
        if (player is not null)
        {
            playerPos = player.Center;
            nearPlayerFade = Ease.CubeOut(Math.Min(Vector2.Distance(playerPos, Center) / radius, 1));
        }
    }

    public override void Render()
    {
        float proj = Vector2.Dot(dir, playerPos - Center);

        int lines = (int) Math.Ceiling(radius / 8);
        for (int i = 0; i < lines; i++)
        {
            float percent = (float) i / lines;

            float sin = (float) Math.Sin(Scene.TimeActive * -5 + percent * 10) * .5f + .5f;
            float l = .25f + sin * 0.3f;

            Vector2 from = Vector2.Lerp(Center, start, percent);
            Vector2 to = Vector2.Lerp(Center, start, (float) (i + l) / lines);

            float highlight = Calc.Clamp((1 - Math.Abs(proj - percent * radius) * 0.02f) * nearPlayerFade, 0.2f, 0.85f);
            Draw.Line(from, to, Color.White * highlight);
        }

        float angleToPlayer = (playerPos - Center).Angle();

        float f = clockwise ? 1 : -1;

        float step = 8 / radius;
        float th = 0;
        float stop = Math.Min(angle, MathHelper.TwoPi - step);
        while (th < stop)
        {
            float angle = startAngle + th * f;

            float sin = (float) Math.Sin(Scene.TimeActive * -6 + th * 10) * .5f + .5f;
            float l = .3f + sin;
            Vector2 dir = f * Calc.AngleToVector(angle, 1f).Perpendicular();

            Vector2 from = Calc.AngleToVector(angle, radius) + Center;
            Vector2 to = from + dir * 4 * l;

            float dth = MathHelper.WrapAngle(angleToPlayer - angle);
            float highlight = Calc.Clamp((1 - Math.Abs(dth) * 1.5f) * nearPlayerFade, 0.2f, 0.85f);
            Draw.Line(from, to, Color.White * highlight);

            th += step;
        }

        base.Render();
    }
}
