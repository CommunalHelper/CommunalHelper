using Celeste.Mod.CommunalHelper.Imports;
using Celeste.Mod.CommunalHelper.Utils;
using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/CurvedBooster")]
public class CurvedBooster : CustomBooster
{
    public static ParticleType P_BurstExplode { get; private set; }
    public static ParticleType P_PathReveal { get; private set; }

    public static readonly Color BurstColor = Calc.HexToColor("062245");
    public static readonly Color AppearColor = Calc.HexToColor("59c3e6");

    public static readonly Color[] PathColors = new[] {
        Calc.HexToColor("1878a6"), Calc.HexToColor("0d5382"),
    };

    public class PathRenderer : PathRendererBase<CurvedBooster>
    {
        private struct Node
        {
            public readonly float Distance;
            public readonly Vector2 Position, Dir, Perp;

            public Node(float d, Vector2 position, Vector2 dir)
            {
                Distance = d;
                Position = position;
                Dir = dir;
                Perp = dir.Perpendicular();
            }
        }

        private readonly Node[] nodes;
        private Node last;

        public float Percent { get; set; }

        public PathRenderer(float alpha, CurvedBooster booster)
            : base(alpha, booster.style, PathColors, booster)
        {
            float sep = 6f;
            nodes = new Node[(int) Math.Ceiling(booster.curve.Length / sep)];

            for (int i = 0; i < nodes.Length; i++)
            {
                float d = i * sep;
                booster.curve.GetAllByDistance(d, out Vector2 point, out Vector2 derivative);
                nodes[i] = new Node(d, Calc.Round(point), Calc.SafeNormalize(derivative));
            }

            Depth = Depths.Above + 1;
            Percent = alpha;
        }

        public void SetLastNode(float distance, Vector2 point, Vector2 derivative)
        {
            last = new Node(distance, Calc.Round(point), Calc.SafeNormalize(derivative));
        }

        public override void Render()
        {
            base.Render();

            Color color = Booster.BoostingPlayer ? Color : Color.White;

            Player player = null;
            if (Booster.proximityPath)
                Util.TryGetPlayer(out player);

            for (int i = 0; i < nodes.Length * Percent; i++)
            {
                Node node = nodes[i];
                DrawPathLine(node.Position, node.Dir, node.Perp, node.Distance, player, color);
            }
            if (Percent != 0)
                DrawPathLine(last.Position, last.Dir, last.Perp, last.Distance, null, color);
        }
    }

    private PathRenderer pathRenderer;
    private readonly PathStyle style;
    private bool showPath = true;
    private readonly bool proximityPath;

    private readonly BakedCurve curve;
    private float travel = 0;

    private readonly Vector2 endingSpeed;

    public override bool IgnorePlayerSpeed => true;

    public CurvedBooster(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.NodesWithPosition(offset), data.Enum<CurveType>("curve"), !data.Bool("hidePath"), data.Enum("pathStyle", PathStyle.Arrow), data.Bool("proximityPath", true)) { }

    public CurvedBooster(Vector2 position, Vector2[] nodes, CurveType mode, bool showPath, PathStyle style, bool proximityPath = true)
        : base(position, redBoost: true)
    {
        this.showPath = showPath;
        this.proximityPath = proximityPath;
        this.style = style;

        curve = new BakedCurve(nodes, mode, 24);
        endingSpeed = Calc.SafeNormalize(curve.GetPointByDistance(curve.Length) - curve.GetPointByDistance(curve.Length - 1f), 240);

        ReplaceSprite(CommunalHelperGFX.SpriteBank.Create("curvedBooster"));
        SetParticleColors(BurstColor, AppearColor);
        SetSoundEvent(
            showPath ? CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter : CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter_cue,
            SFX.game_05_redbooster_move_loop,
            false);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Add(pathRenderer = new PathRenderer(Util.ToInt(showPath), this));
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        scene.Remove(pathRenderer);
        pathRenderer = null;
        base.Removed(scene);
    }

    protected override void OnPlayerEnter(Player player)
    {
        base.OnPlayerEnter(player);
        travel = 0f;
        if (!showPath)
            Add(new Coroutine(RevealPathRoutine()));
    }

    protected override void OnPlayerExit(Player player)
    {
        base.OnPlayerExit(player);

        float angle = curve.GetDerivativeByDistance(travel).Angle() - 0.5f;
        Level level = SceneAs<Level>();
        for (int i = 0; i < 20; i++)
            level.ParticlesBG.Emit(P_BurstExplode, 1, player.Center, new Vector2(3f, 3f), angle + Calc.Random.NextFloat());
    }

    protected override int? RedDashUpdateBefore(Player player)
    {
        base.RedDashUpdateBefore(player);

        DynamicData data = player.GetData();

        Vector2 prev = player.Position;

        travel += 240f * Engine.DeltaTime; // booster speed constant
        bool end = travel >= curve.Length;

        curve.GetAllByDistance(travel, out Vector2 next, out Vector2 derivative);

        // Override GravityHelper's changes while we naively move the player with the curved booster.
        GravityHelper.BeginOverride?.Invoke();

        bool inverted = GravityHelper.IsPlayerInverted?.Invoke() ?? false;
        int offY = inverted ? -7 : 8;

        // player's speed won't matter, we won't allow it to move while in a curved booster.
        // this is here so that the player doesn't die to spikes that it shouldn't die to.
        player.SetBoosterFacing(derivative.SafeNormalize());

        bool stopped = false;
        player.MoveToX(next.X, _ => stopped = true);
        player.MoveToY(next.Y + offY, _ => stopped = true);

        // Then finish overriding.
        GravityHelper.EndOverride?.Invoke();

        if (stopped)
            return Player.StNormal;

        if (end)
        {
            Vector2 speed = endingSpeed;
            if (inverted)
                speed.Y *= -1f;
            player.Speed = speed;
            return Player.StNormal;
        }

        return null;
    }

    private IEnumerator RevealPathRoutine()
    {
        float distance = 0f;

        showPath = true;
        SetSoundEvent(
            CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter,
            SFX.game_05_redbooster_move_loop,
            false);

        ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
        while (distance < curve.Length)
        {
            pathRenderer.Alpha = Calc.Approach(pathRenderer.Alpha, 1f, Engine.DeltaTime);
            pathRenderer.Percent = distance / curve.Length;
            distance = Calc.Approach(distance, curve.Length, Engine.DeltaTime * 360);

            curve.GetAllByDistance(distance, out Vector2 point, out Vector2 derivative);
            pathRenderer.SetLastNode(distance, point, derivative);

            particlesBG.Emit(P_PathReveal, 1, point, Vector2.One * 2f, (-derivative).Angle());
            yield return null;
        }
        pathRenderer.Percent = 1f;
    }

    internal static void InitializeParticles()
    {
        P_BurstExplode = new ParticleType(P_Burst)
        {
            Color = BurstColor,
            SpeedMax = 250
        };
        P_PathReveal = new ParticleType(P_Appear)
        {
            Color = AppearColor,
            SpeedMax = 60
        };
    }
}
