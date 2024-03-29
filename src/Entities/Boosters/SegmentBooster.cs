﻿using Celeste.Mod.CommunalHelper.Imports;
using MonoMod.Utils;
using System.Collections;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/SegmentBooster")]
public class SegmentBooster : CustomBooster
{
    public class PathRenderer : PathRendererBase<SegmentBooster>
    {
        private readonly Vector2 perp;
        public float Percent { get; set; }

        public PathRenderer(float alpha, SegmentBooster booster)
            : base(alpha, booster.style, CurvedBooster.PathColors, booster)
        {
            perp = booster.Dir.Perpendicular();
            Percent = alpha;
            Depth = Depths.Above + 1;
        }

        public override void Render()
        {
            base.Render();
            if (Alpha <= 0f)
                return;

            Color color = Booster.BoostingPlayer ? Color : Color.White;

            Player player = null;
            if (Booster.proximityPath)
                Util.TryGetPlayer(out player);

            for (float f = 0f; f < Booster.Length * Percent; f += 6f)
                DrawPathLine(Calc.Round(Booster.Start + (Booster.Dir * f)), Booster.Dir, perp, f, player, color);
            DrawPathLine(Calc.Round(Booster.Target), Booster.Dir, perp, Booster.Length, null, color);
        }
    }

    private PathRenderer pathRenderer;
    private readonly PathStyle style;
    private bool showPath = true;
    private readonly bool proximityPath;

    public float Length;
    public Vector2 Start, Target, Dir;

    public SegmentBooster(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Nodes[0] + offset, !data.Bool("hidePath"), data.Enum("pathStyle", PathStyle.Arrow), data.Bool("proximityPath", true)) { }

    public SegmentBooster(Vector2 position, Vector2 node, bool showPath, PathStyle style, bool proximityPath = true)
        : base(position, redBoost: true)
    {
        this.showPath = showPath;
        this.proximityPath = proximityPath;
        this.style = style;

        Target = node;
        Dir = Calc.SafeNormalize(Target - Position);
        Length = Vector2.Distance(position, Target);
        Start = position;

        ReplaceSprite(CommunalHelperGFX.SpriteBank.Create("segmentBooster"));
        SetParticleColors(CurvedBooster.BurstColor, CurvedBooster.AppearColor);
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
    }

    protected override void OnPlayerEnter(Player player)
    {
        base.OnPlayerEnter(player);
        pathRenderer.ResetRainbow();
        if (!showPath)
            Add(new Coroutine(RevealPathRoutine()));
    }

    protected override void OnPlayerExit(Player player)
    {
        base.OnPlayerExit(player);

        float angle = Dir.Angle() - 0.5f;
        Level level = SceneAs<Level>();
        for (int i = 0; i < 20; i++)
            level.ParticlesBG.Emit(CurvedBooster.P_BurstExplode, 1, player.Center, new Vector2(3f, 3f), angle + Calc.Random.NextFloat());
    }

    protected override int? RedDashUpdateBefore(Player player)
    {
        base.RedDashUpdateBefore(player);

        if (Vector2.Distance(player.Center, Start) >= Length)
        {
            player.Position = Target;
            SceneAs<Level>().DirectionalShake(Dir, 0.175f);
            return Player.StNormal;
        }

        return null;
    }

    protected override IEnumerator RedDashCoroutineAfter(Player player)
    {
        DynamicData data = new(player);

        player.DashDir = Dir;
        data.Set("gliderBoostDir", Dir);
        player.Speed = Dir * 240f;

        // If the player is inverted, invert its vertical speed so that it moves in the same direction no matter what.
        if (GravityHelper.IsPlayerInverted?.Invoke() ?? false)
            player.Speed.Y *= -1f;

        player.SceneAs<Level>().DirectionalShake(player.DashDir, 0.2f);
        if (player.DashDir.X != 0f)
            player.Facing = (Facings) Math.Sign(player.DashDir.X);

        yield break;
    }

    private IEnumerator RevealPathRoutine()
    {
        float duration = 0.5f;
        float timer = 0f;

        showPath = true;
        SetSoundEvent(
            CustomSFX.game_customBoosters_dreamBooster_dreambooster_enter,
            CustomSFX.game_customBoosters_dreamBooster_dreambooster_move,
            false);

        ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
        while (timer < duration)
        {
            timer += Engine.DeltaTime;
            pathRenderer.Alpha = pathRenderer.Percent = Math.Min(Ease.SineOut(timer / duration), 1);

            Vector2 pos = Start + (Dir * pathRenderer.Percent * Length);

            particlesBG.Emit(CurvedBooster.P_PathReveal, 2, pos, Vector2.One * 2f, (-Dir).Angle());
            yield return null;
        }
        pathRenderer.Percent = 1f;
    }
}
