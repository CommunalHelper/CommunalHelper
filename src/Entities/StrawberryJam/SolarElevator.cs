using FMOD.Studio;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[CustomEntity("CommunalHelper/SJ/SolarElevator")]
[Tracked]
public class SolarElevator : Solid
{
    private sealed class Background : Entity
    {
        private readonly SolarElevator elevator;

        public Background(SolarElevator elevator, int depth)
        {
            Depth = depth;
            this.elevator = elevator;
        }

        public override void Render()
        {
            for (int y = 0; y < elevator.Distance + 60; y += elevator.rails.Height)
                elevator.rails.DrawJustified(new(elevator.X, elevator.StartY - y), new(0.5f, 1.0f));

            elevator.back.DrawJustified(elevator.Position + Vector2.UnitY * 10, new(0.5f, 1.0f));
        }
    }

    public enum StartPosition
    {
        Closest,
        Top,
        Bottom,
    }

    private readonly ColliderList OpenCollider = new(
        new Hitbox(3, 16, -24, -54),
        new Hitbox(3, 16, 21, -54),
        new Hitbox(48, 8, -24, -62),
        new Hitbox(48, 5, -24, 0)
    );
    private readonly ColliderList ClosedCollider = new(
        new Hitbox(3, 54, -24, -54),
        new Hitbox(3, 54, 21, -54),
        new Hitbox(48, 8, -24, -62),
        new Hitbox(48, 5, -24, 0)
    );

    private readonly TalkComponent interaction;
    private readonly SoundSource sfx;

    public readonly float StartY;
    public readonly float Distance;
    private readonly int bgDepth;
    private readonly float time, delay;
    private readonly bool oneWay;
    private readonly StartPosition startPosition;
    private readonly string moveSfx, haltSfx;

    private readonly MTexture back, rails;

    public const string DefaultHintDialog = "communalhelper_entities_strawberry_jam_solar_elevator_hint";
    public readonly string HoldableHintDialog;
    public readonly bool RequiresHoldable;

    public bool Moving = false;
    public bool AtGroundFloor = true;
    public bool IsCarryingHoldable = false;

    private Background bg;

    private readonly HashSet<Actor> riders;

    public EntityID ID { get; }

    public SolarElevator(EntityData data, Vector2 offset, EntityID id)
        : this(
              id,
              data.Position + offset,
              data.Int("distance", 128),
              data.Int("bgDepth", Depths.BGDecals),
              data.Float("time", 3.0f),
              data.Float("delay", 1.0f),
              data.Bool("oneWay", false),
              data.Enum("startPosition", StartPosition.Closest),
              data.Attr("moveSfx", SFX.NONE),
              data.Attr("haltSfx", SFX.NONE),
              data.Bool("requiresHoldable", false),
              data.Attr("holdableHintDialog", DefaultHintDialog),
              data.Attr("reskinDirectory", "")
        )
    { }

    public SolarElevator(EntityID id, Vector2 position,
        int distance,
        int bgDepth,
        float time,
        float delay,
        bool oneWay = false,
        StartPosition startPosition = StartPosition.Closest,
        string moveSfx = SFX.NONE,
        string haltSfx = SFX.NONE,
        bool requiresHoldable = false,
        string holdableHintDialog = DefaultHintDialog,
        string reskinDirectory = "")
        : base(position, 56, 80, safe: true)
    {
        ID = id;

        Depth = Depths.FGDecals;
        SurfaceSoundIndex = SurfaceIndex.MoonCafe;

        StartY = Y;
        Distance = distance;
        this.bgDepth = bgDepth;
        this.time = time;
        this.delay = delay;
        this.oneWay = oneWay;
        this.startPosition = startPosition;
        this.moveSfx = moveSfx;
        this.haltSfx = haltSfx;

        HoldableHintDialog = holdableHintDialog;
        RequiresHoldable = requiresHoldable;

        UpdateCollider(open: true);

        Add(sfx = new());
        Add(interaction = new TalkComponent(new Rectangle(-12, -8, 24, 8), Vector2.UnitY * -24, Activate));

        string dir = reskinDirectory.Trim().TrimEnd('/');
        string path = string.IsNullOrEmpty(dir) ? "objects/CommunalHelper/strawberryJam/solarElevator/" : (dir + "/");

        GFX.Game.PushFallback(GFX.Game["objects/CommunalHelper/strawberryJam/solarElevator/front"]);
        MTexture front = GFX.Game[path + "front"];
        GFX.Game.PopFallback();

        GFX.Game.PushFallback(GFX.Game["objects/CommunalHelper/strawberryJam/solarElevator/back"]);
        back = GFX.Game[path + "back"];
        GFX.Game.PopFallback();

        GFX.Game.PushFallback(GFX.Game["objects/CommunalHelper/strawberryJam/solarElevator/rails"]);
        rails = GFX.Game[path + "rails"];
        GFX.Game.PopFallback();

        Image img = new(front);
        img.JustifyOrigin(0.5f, 1.0f);
        img.Position.Y = 10;
        Add(img);

        riders = new DynamicData(typeof(Solid), this).Get<HashSet<Actor>>("riders");
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        switch (startPosition)
        {
            case StartPosition.Bottom:
                break;

            case StartPosition.Top:
                Y -= Distance;
                AtGroundFloor = false;
                break;

            default:
            case StartPosition.Closest:
                Player player = scene.Tracker.GetEntity<Player>();
                if (player is null)
                    return;
                float distanceFromStart = Vector2.DistanceSquared(player.Center, Position);
                float distanceFromEnd = Vector2.DistanceSquared(player.Center, Position - Vector2.UnitY * Distance);
                if (distanceFromStart > distanceFromEnd)
                {
                    Y -= Distance;
                    AtGroundFloor = false;
                }
                break;
        }
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Add(bg = new Background(this, bgDepth));
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        scene.Remove(bg);
    }

