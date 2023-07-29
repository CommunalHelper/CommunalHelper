using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.Entities;
using FMOD.Studio;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper;

[CustomEntity("CommunalHelper/ConnectedMoveBlock")]
[Tracked]
public class ConnectedMoveBlock : ConnectedSolid
{
    // Custom Border Entity
    protected class Border : Entity
    {
        public ConnectedMoveBlock Parent;
        private static Vector2 offset = new(1, 1);

        public Border(ConnectedMoveBlock parent)
        {
            Parent = parent;
            Depth = parent.BGRenderer.Depth + 1;
        }

        public override void Update()
        {
            if (Parent.Scene != Scene)
            {
                RemoveSelf();
            }
            base.Update();
        }

        public override void Render()
        {
            foreach (Hitbox hitbox in Parent.AllColliders)
            {
                if (Parent.outline)
                    Draw.Rect(hitbox.Position + Parent.Position + Parent.Shake - offset, hitbox.Width + 2f, hitbox.Height + 2f, Color.Black);

                float num = Parent.flash * 4f;
                if (Parent.flash > 0f)
                {
                    Draw.Rect(hitbox.Position + Parent.Position - new Vector2(num, num), hitbox.Width + (2f * num), hitbox.Height + (2f * num), Color.White * Parent.flash);
                }
            }
        }
    }

    public enum MovementState
    {
        Idling,
        Moving,
        Breaking
    }

    public MovementState State;

    public MoveBlockGroup Group { get; internal set; }
    public bool GroupSignal { get; internal set; }
    public bool CheckGroupRespawn { get; internal set; }

    protected static MTexture[,] masterEdges = new MTexture[3, 3];
    protected static MTexture[,] masterInnerCorners = new MTexture[2, 2];
    protected static List<MTexture> masterArrows = new();
    protected MTexture xTexture;

    //Custom Texture support
    protected bool customTexture;
    protected Tuple<MTexture[,], MTexture[,]> tiles;
    protected List<MTexture> arrows;

    //Custom Sound support
    protected string ActivateSoundEffect = SFX.game_04_arrowblock_activate;
    protected string BreakSoundEffect = SFX.game_04_arrowblock_break;
    protected string ReformBeginSoundEffect = SFX.game_04_arrowblock_reform_begin;
    protected string ReappearSoundEffect = SFX.game_04_arrowblock_reappear;

    //ColorModifiers added to entityData constructor.
    protected readonly Color idleBgFill = Calc.HexToColor("474070");
    protected readonly Color pressedBgFill = Calc.HexToColor("30b335");
    protected readonly Color breakingBgFill = Calc.HexToColor("cc2541");
    protected Color fillColor;

    protected float particleRemainder;

    protected Vector2 startPosition;

    public MoveBlock.Directions Direction;

    protected List<Hitbox> ArrowsList;

    protected float moveSpeed;
    protected bool triggered;

    protected float speed;
    protected float targetSpeed;

    protected float angle;
    protected float targetAngle;
    protected float homeAngle;

    protected float flash;
    protected Border border;

    protected Player noSquish;

    protected SoundSource moveSfx;

    // Flag options
    // A list of sets of flags. When all in a set are on (or off, and preceded by !), the move block will activate.
    // The flag "_pressed" is considered to be on when the player is riding the block. This is the only set by default.
    // Sets are separated by '|' and flags in a set are seperated by ','.
    protected List<List<string>> ActivatorFlags = new();
    // A list of flags to be enabled (or disabled when preceded by !, toggled when preceded by ~) when the move block activates.
    protected List<string> OnActivateFlags = new();
    // A list of sets of flags. When all are on (or off, and preceded by !), the move block will immediately break, and will not respawn until off.
    // The flag "_obstructed" is considered to be on when the block is obstructed (and not broken) by a solid or screen edge. This is the only set by default.
    // Sets are separated by '|' and flags in a set are seperated by ','.
    protected List<List<string>> BreakerFlags = new();
    // A list of flags to be enabled (or disabled when preceded by !, toggled when preceded by ~) when the move block breaks.
    protected List<string> OnBreakFlags = new();
    // If true, OnBreakFlags will not be set if the block breaks inside a seeker barrier.
    protected bool BarrierBlocksFlags = false;
    // If true, the block will not appear until breaker flags are disabled.
    protected bool WaitForFlags = false;

