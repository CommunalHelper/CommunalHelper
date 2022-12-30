using Celeste.Mod.CommunalHelper.DashStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using static Celeste.Mod.CommunalHelper.Entities.PlayerSeekerBarrierRenderer;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/PlayerSeekerBarrier")]
[TrackedAs(typeof(SeekerBarrier))]
[Tracked]
public class PlayerSeekerBarrier : SeekerBarrier
{
    private static readonly float UncollidableParticleSpeedFactor = 1.0f;
    private static readonly float CollidableParticleSpeedFactor = 0.2f;

    private float speedFactor = UncollidableParticleSpeedFactor;

    // about 6 frames
    public const float WavedashLeniencyTimer = 0.1f;
    public float WavedashTime;

    private bool hasGroup;
    private PlayerSeekerBarrier master;
    private List<PlayerSeekerBarrier> group;

    public Tiling TileSpikes;
    private Spikes spikeUp, spikeDown, spikeLeft, spikeRight;

    public PlayerSeekerBarrier(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        SurfaceSoundIndex = SurfaceIndex.AuroraGlass;

        if (data.Bool("spiky", false))
            TileSpikes = Tiling.SpikeAll;
        else
        {
            if (data.Bool("spikeUp", false)) TileSpikes |= Tiling.SpikeUp;
            if (data.Bool("spikeDown", false)) TileSpikes |= Tiling.SpikeDown;
            if (data.Bool("spikeLeft", false)) TileSpikes |= Tiling.SpikeLeft;
            if (data.Bool("spikeRight", false)) TileSpikes |= Tiling.SpikeRight;
        }
    }

    public override void Update()
    {
        bool collidable = SeekerDash.HasSeekerDash || SeekerDash.SeekerAttacking;
        float targetSpeed = collidable ? CollidableParticleSpeedFactor : UncollidableParticleSpeedFactor;
        speedFactor = Calc.Approach(speedFactor, targetSpeed, Engine.DeltaTime * (collidable ? 0.5f : 4.0f));

        WavedashTime = Calc.Approach(WavedashTime, 0f, Engine.DeltaTime);

        if (spikeUp is not null) spikeUp.Collidable = collidable;
        if (spikeDown is not null) spikeDown.Collidable = collidable;
        if (spikeLeft is not null) spikeLeft.Collidable = collidable;
        if (spikeRight is not null) spikeRight.Collidable = collidable;

        base.Update();
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Tracker.GetEntity<PlayerSeekerBarrierRenderer>().Track(this);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        if (!hasGroup)
        {
            group = new();
            AddToGroupAndFindChildren(this);
        }

        if (TileSpikes.HasFlag(Tiling.SpikeUp)) AddInvisibleSpike(spikeUp = new Spikes(Position, (int) Width, Spikes.Directions.Up, string.Empty) { Visible = false });
        if (TileSpikes.HasFlag(Tiling.SpikeDown)) AddInvisibleSpike(spikeDown = new Spikes(Position + (Vector2.UnitY * Height), (int) Width, Spikes.Directions.Down, string.Empty) { Visible = false });
        if (TileSpikes.HasFlag(Tiling.SpikeLeft)) AddInvisibleSpike(spikeLeft = new Spikes(Position, (int) Height, Spikes.Directions.Left, string.Empty) { Visible = false });
        if (TileSpikes.HasFlag(Tiling.SpikeRight)) AddInvisibleSpike(spikeRight = new Spikes(Position + (Vector2.UnitX * Width), (int) Height, Spikes.Directions.Right, string.Empty) { Visible = false });
    }

    // kinda cursed
    private void AddInvisibleSpike(Spikes spike)
    {
        StaticMover sm = spike.Get<StaticMover>();
        sm.JumpThruChecker = null;
        sm.SolidChecker = null;
        sm.Platform = this;
        staticMovers.Add(sm);
        Scene.Add(spike);
    }

    private void AddToGroupAndFindChildren(PlayerSeekerBarrier from)
    {
        from.hasGroup = true;
        from.master = this;
        group.Add(from);

        foreach (PlayerSeekerBarrier barrier in Scene.Tracker.GetEntities<PlayerSeekerBarrier>())
        {
            if (barrier != from && !barrier.hasGroup)
            {
                barrier.Collidable = true;
                bool attached = Scene.CollideCheck(new Rectangle((int) from.X - 1, (int) from.Y, (int) from.Width + 2, (int) from.Height), barrier) ||
                                Scene.CollideCheck(new Rectangle((int) from.X, (int) from.Y - 1, (int) from.Width, (int) from.Height + 2), barrier);
                barrier.Collidable = false;
                if (attached)
                    AddToGroupAndFindChildren(barrier);
            }
        }
    }

    public void MakeGroupUncollidable()
    {
        foreach (PlayerSeekerBarrier barrier in master.group)
            barrier.Collidable = false;
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        scene.Tracker.GetEntity<PlayerSeekerBarrierRenderer>().Untrack(this);
    }

    #region Hooks

    internal static void Hook()
    {
        On.Celeste.SeekerBarrierRenderer.Track += SeekerBarrierRenderer_Track;
        On.Celeste.SeekerBarrierRenderer.Untrack += SeekerBarrierRenderer_Untrack;
        IL.Celeste.SeekerBarrier.Update += SeekerBarrier_Update;
    }

    internal static void Unhook()
    {
        On.Celeste.SeekerBarrierRenderer.Track -= SeekerBarrierRenderer_Track;
        On.Celeste.SeekerBarrierRenderer.Untrack -= SeekerBarrierRenderer_Untrack;
        IL.Celeste.SeekerBarrier.Update -= SeekerBarrier_Update;
    }

    private static void SeekerBarrierRenderer_Track(On.Celeste.SeekerBarrierRenderer.orig_Track orig, SeekerBarrierRenderer self, SeekerBarrier block)
    {
        if (block is PlayerSeekerBarrier)
            return;
        orig(self, block);
    }

    private static void SeekerBarrierRenderer_Untrack(On.Celeste.SeekerBarrierRenderer.orig_Untrack orig, SeekerBarrierRenderer self, SeekerBarrier block)
    {
        if (block is PlayerSeekerBarrier)
            return;
        orig(self, block);
    }

    private static void SeekerBarrier_Update(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchLdelemR4());
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<float, SeekerBarrier, float>>((speed, barrier) =>
        {
            if (barrier is PlayerSeekerBarrier playerSeekerBarrier)
                speed *= playerSeekerBarrier.speedFactor;
            return speed;
        });
    }

    #endregion
}