    private void UpdateCollider(bool open)
    {
        Collider = open ? OpenCollider : ClosedCollider;
    }

    private void Activate(Player player)
    {
        Audio.Play(SFX.game_10_ppt_mouseclick, Position);

        if (Moving || (RequiresHoldable && !IsCarryingHoldable))
            return;

        Add(new Coroutine(Sequence(player)));
    }

    private bool CanCarry(Holdable holdable)
    {
        if (holdable.Entity is not Actor actor)
            return false;

        if (holdable.Holder?.IsRiding(this) ?? false)
            return true;

        if (actor.Left >= Left + 3 && actor.Right <= Right - 3 && actor.Bottom <= Bottom && actor.Top >= Top + 8)
            return true;

        return false;
    }

    private bool HoldableCheck()
    {
        foreach (Holdable holdable in Scene.Tracker.GetComponents<Holdable>())
            if (CanCarry(holdable))
                return true;
        return false;
    }

    public void SetLevel(StartPosition position)
    {
        if (Moving)
        {
            Get<Coroutine>()?.RemoveSelf();
            sfx.Stop();
            UpdateCollider(open: true);
            interaction.Enabled = true;
            Moving = false;
        }

        switch (position)
        {
            case StartPosition.Bottom:
                Y = StartY;
                AtGroundFloor = true;
                break;

            case StartPosition.Top:
                Y = StartY - Distance;
                AtGroundFloor = false;
                break;

            default:
            case StartPosition.Closest:
                Player player = Scene.Tracker.GetEntity<Player>();
                if (player is null)
                    return;
                float distanceFromStart = Vector2.DistanceSquared(player.Center, Position);
                float distanceFromEnd = Vector2.DistanceSquared(player.Center, Position - Vector2.UnitY * Distance);
                if (distanceFromStart > distanceFromEnd)
                {
                    Y = StartY;
                    AtGroundFloor = true;
                }
                else
                {
                    Y = StartY - Distance;
                    AtGroundFloor = false;
                }
                break;
        }
    }

    public override void Update()
    {
        base.Update();
        IsCarryingHoldable = HoldableCheck();
    }

    private IEnumerator Sequence(Player player)
    {
        Level level = SceneAs<Level>();

        // drop the eventual holdable.
        if (player.Holding is not null)
            player.Drop();

        // the player here is guaranteed to hold nothing.
        // let's also make it impossible to hold any holdable during the elevator sequence.
        foreach (Holdable holdable in Scene.Tracker.GetComponents<Holdable>())
            if (CanCarry(holdable))
                DynamicData.For(holdable).Set("cannotHoldTimer", delay + time);

        Moving = true;
        interaction.Enabled = false;
        UpdateCollider(open: false);

        yield return delay;

        sfx.Play(moveSfx);
        level.DirectionalShake(Vector2.UnitY, 0.15f);

        float start = Y;
        float end = AtGroundFloor ? (start - Distance) : (start + Distance);
        float t = 0.0f;
        while (t < time)
        {
            float percent = t / time;
            float at = start + (end - start) * percent;
            MoveToY(at);

            t = Calc.Approach(t, time, Engine.DeltaTime);
            yield return null;
        }

        MoveToY(end);
        sfx.Stop();
        Audio.Play(haltSfx, Position);
        level.DirectionalShake(Vector2.UnitY, 0.2f);

        UpdateCollider(open: true);
        AtGroundFloor = !AtGroundFloor;

        if (oneWay)
            yield break;

        Moving = false;
        interaction.Enabled = true;
    }

