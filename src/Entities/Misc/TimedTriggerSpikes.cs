using Celeste.Mod.CommunalHelper.Imports;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Directions = Celeste.Spikes.Directions;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity(new string[]
{
    "CommunalHelper/TimedTriggerSpikesUp = LoadUp",
    "CommunalHelper/TimedTriggerSpikesDown = LoadDown",
    "CommunalHelper/TimedTriggerSpikesLeft = LoadLeft",
    "CommunalHelper/TimedTriggerSpikesRight = LoadRight"
})]
public class TimedTriggerSpikes : Entity
{
    protected struct SpikeInfo
    {
        public TimedTriggerSpikes Parent;

        public int Index;

        public int TextureIndex;

        public Vector2 Position;

        private bool triggered;
        public bool Triggered
        {
            get => Parent.grouped ? Parent.Triggered : triggered;
            set
            {
                if (Parent.grouped)
                    Parent.Triggered = value;
                else
                    triggered = value;
            }
        }

        public float RetractTimer;
        public float DelayTimer;

        public float Lerp;

        public Color color;

        public void Update()
        {
            if (Triggered)
            {
                if (DelayTimer > 0f)
                {
                    DelayTimer -= Engine.DeltaTime;
                    if (DelayTimer <= 0f)
                    {
                        var check = PlayerCheck();
                        if (check || (Parent.grouped && Parent.waitForPlayer && Parent.playerPresent))
                        {
                            DelayTimer = 0.05f;
                        }
                        else
                        {
                            Audio.Play(SFX.game_03_fluff_tendril_emerge, Parent.Position + Position);
                        }
                    }
                }
                else
                {
                    Lerp = Calc.Approach(Lerp, 1f, 8f * Engine.DeltaTime);
                }
            }
            else
            {
                Lerp = Calc.Approach(Lerp, 0f, 4f * Engine.DeltaTime);
                if (Lerp <= 0f)
                {
                    Triggered = false;
                }
            }
            if (Parent.rainbow)
                color = GetHue(Parent.Scene, Parent.Position + Position);
        }

        public bool PlayerCheck()
        {
            return Parent.PlayerCheck(Index);
        }

        public bool OnPlayer(Player player, Vector2 outwards)
        {
            if (!Triggered)
            {
                Audio.Play(SFX.game_03_fluff_tendril_touch, Parent.Position + Position);
                Triggered = true;
                RetractTimer = RetractTime;
                return false;
            }

            if (Lerp >= 1f)
            {
                player.Die(outwards);
                return true;
            }
            return false;
        }
    }

    #region Loading

    public static Entity LoadUp(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        return new TimedTriggerSpikes(entityData, offset, Directions.Up);
    }

    public static Entity LoadDown(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        return new TimedTriggerSpikes(entityData, offset, Directions.Down);
    }

