using FMOD.Studio;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

public class CSGEN_HeartGemShards : CutsceneEntity
{
    private readonly HeartGem heart;
    private readonly DynamicData heartData;

    private Vector2 cameraStart;

    private ParticleSystem system;
    private EventInstance snapshot;
    private EventInstance sfx;

    public CSGEN_HeartGemShards(HeartGem heart)
        : base(true, false)
    {
        this.heart = heart;
        heartData = new(typeof(HeartGem), heart);
    }

    public override void OnBegin(Level level)
    {
        cameraStart = level.Camera.Position;
        Add(new Coroutine(Cutscene(level), true));
    }

    private IEnumerator Cutscene(Level level)
    {
        sfx = Audio.Play(CustomSFX.game_seedCrystalHeart_collect_all_main, Position);
        snapshot = Audio.CreateSnapshot(Snapshots.MAIN_DOWN, true);

        Player entity = Scene.Tracker.GetEntity<Player>();
        if (entity != null)
        {
            cameraStart = entity.CameraTarget;
        }

        List<HeartGemShard> pieces = heartData.Get<List<HeartGemShard>>(HeartGemShard.HeartGem_HeartGemPieces);
        foreach (HeartGemShard piece in pieces)
        {
            piece.OnAllCollected();
        }

        heart.Depth = Depths.FormationSequences - 2;
        heart.AddTag(Tags.FrozenUpdate);
        yield return 0.35f;

        Tag = Tags.FrozenUpdate | Tags.HUD;
        level.Frozen = true;
        level.FormationBackdrop.Display = true;
        level.FormationBackdrop.Alpha = 0.5f;
        level.Displacement.Clear();
        level.Displacement.Enabled = false;
        Audio.BusPaused(Buses.AMBIENCE, true);
        Audio.BusPaused(Buses.CHAR, true);
        Audio.BusPaused(Buses.YES_PAUSE, true);
        Audio.BusPaused(Buses.CHAPTERS, true);
        yield return 0.1f;

        system = new ParticleSystem(-2000002, 50)
        {
            Tag = Tags.FrozenUpdate
        };
        level.Add(system);
        float angleIncr = Calc.Circle / pieces.Count;
        float angle = angleIncr;
        Vector2 averatePos = Vector2.Zero;
        foreach (HeartGemShard piece in pieces)
        {
            averatePos += piece.Position;
        }
        averatePos /= pieces.Count;

        float duration = 5f;
        bool specialCase = pieces.Count == 3;
        foreach (HeartGemShard piece in pieces)
        {
            piece.StartSpinAnimation(averatePos, heart.Position, angle, duration, regular: !specialCase);
            angle += angleIncr;
        }
        Vector2 cameraTarget = heart.Position - new Vector2(160f, 90f);
        cameraTarget = cameraTarget.Clamp(level.Bounds.Left, level.Bounds.Top, level.Bounds.Right - 320, level.Bounds.Bottom - 180);
        Add(new Coroutine(CameraTo(cameraTarget, duration - .8f, Ease.CubeInOut, 0f), true));
        yield return duration;

        Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
        Audio.Play(CustomSFX.game_seedCrystalHeart_shards_reform, heart.Position);
        foreach (HeartGemShard piece in pieces)
        {
            piece.StartCombineAnimation(heart.Position, 0.658f, system, level, spin: !specialCase);
        }
        yield return 0.658f;

        Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        foreach (HeartGemShard piece in pieces)
        {
            piece.RemoveSelf();
        }
        level.Shake();
        level.Flash(Color.White * .8f);
        HeartGemShard.CollectedPieces(heartData);
        yield return 0.5f;

        float dist = (level.Camera.Position - cameraStart).Length();
        yield return CameraTo(cameraStart, dist / 180f, null, 0f);

        if (dist > 80f)
        {
            yield return 0.25f;
        }

        level.EndCutscene();
        OnEnd(level);
        yield break;
    }

    public override void OnEnd(Level level)
    {
        if (WasSkipped)
        {
            Audio.Stop(sfx, true);
        }

        level.OnEndOfFrame += delegate
        {
            if (WasSkipped)
            {
                foreach (HeartGemShard piece in heartData.Get<List<HeartGemShard>>(HeartGemShard.HeartGem_HeartGemPieces))
                {
                    piece.RemoveSelf();
                }
                HeartGemShard.CollectedPieces(heartData);
                level.Camera.Position = cameraStart;
            }
            heart.Depth = Depths.Pickups;
            heart.RemoveTag(Tags.FrozenUpdate);
            level.Frozen = false;
            level.FormationBackdrop.Display = false;
            level.Displacement.Enabled = true;
        };

        RemoveSelf();
    }

    private void EndSfx()
    {
        Audio.BusPaused(Buses.AMBIENCE, false);
        Audio.BusPaused(Buses.CHAR, false);
        Audio.BusPaused(Buses.YES_PAUSE, false);
        Audio.BusPaused(Buses.CHAPTERS, false);
        Audio.ReleaseSnapshot(snapshot);
    }

    public override void Removed(Scene scene)
    {
        EndSfx();
        base.Removed(scene);
    }

    public override void SceneEnd(Scene scene)
    {
        EndSfx();
        base.SceneEnd(scene);
    }

}