    // Fix wrong collision resolution against collider lists.
    // In this case, the entity only moves vertically, so let's just change MoveVExact only.
    // Copied from https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/ConnectedStuff/ConnectedSolid.cs#L396.
    // Might not behave well with Gravity Helper (inverted actors).
    public override void MoveVExact(int move)
    {
        if (Collider is not ColliderList)
        {
            base.MoveVExact(move);
            return;
        }

        Collider[] colliders = (Collider as ColliderList).colliders;

        GetRiders();

        Y += move;
        MoveStaticMovers(Vector2.UnitY * move);

        if (Collidable)
        {
            foreach (Actor entity in Scene.Tracker.GetEntities<Actor>())
            {
                if (!entity.AllowPushing)
                    continue;

                bool collidable = entity.Collidable;
                entity.Collidable = true;

                if (!entity.TreatNaive && CollideCheck(entity, Position))
                {
                    foreach (Hitbox hitbox in colliders)
                    {
                        if (!hitbox.Collide(entity))
                            continue;

                        float top = Y + hitbox.Top;
                        float bottom = Y + hitbox.Bottom;
                        int moveV = (move <= 0) ? (int) (top - entity.Bottom) : (int) (bottom - entity.Top);

                        Collidable = false;
                        entity.MoveVExact(moveV, entity.SquishCallback, this);
                        entity.LiftSpeed = LiftSpeed;
                        Collidable = true;
                    }
                }
                else if (riders.Contains(entity))
                {
                    Collidable = false;
                    if (entity.TreatNaive)
                        entity.NaiveMove(Vector2.UnitY * move);
                    else
                        entity.MoveVExact(move);
                    entity.LiftSpeed = LiftSpeed;
                    Collidable = true;
                }

                entity.Collidable = collidable;
            }
        }

        riders.Clear();
    }

    #region Hooks

    internal static void Hook()
    {
        On.Celeste.TalkComponent.Update += TalkComponent_Update;
    }

    internal static void Unhook()
    {
        On.Celeste.TalkComponent.Update -= TalkComponent_Update;
    }

    private static void TalkComponent_Update(On.Celeste.TalkComponent.orig_Update orig, TalkComponent self)
    {
        if (self.Entity is SolarElevator elevator && elevator.RequiresHoldable && self.UI is null)
            self.Scene.Add(self.UI = new HintTalkComponentUI(self, elevator));
        orig(self);
    }

    #endregion
}

public class HintTalkComponentUI : TalkComponent.TalkComponentUI
{
    private readonly DynamicData data;

    private readonly SolarElevator elevator;
    private readonly string text;
    private float lerp, prev;

    private float textTimer;

    public HintTalkComponentUI(TalkComponent handler, SolarElevator elevator)
        : base(handler)
    {
        this.elevator = elevator;
        text = Dialog.Clean(elevator.HoldableHintDialog);

        data = DynamicData.For(this);
    }

    public override void Update()
    {
        base.Update();

        bool show = Highlighted && !elevator.IsCarryingHoldable && !elevator.Moving;
        textTimer = show ? Calc.Approach(textTimer, 0f, Engine.DeltaTime) : 0.25f;

        prev = lerp;
        lerp = Calc.Approach(lerp, show && textTimer <= 0 ? 1f : 0f, Engine.DeltaTime * 6f);

        EventInstance sfx = null;
        if (prev == 1f && lerp < 1f)
            sfx = Audio.Play(SFX.ui_game_textbox_other_out, elevator.Position);
        else if (prev == 0 && lerp > 0)
            sfx = Audio.Play(SFX.ui_game_textbox_other_in, elevator.Position);

        sfx?.setPitch(1.75f);
    }

    public override void Render()
    {
        base.Render();

        float slide = data.Get<float>("slide");
        float timer = data.Get<float>("timer");
        float alpha = data.Get<float>("alpha");
        Wiggler wiggler = data.Get<Wiggler>("wiggler");

        Level level = Scene as Level;
        if (level.FrozenOrPaused || slide <= 0 || Handler.Entity is null)
            return;

        Vector2 pos = Handler.Entity.Position + Handler.DrawAt - level.Camera.Position.Floor();
        if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
            pos.X = 320f - pos.X;
        pos *= 6f;
        pos.Y += (float) Math.Sin(timer * 4f) * 12f + 64f * (1f - Ease.CubeOut(slide)) + 12f;

        float transparence = Ease.CubeInOut(slide) * alpha * lerp;
        float wigglerMask = textTimer > 0 ? 1 : 0f;
        float scale = Math.Max(wiggler.Value * wigglerMask * lerp * 0.1f + Ease.CubeOut(lerp) * 0.65f, 0);

        ActiveFont.DrawOutline(text, pos, Vector2.One * 0.5f, Vector2.One * scale, Color.White * transparence, 2f, Color.Black);
    }
}