    public static Entity LoadLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        return new TimedTriggerSpikes(entityData, offset, Directions.Left);
    }

    public static Entity LoadRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        return new TimedTriggerSpikes(entityData, offset, Directions.Right);
    }

    #endregion

    // Used to maintain compatibility with Maddie's Helping Hand RainbowSpinnerColorController
    private static readonly CrystalStaticSpinner crystalSpinner = new(Vector2.Zero, false, CrystalColor.Rainbow);
    [MethodImpl(MethodImplOptions.NoInlining)] // No in-lining, method implemented by IL hook
    public static Color GetHue(Scene scene, Vector2 position)
    {
        Console.Error.Write("NoInlining");
        throw new NoInliningException();
    }

    private const float RetractTime = 6f;

    protected float Delay = 0.4f;
    protected bool Triggered = false;

    private readonly bool waitForPlayer;

    private readonly int size;

    private readonly Directions direction;

    private readonly string overrideType;

    private Vector2 outwards;

    private Vector2 shakeOffset;

    private readonly bool grouped = false;
    private readonly bool rainbow = false;

    private string spikeType;
    private SpikeInfo[] spikes;
    private List<MTexture> spikeTextures;

    private readonly bool triggerAlways;


    public TimedTriggerSpikes(EntityData data, Vector2 offset, Directions dir)
        : this(data.Position, offset, GetSize(data, dir), dir, data.Attr("type", "default"), data.Float("Delay", 0.4f), data.Bool("WaitForPlayer", false), data.Bool("Grouped", false), data.Bool("Rainbow", false), data.Bool("TriggerAlways", false))
    {
    }

    public TimedTriggerSpikes(Vector2 position, Vector2 offset, int size, Directions direction, string overrideType, float Delay, bool waitForPlayer, bool grouped, bool rainbow, bool triggerAlways)
        : base(position + offset)
    {
        if (grouped && !OptionalDependencies.MaxHelpingHandLoaded)
        {
            throw new Exception("Grouped Timed Trigger Spikes attempted to load without Maddie's Helping Hand as a dependency.");
        }

        if (rainbow && !OptionalDependencies.VivHelperLoaded)
        {
            throw new Exception("Rainbow Timed Trigger Spikes attempted to load without Viv's Helper as a dependency.");
        }

        this.size = size;
        this.direction = direction;
        this.overrideType = overrideType;
        this.Delay = Delay;
        this.waitForPlayer = waitForPlayer;
        this.grouped = grouped;
        this.rainbow = rainbow;
        this.triggerAlways = triggerAlways;

        SafeGroundBlocker safeGroundBlocker = null;
        LedgeBlocker ledgeBlocker = null;

        switch (direction)
        {
            case Directions.Up:
                outwards = new Vector2(0f, -1f);
                Collider = new Hitbox(size, 3f, 0f, -3f);
                Add(safeGroundBlocker = new SafeGroundBlocker());
                Add(ledgeBlocker = new LedgeBlocker(UpSafeBlockCheck));
                break;

            case Directions.Down:
                outwards = new Vector2(0f, 1f);
                Collider = new Hitbox(size, 3f);
                // note: we set Blocking = false, set to true only using GravityHelper
                Add(safeGroundBlocker = new SafeGroundBlocker() { Blocking = false });
                Add(ledgeBlocker = new LedgeBlocker(UpSafeBlockCheck) { Blocking = false });
                break;

            case Directions.Left:
                outwards = new Vector2(-1f, 0f);
                Collider = new Hitbox(3f, size, -3f);
                Add(safeGroundBlocker = new SafeGroundBlocker());
                Add(ledgeBlocker = new LedgeBlocker(SideSafeBlockCheck));
                break;

            case Directions.Right:
                outwards = new Vector2(1f, 0f);
                Collider = new Hitbox(3f, size);
                Add(safeGroundBlocker = new SafeGroundBlocker());
                Add(ledgeBlocker = new LedgeBlocker(SideSafeBlockCheck));
                break;
        }

        // GravityHelper listener to enable inverted ledge blocks & safe ground blockers
        Component listener = GravityHelper.CreatePlayerGravityListener?.Invoke((_, value, _) =>
        {
            bool active = direction == Directions.Up ^ value == (int) GravityType.Inverted;
            safeGroundBlocker.Blocking = ledgeBlocker.Blocking = active;
        });
        if (listener is not null)
            Add(listener);

        Add(new PlayerCollider(OnCollide));
        Add(new StaticMover
        {
            OnShake = OnShake,
            SolidChecker = IsRiding,
            JumpThruChecker = IsRiding
        });

        Depth = Depths.Dust;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        AreaData areaData = AreaData.Get(scene);
        spikeType = areaData.Spike;
        if (!string.IsNullOrEmpty(overrideType) && overrideType != "default")
        {
            spikeType = overrideType;
        }
        if (spikeType == "tentacles")
        {
            throw new NotSupportedException("Trigger tentacles currently not supported");
        }

        spikes = new SpikeInfo[size / 8];
        string str = direction.ToString().ToLower();
        spikeTextures = GFX.Game.GetAtlasSubtextures("danger/spikes/" + spikeType + "_" + str);
        for (int i = 0; i < spikes.Length; i++)
        {
            spikes[i].Parent = this;
            spikes[i].Index = i;
            spikes[i].Position = direction switch
            {
                Directions.Up => (Vector2.UnitX * (i + 0.5f) * 8f) + Vector2.UnitY,
                Directions.Down => (Vector2.UnitX * (i + 0.5f) * 8f) - Vector2.UnitY,
                Directions.Left => (Vector2.UnitY * (i + 0.5f) * 8f) + Vector2.UnitX,
                Directions.Right => (Vector2.UnitY * (i + 0.5f) * 8f) - Vector2.UnitX,
                _ => throw new NotImplementedException(),
            };
            spikes[i].DelayTimer = Delay;
            spikes[i].color = Color.White;
        }
    }

    private void OnShake(Vector2 amount)
    {
        shakeOffset += amount;
    }

    private bool UpSafeBlockCheck(Player player)
    {
        int dir = 8 * (int) player.Facing;
        int left = (int) ((player.Left + dir - Left) / 8f);
        int right = (int) ((player.Right + dir - Left) / 8f);

        if (right < 0 || left >= spikes.Length)
            return false;

        left = Math.Max(left, 0);
        right = Math.Min(right, spikes.Length - 1);
        for (int i = left; i <= right; i++)
            if (spikes[i].Lerp >= 1f)
                return true;

        return false;
    }

    private bool SideSafeBlockCheck(Player player)
    {
        int top = (int) ((player.Top - Top) / 4f);
        int bottom = (int) ((player.Bottom - Top) / 4f);

        if (bottom < 0 || top >= spikes.Length)
            return false;

        top = Math.Max(top, 0);
        bottom = Math.Min(bottom, spikes.Length - 1);

        for (int i = top; i <= bottom; i++)
            if (spikes[i].Lerp >= 1f)
                return true;

        return false;
    }

    private void OnCollide(Player player)
    {
        GetPlayerCollideIndex(player, out int minIndex, out int maxIndex);
        if (maxIndex >= 0 && minIndex < spikes.Length)
        {
            minIndex = Math.Max(minIndex, 0);
            maxIndex = Math.Min(maxIndex, spikes.Length - 1);

            //attempt to breakout early if player dies
            bool breakout = false;
            for (int i = minIndex; i <= maxIndex; i++)
            {

                // we need to flip the vertical speed if the player is inverted, just for the check
                float ySpeed = player.Speed.Y;
                if (GravityHelper.IsPlayerInverted?.Invoke() ?? false)
                    ySpeed *= -1f;

                //direction
                switch (direction)
                {
                    case Directions.Up:
                        if (ySpeed >= 0f || (!spikes[i].Triggered && triggerAlways))
                            breakout = !spikes[i].OnPlayer(player, outwards);
                        break;
                    case Directions.Down:
                        if (ySpeed <= 0f || (!spikes[i].Triggered && triggerAlways))
                            breakout = !spikes[i].OnPlayer(player, outwards);
                        break;
                    case Directions.Left:
                        if (player.Speed.X >= 0f || (!spikes[i].Triggered && triggerAlways))
                            breakout = !spikes[i].OnPlayer(player, outwards);
                        break;
                    case Directions.Right:
                        if (player.Speed.X <= 0f || (!spikes[i].Triggered && triggerAlways))
                            breakout = !spikes[i].OnPlayer(player, outwards);
                        break;
                }
                if (breakout)
                    break;
            }
        }
    }

    private void GetPlayerCollideIndex(Player player, out int minIndex, out int maxIndex)
    {
        minIndex = maxIndex = -1;
        switch (direction)
        {
            case Directions.Up:
                if (player.Speed.Y >= 0f || triggerAlways)
                {
                    minIndex = (int) ((player.Left - Left) / 8f);
                    maxIndex = (int) ((player.Right - Left) / 8f);
                }
                break;
            case Directions.Down:
                if (player.Speed.Y <= 0f || triggerAlways)
                {
                    minIndex = (int) ((player.Left - Left) / 8f);
                    maxIndex = (int) ((player.Right - Left) / 8f);
                }
                break;
            case Directions.Left:
                if (player.Speed.X >= 0f || triggerAlways)
                {
                    minIndex = (int) ((player.Top - Top) / 8f);
                    maxIndex = (int) ((player.Bottom - Top) / 8f);
                }
                break;
            case Directions.Right:
                if (player.Speed.X <= 0f || triggerAlways)
                {
                    minIndex = (int) ((player.Top - Top) / 8f);
                    maxIndex = (int) ((player.Bottom - Top) / 8f);
                }
                break;
        }
    }

    private bool PlayerCheck(int spikeIndex)
    {
        Player player = CollideFirst<Player>();
        if (player == null || !waitForPlayer)
        {
            return false;
        }

        GetPlayerCollideIndex(player, out int minIndex, out int maxIndex);
        return minIndex <= spikeIndex + 1 && maxIndex >= spikeIndex - 1;
    }

    private static int GetSize(EntityData data, Directions dir)
    {
        return dir <= Directions.Down ? data.Width : data.Height;
    }

    public override void Update()
    {
        base.Update();
        for (int i = 0; i < spikes.Length; i++)
        {
            spikes[i].Update();
        }
        playerPresent = CollideFirst<Player>() is not null;
    }

    public override void Render()
    {
        base.Render();
        Vector2 justify = direction switch
        {
            Directions.Up => new Vector2(0.5f, 1f),
            Directions.Down => new Vector2(0.5f, 0f),
            Directions.Left => new Vector2(1f, 0.5f),
            Directions.Right => new Vector2(0f, 0.5f),
            _ => Vector2.One * 0.5f,
        };
        for (int i = 0; i < spikes.Length; i++)
        {
            MTexture mTexture = spikeTextures[spikes[i].TextureIndex];
            Vector2 position = Position + shakeOffset + spikes[i].Position + (outwards * (-4f + (spikes[i].Lerp * 4f)));
            mTexture.DrawJustified(position, justify, spikes[i].color);
        }
    }

    private bool IsRiding(Solid solid)
    {
        return direction switch
        {
            Directions.Up => CollideCheckOutside(solid, Position + Vector2.UnitY),
            Directions.Down => CollideCheckOutside(solid, Position - Vector2.UnitY),
            Directions.Left => CollideCheckOutside(solid, Position + Vector2.UnitX),
            Directions.Right => CollideCheckOutside(solid, Position - Vector2.UnitX),
            _ => false,
        };
    }

    private bool IsRiding(JumpThru jumpThru)
    {
        return direction == Directions.Up && CollideCheck(jumpThru, Position + Vector2.UnitY);
    }

    #region Hooks

    private static IDetour hook_TimedTriggerSpikes_GetHue;
    private bool playerPresent;

    internal static void LoadDelayed()
    {
        hook_TimedTriggerSpikes_GetHue = new ILHook(
            typeof(TimedTriggerSpikes).GetMethod(nameof(GetHue)),
            TimedTriggerSpikes_GetHue);
    }

    internal static void Unload()
    {
        hook_TimedTriggerSpikes_GetHue.Dispose();
    }

    private static void TimedTriggerSpikes_GetHue(ILContext il)
    {
        FieldInfo crystalSpinner = typeof(TimedTriggerSpikes).GetField(nameof(TimedTriggerSpikes.crystalSpinner), BindingFlags.NonPublic | BindingFlags.Static);
        il.Instrs.Clear();

        ILCursor cursor = new(il);
        // TimedTriggerSpikes.crystalSpinner.Scene = scene;
        cursor.Emit(OpCodes.Ldsfld, crystalSpinner);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Call, typeof(Entity).GetProperty("Scene").GetSetMethod(true));

        // return TimedTriggerSpikes.crystalSpinner.GetHue(position);
        cursor.Emit(OpCodes.Ldsfld, crystalSpinner);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Call, typeof(CrystalStaticSpinner).GetMethod("GetHue", BindingFlags.NonPublic | BindingFlags.Instance));
        cursor.Emit(OpCodes.Ret);
    }

    #endregion

}