    protected bool curMoveCheck = false;

    private readonly bool outline;

    public ConnectedMoveBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Enum<MoveBlock.Directions>("direction"), data.Bool("fast") ? 75f : data.Float("moveSpeed", 60f))
    {
        idleBgFill = Util.TryParseColor(data.Attr("idleColor", "474070"));
        pressedBgFill = Util.TryParseColor(data.Attr("pressedColor", "30b335"));
        breakingBgFill = Util.TryParseColor(data.Attr("breakColor", "cc2541"));
        fillColor = idleBgFill;
        string customTexturePath = data.Attr("customBlockTexture").Trim().TrimEnd('/');
        GFX.Game.PushFallback(null);
        customTexture = !string.IsNullOrWhiteSpace(customTexturePath);
        if (customTexture)
        {
            string temp;
            if (!GFX.Game.Has("objects/" + customTexturePath))
            {
                if (GFX.Game["objects/" + customTexturePath + "/tileset"] == null)
                {
                    throw new Exception($"No valid tileset found, searched @ objects/{customTexturePath}.png & objects/{customTexturePath}/tileset.png\nFor custom arrow textures, use 'objects/{customTexturePath}/arrow', 'objects/{customTexturePath}/tileset' for tiles, and 'objects/{customTexturePath}/x.png' for the breaking X sprite.");
                }

                arrows = GFX.Game.GetAtlasSubtextures("objects/" + customTexturePath + "/arrow");
                if (arrows.Count != 8)
                {
                    Util.Log("Invalid or no custom arrow textures found, defaulting to normal.");
                    arrows = null;
                }
                temp = customTexturePath + "/tileset";
                xTexture = GFX.Game[$"objects/{customTexturePath}/x"];
                if (xTexture == null)
                {
                    Util.Log("No breaking texture found, defaulting to normal");
                    xTexture = GFX.Game["objects/moveBlock/x"];
                }
            }
            else
            {
                List<string> temp1 = new();
                temp1.AddRange(customTexturePath.Split('/'));
                temp1.RemoveAt(temp1.Count - 1);
                string temp2 = string.Join("/", temp1);
                arrows = GFX.Game.GetAtlasSubtextures("objects/" + temp2 + "/arrow");
                if (arrows.Count != 8)
                {
                    Util.Log("Invalid or no custom arrow textures found, defaulting to normal.");
                    arrows = null;
                }
                temp = customTexturePath;
                xTexture = GFX.Game[$"objects/{temp2}/x"];
                if (xTexture == null)
                {
                    Util.Log("No breaking texture found, defaulting to normal");
                    xTexture = GFX.Game["objects/moveBlock/x"];
                }
            }
            tiles = SetupCustomTileset(temp);

        }
        else
        {
            xTexture = GFX.Game["objects/moveBlock/x"];
        }
        GFX.Game.PopFallback();

        LoadCustomSounds(data.Attr("customSoundEffect"));

        ActivatorFlags.AddRange(data.Attr("activatorFlags", "_pressed").Split('|').Select(l => l.Split(',').ToList()));
        BreakerFlags.AddRange(data.Attr("breakerFlags", "_obstructed").Split('|').Select(l => l.Split(',').ToList()));
        OnActivateFlags.AddRange(data.Attr("onActivateFlags", "").Split(','));
        OnBreakFlags.AddRange(data.Attr("onBreakFlags", "").Split(','));
        BarrierBlocksFlags = data.Bool("barrierBlocksFlags", false);
        WaitForFlags = data.Bool("waitForFlags", false);

        outline = data.Bool("outline", true);
    }

    public ConnectedMoveBlock(Vector2 position, int width, int height, MoveBlock.Directions direction, float moveSpeed)
        : base(position, width, height, safe: false)
    {

        Depth = Depths.Player - 1;
        startPosition = position;
        Direction = direction;
        this.moveSpeed = moveSpeed;

        homeAngle = targetAngle = angle = direction.Angle();
        Add(moveSfx = new SoundSource());
        Add(new Coroutine(Controller()));
        UpdateColors();
        Add(new LightOcclude(0.5f));
    }

    public override void OnStaticMoverTrigger(StaticMover sm)
    {
        base.OnStaticMoverTrigger(sm);

        // If this move block has the "_pressed" flag, meaning it can be manually activated by the player,
        // then it will be activated by a static mover being triggered.
        if (ActivatorFlags.Any(set => set.Any(flag => flag == "_pressed")))
            triggered = true;
    }

    protected virtual IEnumerator Controller()
    {
        // If we're waiting for flags before becoming visible, start off invisible.
        bool startInvisible = AnySetEnabled(BreakerFlags) && WaitForFlags;
        if (startInvisible)
            Visible = Collidable = false;
        while (true)
        {
            bool startingBroken = false, startingByActivator = false;
            curMoveCheck = false;
            triggered = false;
            State = MovementState.Idling;
            while (!triggered && !startingByActivator && !startingBroken && !GroupSignal)
            {
                if (startInvisible && !AnySetEnabled(BreakerFlags))
                {
                    goto Rebuild;
                }
                yield return null;
                startingBroken = AnySetEnabled(BreakerFlags) && !startInvisible;
                startingByActivator = AnySetEnabled(ActivatorFlags);
            }

            if (Group is not null && Group.SyncActivation)
            {
                if (!GroupSignal)
                    Group.Trigger(); // block was manually triggered
                // ensures all moveblock in the group start simultaneously
                while (!GroupSignal) // wait for signal to come back
                    yield return null;
                GroupSignal = false; // reset
            }

            Audio.Play(ActivateSoundEffect, Position);
            State = MovementState.Moving;
            StartShaking(0.2f);
            ActivateParticles();
            if (!startingBroken)
            {
                foreach (string flag in OnActivateFlags)
                {
                    if (flag.Length > 0)
                        if (flag.StartsWith("!"))
                        {
                            SceneAs<Level>().Session.SetFlag(flag.Substring(1), false);
                        }
                        else if (flag.StartsWith("~"))
                        {
                            SceneAs<Level>().Session.SetFlag(flag.Substring(1), SceneAs<Level>().Session.GetFlag(flag.Substring(1)));
                        }
                        else
                            SceneAs<Level>().Session.SetFlag(flag);
                }
            }
            else
                State = MovementState.Breaking;
            yield return 0.2f;

            targetSpeed = moveSpeed;
            moveSfx.Play(SFX.game_04_arrowblock_move_loop);
            moveSfx.Param("arrow_stop", 0f);
            StopPlayerRunIntoAnimation = false;
            float crashTimer = 0.15f;
            float crashResetTimer = 0.1f;
            while (true)
            {
                if (Scene.OnInterval(0.02f))
                {
                    MoveParticles();
                }
                speed = startingBroken ? 0 : Calc.Approach(speed, targetSpeed, 300f * Engine.DeltaTime);
                angle = Calc.Approach(angle, targetAngle, (float) Math.PI * 16f * Engine.DeltaTime);
                Vector2 vec = Calc.AngleToVector(angle, speed) * Engine.DeltaTime;
                bool flag2;
                Vector2 start = Position;
                if (Direction is MoveBlock.Directions.Right or MoveBlock.Directions.Left)
                {
                    flag2 = MoveCheck(vec.XComp());
                    noSquish = Scene.Tracker.GetEntity<Player>();
                    MoveVCollideSolids(vec.Y, thruDashBlocks: false);
                    noSquish = null;
                }
                else
                {
                    flag2 = MoveCheck(vec.YComp());
                    noSquish = Scene.Tracker.GetEntity<Player>();
                    MoveHCollideSolids(vec.X, thruDashBlocks: false);
                    noSquish = null;
                    if (Direction == MoveBlock.Directions.Down && Top > SceneAs<Level>().Bounds.Bottom + 32)
                    {
                        flag2 = true;
                    }
                }
                Vector2 move = Position - start;
                if (Scene.OnInterval(0.03f))
                    SpawnScrapeParticles(Math.Abs(move.X) != 0, Math.Abs(move.Y) != 0);

                curMoveCheck = flag2;

                if (startingBroken || AnySetEnabled(BreakerFlags))
                {
                    moveSfx.Param("arrow_stop", 1f);
                    crashResetTimer = 0.1f;
                    if (!(crashTimer > 0f))
                    {
                        break;
                    }
                    crashTimer -= Engine.DeltaTime;
                }
                else
                {
                    moveSfx.Param("arrow_stop", 0f);
                    if (crashResetTimer > 0f)
                    {
                        crashResetTimer -= Engine.DeltaTime;
                    }
                    else
                    {
                        crashTimer = 0.15f;
                    }
                }
                Level level = Scene as Level;
                if (Left < level.Bounds.Left || Top < level.Bounds.Top || Right > level.Bounds.Right)
                {
                    break;
                }
                yield return null;
            }

            Audio.Play(BreakSoundEffect, Position);
            moveSfx.Stop();
            State = MovementState.Breaking;
            speed = targetSpeed = 0f;
            angle = targetAngle = homeAngle;
            StartShaking(0.2f);
            StopPlayerRunIntoAnimation = true;
            yield return 0.2f;

            BreakParticles();

            List<MoveBlockDebris> debris = new();
            int tWidth = (int) ((GroupBoundsMax.X - GroupBoundsMin.X) / 8);
            int tHeight = (int) ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8);

            for (int i = 0; i < tWidth; i++)
            {
                for (int j = 0; j < tHeight; j++)
                {
                    if (AllGroupTiles[i, j])
                    {
                        Vector2 value = new((i * 8) + 4, (j * 8) + 4);
                        Vector2 pos = value + Position + GroupOffset;
                        MoveBlockDebris debris2 = Engine.Pooler.Create<MoveBlockDebris>().Init(pos, GroupCenter, startPosition + GroupOffset + value);
                        debris.Add(debris2);
                        Scene.Add(debris2);
                    }
                }
            }
            MoveStaticMovers(startPosition - Position);
            DisableStaticMovers();

            bool shouldProcessBreakFlags = true;
            if (BarrierBlocksFlags)
            {
                bool colliding = false;
                foreach (SeekerBarrier entity in Scene.Tracker.GetEntities<SeekerBarrier>())
                {
                    entity.Collidable = true;
                    bool collision = CollideCheck(entity);
                    colliding |= collision;
                    entity.Collidable = false;
                }
                shouldProcessBreakFlags = !colliding;
            }

            Position = startPosition;
            Visible = Collidable = false;

            if (shouldProcessBreakFlags)
                foreach (string flag in OnBreakFlags)
                {
                    if (flag.Length > 0)
                    {
                        if (flag.StartsWith("!"))
                        {
                            SceneAs<Level>().Session.SetFlag(flag.Substring(1), false);
                        }
                        else if (flag.StartsWith("~"))
                        {
                            SceneAs<Level>().Session.SetFlag(flag.Substring(1), SceneAs<Level>().Session.GetFlag(flag.Substring(1)));
                        }
                        else
                            SceneAs<Level>().Session.SetFlag(flag);
                    }
                }
            curMoveCheck = false;
            yield return 2.2f;

            if (Group is not null)
            {
                CheckGroupRespawn = true;
                while (!Group.CanRespawn(this))
                    yield return null;
            }

            foreach (MoveBlockDebris item in debris)
            {
                item.StopMoving();
            }
            while (CollideCheck<Actor>() || CollideCheck<Solid>() || AnySetEnabled(BreakerFlags))
            {
                yield return null;
            }

            Collidable = true;
            EventInstance instance = Audio.Play(ReformBeginSoundEffect, debris[0].Position);
            Coroutine component;
            Coroutine routine = component = new Coroutine(SoundFollowsDebrisCenter(instance, debris));
            Add(component);
            foreach (MoveBlockDebris item2 in debris)
            {
                item2.StartShaking();
            }
            yield return 0.2f;

            foreach (MoveBlockDebris item3 in debris)
            {
                item3.ReturnHome(0.65f);
            }
            yield return 0.6f;

            routine.RemoveSelf();
            foreach (MoveBlockDebris item4 in debris)
            {
                item4.RemoveSelf();
            }

            CheckGroupRespawn = false;
        Rebuild:
            Audio.Play(ReappearSoundEffect, Position);
            Visible = true;
            Collidable = true;
            EnableStaticMovers();
            speed = targetSpeed = 0f;
            angle = targetAngle = homeAngle;
            noSquish = null;
            fillColor = idleBgFill;
            UpdateColors();
            flash = 1f;
            startInvisible = false;
        }
    }

    protected IEnumerator SoundFollowsDebrisCenter(EventInstance instance, List<MoveBlockDebris> debris)
    {
        while (true)
        {
            instance.getPlaybackState(out PLAYBACK_STATE pLAYBACK_STATE);
            if (pLAYBACK_STATE == PLAYBACK_STATE.STOPPED)
            {
                break;
            }
            Vector2 zero = Vector2.Zero;
            foreach (MoveBlockDebris debri in debris)
            {
                zero += debri.Position;
            }
            zero /= debris.Count;
            Audio.Position(instance, zero);
            yield return null;
        }
    }

    protected void LoadCustomSounds(string customSoundEffectPath)
    {
        static void LoadSfxIfPresent(string sfxPath, ref string target)
        {
            if (Audio.GetEventDescription(sfxPath) != null)
            {
                target = sfxPath;
            }
        }


        customSoundEffectPath = customSoundEffectPath.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(customSoundEffectPath))
        {
            if (!customSoundEffectPath.StartsWith("event:/"))
            {
                customSoundEffectPath = customSoundEffectPath.TrimStart('/');
                customSoundEffectPath = $"event:/{customSoundEffectPath}";
            }

            LoadSfxIfPresent($"{customSoundEffectPath}_activate", ref ActivateSoundEffect);
            LoadSfxIfPresent($"{customSoundEffectPath}_break", ref BreakSoundEffect);
            LoadSfxIfPresent($"{customSoundEffectPath}_reform_begin", ref ReformBeginSoundEffect);
            LoadSfxIfPresent($"{customSoundEffectPath}_reappear", ref ReappearSoundEffect);
        }
    }

    protected void UpdateColors()
    {
        Color value = State switch
        {
            MovementState.Moving => pressedBgFill,
            MovementState.Breaking => breakingBgFill,
            _ => idleBgFill,
        };
        fillColor = Color.Lerp(fillColor, value, 10f * Engine.DeltaTime);
    }

    public override void MoveHExact(int move)
    {
        if (noSquish != null && ((move < 0 && noSquish.X < X) || (move > 0 && noSquish.X > X)))
        {
            while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + (Vector2.UnitX * move)))
            {
                move -= Math.Sign(move);
            }
        }
        base.MoveHExact(move);
    }

    public override void MoveVExact(int move)
    {
        if (noSquish != null && move < 0 && noSquish.Y <= Y)
        {
            while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + (Vector2.UnitY * move)))
            {
                move -= Math.Sign(move);
            }
        }
        base.MoveVExact(move);
    }

    protected bool MoveCheck(Vector2 speed)
    {
        if (speed.X != 0f)
        {
            if (MoveHCollideSolids(speed.X, thruDashBlocks: false))
            {
                for (int i = 1; i <= 3; i++)
                {
                    for (int num = 1; num >= -1; num -= 2)
                    {
                        Vector2 value = new(Math.Sign(speed.X), i * num);
                        if (!CollideCheck<Solid>(Position + value))
                        {
                            MoveVExact(i * num);
                            MoveHExact(Math.Sign(speed.X));
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }
        if (speed.Y != 0f)
        {
            if (MoveVCollideSolids(speed.Y, thruDashBlocks: false))
            {
                for (int j = 1; j <= 3; j++)
                {
                    for (int num2 = 1; num2 >= -1; num2 -= 2)
                    {
                        Vector2 value2 = new(j * num2, Math.Sign(speed.Y));
                        if (!CollideCheck<Solid>(Position + value2))
                        {
                            MoveHExact(j * num2);
                            MoveVExact(Math.Sign(speed.Y));
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }
        return false;
    }

    protected void ActivateParticles()
    {
        foreach (Hitbox hitbox in Colliders)
        {
            bool left = !CollideCheck<Player>(Position - Vector2.UnitX);
            bool right = !CollideCheck<Player>(Position + Vector2.UnitX);
            bool top = !CollideCheck<Player>(Position - Vector2.UnitY);

            if (left)
            {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Height / 2f), Position + hitbox.CenterLeft, Vector2.UnitY * (hitbox.Height - 4f) * 0.5f, (float) Math.PI);
            }
            if (right)
            {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Height / 2f), Position + hitbox.CenterRight, Vector2.UnitY * (hitbox.Height - 4f) * 0.5f, 0f);
            }
            if (top)
            {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Width / 2f), Position + hitbox.TopCenter, Vector2.UnitX * (hitbox.Width - 4f) * 0.5f, -(float) Math.PI / 2f);
            }
            SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Activate, (int) (hitbox.Width / 2f), Position + hitbox.BottomCenter, Vector2.UnitX * (hitbox.Width - 4f) * 0.5f, (float) Math.PI / 2f);
        }
    }

    protected void BreakParticles()
    {
        foreach (Hitbox hitbox in Colliders)
        {

            Vector2 center = Position + hitbox.Center;
            for (int i = 0; i < hitbox.Width; i += 4)
            {
                for (int j = 0; j < hitbox.Height; j += 4)
                {
                    Vector2 vector = Position + hitbox.Position + new Vector2(2 + i, 2 + j);
                    SceneAs<Level>().Particles.Emit(MoveBlock.P_Break, 1, vector, Vector2.One * 2f, (vector - center).Angle());
                }
            }

        }
    }

    protected void MoveParticles()
    {
        foreach (Hitbox hitbox in Colliders)
        {

            Vector2 position;
            Vector2 positionRange;
            float num;
            float num2;
            if (Direction == MoveBlock.Directions.Right)
            {
                position = hitbox.CenterLeft + Vector2.UnitX;
                positionRange = Vector2.UnitY * (hitbox.Height - 4f);
                num = (float) Math.PI;
                num2 = hitbox.Height / 32f;
            }
            else if (Direction == MoveBlock.Directions.Left)
            {
                position = hitbox.CenterRight;
                positionRange = Vector2.UnitY * (hitbox.Height - 4f);
                num = 0f;
                num2 = hitbox.Height / 32f;
            }
            else if (Direction == MoveBlock.Directions.Down)
            {
                position = hitbox.TopCenter + Vector2.UnitY;
                positionRange = Vector2.UnitX * (hitbox.Width - 4f);
                num = -(float) Math.PI / 2f;
                num2 = hitbox.Width / 32f;
            }
            else
            {
                position = hitbox.BottomCenter;
                positionRange = Vector2.UnitX * (hitbox.Width - 4f);
                num = (float) Math.PI / 2f;
                num2 = hitbox.Width / 32f;
            }
            particleRemainder += num2;
            int num3 = (int) particleRemainder;
            particleRemainder -= num3;
            positionRange *= 0.5f;
            if (num3 > 0)
            {
                SceneAs<Level>().ParticlesBG.Emit(MoveBlock.P_Move, num3, position + Position, positionRange, num);
            }

        }
    }

    protected bool AnySetEnabled(List<List<string>> sets)
    {
        return sets.Any(SetEnabled);
    }

    protected bool SetEnabled(List<string> set)
    {
        return set.All(s => s.Equals("_pressed") ? HasPlayerRider()
                          : s.Equals("_obstructed") ? curMoveCheck
                          : s.StartsWith("!") ? !SceneAs<Level>().Session.GetFlag(s.Substring(1))
                          : SceneAs<Level>().Session.GetFlag(s));
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (customTexture)
        {
            AutoTile(tiles.Item1, tiles.Item2);
        }
        else
        {
            AutoTile(masterEdges, masterInnerCorners);
        }
        scene.Add(border = new Border(this));

        // Get all the colliders that can have an arrow drawn on.
        ArrowsList = new List<Hitbox> { (Hitbox) MasterCollider };
        foreach (Hitbox hitbox in Colliders)
        {
            if (Math.Min(hitbox.Width, hitbox.Height) >= 24)
            {
                ArrowsList.Add(hitbox);
            }
        }

        // Allow this block to be redirected by MoveBlockRedirects if it has a single rectangular collider.
        if (Colliders.Length == 1)
        {
            Add(new Redirectable(new DynamicData(this))
            {
                Get_CanSteer = () => false,
                Get_Direction = () => Direction,
                Set_Direction = dir => Direction = dir,
            });
        }
    }

    public override void Update()
    {
        base.Update();
        if (moveSfx != null && moveSfx.Playing)
        {
            int num = (int) Math.Floor(((0f - (Calc.AngleToVector(angle, 1f) * new Vector2(-1f, 1f)).Angle() + ((float) Math.PI * 2f)) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f) + 0.5f);
            moveSfx.Param("arrow_influence", num + 1);
        }
        border.Visible = Visible;
        flash = Calc.Approach(flash, 0f, Engine.DeltaTime * 5f);
        UpdateColors();
    }

    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;

        foreach (Hitbox hitbox in Colliders)
        {
            Draw.Rect(hitbox.Position.X + Position.X, hitbox.Position.Y + Position.Y, hitbox.Width, hitbox.Height, fillColor);
        }

        base.Render();
        int arrowIndex = Calc.Clamp((int) Math.Floor(((0f - angle + ((float) Math.PI * 2f)) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f) + 0.5f), 0, 7);
        foreach (Hitbox hitbox in ArrowsList)
        {
            Color arrowColor = Group is null
                ? fillColor
                : Color.Lerp(fillColor, Group.Color, Calc.SineMap(Scene.TimeActive * 3, 0, 1));

            Vector2 vec = hitbox.Center + Position;
            Draw.Rect(vec.X - 4f, vec.Y - 4f, 8f, 8f, arrowColor);

            if (State != MovementState.Breaking)
            {
                if (arrows == null)
                    masterArrows[arrowIndex].DrawCentered(vec);
                else
                    arrows[arrowIndex].DrawCentered(vec);
            }
            else
                xTexture.DrawCentered(vec);
        }

        foreach (Image img in Tiles)
            Draw.Rect(img.Position + Position, 8, 8, Color.White * flash);

        Position = position;
    }

    public static void InitializeTextures()
    {
        MTexture edgeTiles = GFX.Game["objects/moveBlock/base"];
        MTexture innerTiles = GFX.Game["objects/CommunalHelper/connectedMoveBlock/innerCorners"];
        masterArrows = GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow");

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                masterEdges[i, j] = edgeTiles.GetSubtexture(i * 8, j * 8, 8, 8);
            }
        }

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                masterInnerCorners[i, j] = innerTiles.GetSubtexture(i * 8, j * 8, 8, 8);
            }
        }

    }
}
