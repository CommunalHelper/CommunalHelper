using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

/*
* Slow routine: Particles spray out from each end diagonally, moving inwards
* Fast routine: Particles spray outwards + diagonally from the ends
* Try to keep the timing on these the same as for DreamBlocks
* 
* Todo:
* Add Feather particles/functionality
* Add Dreamblock activate/deactivate routines
* Two modes, one uses deactivated texture and blocks dashcollides, other fades away and does not block
* Add support for PandorasBox DreamDash controller
* Add OneUse mode
* Add interaction with DreamTunnelDash
*/

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DreamTunnelEntry = LoadDreamTunnelEntry")]
[Tracked]
public class DreamTunnelEntry : AbstractPanel
{
    private struct DreamParticle
    {
        public Vector2 Position;
        public int Layer;
        public Color Color;
        public float TimeOffset;
    }

    #region Loading

    public static Entity LoadDreamTunnelEntry(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        Spikes.Directions orientation = entityData.Enum<Spikes.Directions>("orientation");
        return new DreamTunnelEntry(entityData.Position + offset,
            GetSize(entityData, orientation),
            orientation,
            entityData.Bool("overrideAllowStaticMovers"),
            entityData.Int("depth", Depths.FakeWalls));
    }

    #endregion

    public bool PlayerHasDreamDash;
    private LightOcclude occlude;

    public Vector2 Shake => shake + platformShake;
    private Vector2 shake; // For use in Activation/Deactivation routines

    public float Whitefill;
    public float WhiteHeight;

    private float animTimer;
    private float wobbleEase;
    private float wobbleFrom;
    private float wobbleTo;

    private Vector2? lockedCamera;

    private DreamParticle[] particles;
    private readonly MTexture[] particleTextures;

    private readonly int originalDepth = Depths.FakeWalls;

    public DreamTunnelEntry(Vector2 position, float size, Spikes.Directions orientation, bool overrideAllowStaticMovers, int depth)
        : base(position, size, orientation, overrideAllowStaticMovers)
    {
        Depth = depth - 10;
        originalDepth = depth;

        surfaceSoundIndex = SurfaceIndex.DreamBlockInactive;

        particleTextures = new MTexture[] {
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7, null),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7, null),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7, null),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7, null)
        };
    }

    protected override DashCollisionResults OnDashCollide(DashCollision orig, Player player, Vector2 dir)
    {
        // Correct position/ignore if only partially intersecting
        // Have to use `player.DashDir` instead of `dir` because we need the actual dash direction, not the collision direction
        if (PlayerHasDreamDash)
        {
            switch (Orientation)
            {
                case Spikes.Directions.Up:
                    if (dir.Y > 0 && TryCollidePlayer(player, Vector2.UnitY, player.DashDir))
                    {
                        return DashCollisionResults.Ignore;
                    }
                    break;
                case Spikes.Directions.Down:
                    if (dir.Y < 0 && TryCollidePlayer(player, -Vector2.UnitY, player.DashDir))
                    {
                        return DashCollisionResults.Ignore;
                    }
                    break;
                case Spikes.Directions.Left:
                    if (dir.X > 0 && TryCollidePlayer(player, Vector2.UnitX, player.DashDir))
                    {
                        return DashCollisionResults.Ignore;
                    }
                    break;
                case Spikes.Directions.Right:
                    if (dir.X < 0 && TryCollidePlayer(player, -Vector2.UnitX, player.DashDir))
                    {
                        return DashCollisionResults.Ignore;
                    }
                    break;
            }
        }
        else
        {
            switch (Orientation)
            {
                case Spikes.Directions.Up when dir.Y > 0:
                case Spikes.Directions.Down when dir.Y < 0:
                    if (player.Left > Left - 4 || !Scene.CollideCheck<Solid>(CenterLeft - Vector2.UnitX) ||
                        player.Right < Right + 4 || !Scene.CollideCheck<Solid>(CenterRight + Vector2.UnitX))
                        return DashCollisionResults.NormalCollision;
                    break;
                case Spikes.Directions.Left when dir.X > 0:
                case Spikes.Directions.Right when dir.X < 0:
                    if (player.Top > Top - 4 || (!Scene.CollideCheck<Solid>(TopCenter - Vector2.UnitY) &&
                        player.Bottom < Bottom + 4) || !Scene.CollideCheck<Solid>(BottomCenter + Vector2.UnitY))
                        return DashCollisionResults.NormalCollision;
                    break;
            }
        }
        return base.OnDashCollide(orig, player, dir);
    }

    private bool TryCollidePlayer(Player player, Vector2 offset, Vector2 dir)
    {
        Vector2 at = player.Position + offset;
        if (!player.CollideCheck(this, at))
            return false;

        bool changeState = true;
        if (Orientation is Spikes.Directions.Left or Spikes.Directions.Right)
        {
            if (player.Top < Top)
            {
                if (Top - player.Top <= 4)
                    player.Top = Top;
                else if (dir.Y == 0 && TryCorrectPlayerPosition(player, new Vector2(at.X, Top)))
                    changeState = false;
                else if (dir.Y != 0 && !Scene.CollideCheck<Solid>(TopCenter - Vector2.UnitY))
                {
                    ; // Messy
                }
                else
                    return false;
            }
            else if (player.Bottom > Bottom)
            {
                if (player.Bottom - Bottom <= 4)
                    player.Bottom = Bottom;
                else if (dir.Y == 0 && TryCorrectPlayerPosition(player, new Vector2(at.X, Bottom + player.Height)))
                    changeState = false;
                else if (dir.Y != 0 && !Scene.CollideCheck<Solid>(BottomCenter + Vector2.UnitY))
                {
                    ; // Messy
                }
                else
                    return false;
            }
        }
        else if (Orientation is Spikes.Directions.Up or Spikes.Directions.Down)
        {
            if (player.Left < Left)
            {
                // Sorry for my jank
                if (!(player.OnGround() && !player.CollideCheck<Solid>(new Vector2(Left - (player.Width / 2), at.Y))))
                {
                    if (Left - player.Left <= 4)
                        player.Left = Left;
                    else if (dir.X == 0 && TryCorrectPlayerPosition(player, new Vector2(Left - (player.Width / 2), at.Y)))
                        changeState = false;
                    else if (dir.X != 0 && !Scene.CollideCheck<Solid>(CenterLeft - Vector2.UnitX))
                    {
                        ; // Messy
                    }
                    else
                        return false;
                }
            }
            else if (player.Right > Right)
            {
                if (!(player.OnGround() && !player.CollideCheck<Solid>(new Vector2(Left - (player.Width / 2), at.Y))))
                {
                    if (player.Right - Right <= 4)
                        player.Right = Right;
                    else if (dir.X == 0 && TryCorrectPlayerPosition(player, new Vector2(Right + (player.Width / 2), at.Y)))
                        changeState = false;
                    else if (dir.X != 0 && !Scene.CollideCheck<Solid>(CenterRight + Vector2.UnitX))
                    {
                        ; // Messy
                    }
                    else
                        return false;
                }
            }
        }

        if (changeState)
        {
            player.StateMachine.State = StDreamTunnelDash;
        }

        return true;
    }

    private bool TryCorrectPlayerPosition(Player player, Vector2 at)
    {
        if (!player.CollideCheck<Solid>(at))
        {
            player.Position = at;
            return true;
        }
        return false;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        PlayerHasDreamDash = level.Session.Inventory.DreamDash;

        scene.Add(new DreamBlockDummy(this)
        {
            OnActivate = Activate,
            OnFastActivate = FastActivate,
            OnActivateNoRoutine = ActivateNoRoutine,
            OnDeactivate = Deactivate,
            OnFastDeactivate = FastDeactivate,
            OnDeactivateNoRoutine = DeactivateNoRoutine,
            OnSetup = Setup
        });

        Setup();
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        scene.Tracker.GetEntity<DreamTunnelEntryRenderer>().Track(this, originalDepth);
    }

    protected override void Destroy()
    {
        // Stop rendering the block/outline
        Scene.Tracker.GetEntity<DreamTunnelEntryRenderer>().Untrack(this, originalDepth);
        // "Lock the Camera" to keep dreamblock particles in place
        lockedCamera = SceneAs<Level>().Camera.Position;

        base.Destroy();
    }

    public override void Update()
    {
        base.Update();

        if (PlayerHasDreamDash)
        {
            animTimer += 6f * Engine.DeltaTime;
            wobbleEase += Engine.DeltaTime * 2f;
            if (wobbleEase > 1f)
            {
                wobbleEase = 0f;
                wobbleFrom = wobbleTo;
                wobbleTo = Calc.Random.NextFloat(Calc.Circle);
            }
            surfaceSoundIndex = SurfaceIndex.DreamBlockActive;
        }
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);

        scene.Tracker.GetEntity<DreamTunnelEntryRenderer>().Untrack(this, originalDepth);
    }

    public void FootstepRipple(Vector2 position)
    {
        if (PlayerHasDreamDash)
        {
            DisplacementRenderer.Burst burst = level.Displacement.AddBurst(position, 0.5f, 0f, 40f, 1f);
            burst.WorldClipCollider = Collider;
            burst.WorldClipPadding = 1;
        }
    }

    public override void Render()
    {
        Camera camera = SceneAs<Level>().Camera;
        if (Right < camera.Left || Left > camera.Right || Bottom < camera.Top || Top > camera.Bottom)
            return;
        Vector2 position = lockedCamera ?? camera.Position;
        for (int i = 0; i < particles.Length; i++)
        {
            int layer = particles[i].Layer;
            Vector2 drawPos = particles[i].Position;
            drawPos += position * (0.3f + (0.25f * layer));
            drawPos = this.PutInside(drawPos);
            MTexture mtexture;
            if (layer == 0)
            {
                int num = (int) (((particles[i].TimeOffset * 4f) + animTimer) % 4f);
                mtexture = particleTextures[3 - num];
            }
            else if (layer == 1)
            {
                int num2 = (int) (((particles[i].TimeOffset * 2f) + animTimer) % 2f);
                mtexture = particleTextures[1 + num2];
            }
            else
            {
                mtexture = particleTextures[2];
            }
            if (drawPos.X >= X + 2f && drawPos.Y >= Y + 2f && drawPos.X < Right - 2f && drawPos.Y < Bottom - 2f)
            {
                mtexture.DrawCentered(drawPos + shake, particles[i].Color * Alpha);
            }
        }
    }

    // is custom, edited a few things.
    public void WobbleLine(Vector2 from, Vector2 to, float offset, bool line, bool back)
    {
        float length = (to - from).Length();
        Vector2 vector = Vector2.Normalize(to - from);
        Vector2 vector2 = new(vector.Y, -vector.X);
        Color lineColor = PlayerHasDreamDash ? CustomDreamBlock.ActiveLineColor : CustomDreamBlock.DisabledLineColor;
        Color backColor = PlayerHasDreamDash ? CustomDreamBlock.ActiveBackColor : CustomDreamBlock.DisabledBackColor;
        if (Whitefill > 0f)
        {
            lineColor = Color.Lerp(lineColor, Color.White, Whitefill) * Alpha;
            backColor = Color.Lerp(backColor, Color.White, Whitefill) * Alpha;
        }
        float scaleFactor = 0f;
        int interval = 8;
        for (int i = 0; i < length; i += interval)
        {
            float lerp = MathHelper.Lerp(LineAmplitude(wobbleFrom + offset, i), LineAmplitude(wobbleTo + offset, i), wobbleEase);
            if (i + interval >= length)
            {
                lerp = 0f;
            }
            float num5 = Math.Min(interval, length - i);
            Vector2 vector3 = from + (vector * i) + (vector2 * scaleFactor);
            Vector2 vector4 = from + (vector * (i + num5)) + (vector2 * lerp);
            if (back)
            {
                Draw.Line(vector3 - vector2, vector4 - vector2, backColor);
                Draw.Line(vector3 - (vector2 * 2f), vector4 - (vector2 * 2f), backColor);
                Draw.Line(vector3 - (vector2 * 8f), vector4 - (vector2 * 8f), backColor * 0.95f);
                Draw.Line(vector3 - (vector2 * 9f), vector4 - (vector2 * 9f), backColor * 0.7f);
                Draw.Line(vector3 - (vector2 * 10f), vector4 - (vector2 * 10f), backColor * 0.4f);
                Draw.Line(vector3 - (vector2 * 11f), vector4 - (vector2 * 11f), backColor * 0.2f);
            }
            if (line)
                Draw.Line(vector3, vector4, lineColor);
            scaleFactor = lerp;
        }
    }

    private float LineAmplitude(float seed, float index)
    {
        return (float) (Math.Sin(seed + (index / 16f) + (Math.Sin((seed * 2f) + (index / 32f)) * Calc.Circle)) + 1.0) * 1.5f;
    }

    public void Setup()
    {
        particles = new DreamParticle[(int) (Width / 4f * (Height / 4f) * 0.5f)];
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Position = new Vector2(Calc.Random.NextFloat(Width), Calc.Random.NextFloat(Height));
            particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
            particles[i].TimeOffset = Calc.Random.NextFloat();
            particles[i].Color = Color.LightGray * (0.5f + (particles[i].Layer / 2f * 0.5f));
            if (PlayerHasDreamDash)
            {
                switch (particles[i].Layer)
                {
                    case 0:
                        particles[i].Color = Calc.Random.Choose(Calc.HexToColor("FFEF11"), Calc.HexToColor("FF00D0"), Calc.HexToColor("08a310"));
                        break;
                    case 1:
                        particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5fcde4"), Calc.HexToColor("7fb25e"), Calc.HexToColor("E0564C"));
                        break;
                    case 2:
                        particles[i].Color = Calc.Random.Choose(Calc.HexToColor("5b6ee1"), Calc.HexToColor("CC3B3B"), Calc.HexToColor("7daa64"));
                        break;
                }
            }
        }
    }

    #region Activation

    public void ActivateNoRoutine()
    {
        if (!PlayerHasDreamDash)
        {
            PlayerHasDreamDash = true;
            Setup();
            Remove(occlude);
        }
    }

    public void DeactivateNoRoutine()
    {
        if (PlayerHasDreamDash)
        {
            PlayerHasDreamDash = false;
            Setup();
            occlude ??= new LightOcclude(1f);
            Add(occlude);
            surfaceSoundIndex = SurfaceIndex.DreamBlockInactive;
        }
    }

    public IEnumerator Activate()
    {
        Logger.Log(LogLevel.Warn, "CommunalHelper", "Dreamblock activation/deactivation animations are not yet implemented for DreamTunnelEntry, and are subject to change.");
        yield return null;
        ActivateNoRoutine();
    }

    public IEnumerator FastActivate()
    {
        Logger.Log(LogLevel.Warn, "CommunalHelper", "Dreamblock activation/deactivation animations are not yet implemented for DreamTunnelEntry, and are subject to change.");
        yield return null;
        ActivateNoRoutine();
    }

    public IEnumerator Deactivate()
    {
        Logger.Log(LogLevel.Warn, "CommunalHelper", "Dreamblock activation/deactivation animations are not yet implemented for DreamTunnelEntry, and are subject to change.");
        yield return null;
        DeactivateNoRoutine();
    }

    public IEnumerator FastDeactivate()
    {
        Logger.Log(LogLevel.Warn, "CommunalHelper", "Dreamblock activation/deactivation animations are not yet implemented for DreamTunnelEntry, and are subject to change.");
        yield return null;
        DeactivateNoRoutine();
    }

    #endregion

    #region Hooks

    private static IDetour hook_Player_DashCoroutine;
    private static IDetour hook_Player_orig_WallJump;

    internal static new void Load()
    {

        hook_Player_DashCoroutine = new ILHook(
            typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
            Player_DashCoroutine);

        // Footstep Ripples
        IL.Celeste.Player.ClimbBegin += Player_ClimbBegin;
        IL.Celeste.Player.OnCollideV += Player_OnCollideV;
        hook_Player_orig_WallJump = new ILHook(
            typeof(Player).GetMethod("orig_WallJump", BindingFlags.NonPublic | BindingFlags.Instance),
            Player_orig_WallJump);

        // Add Renderer
        On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
    }

    internal static new void Unload()
    {
        hook_Player_DashCoroutine.Dispose();

        IL.Celeste.Player.ClimbBegin -= Player_ClimbBegin;
        IL.Celeste.Player.OnCollideV -= Player_OnCollideV;
        hook_Player_orig_WallJump.Dispose();

        On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
    }

    /// <summary>
    /// Handle down-diagonal dashing when standing on DreamTunnelEntry
    /// </summary>
    private static void Player_DashCoroutine(ILContext il)
    {
        /*
         * adds a check for !player.CollideCheck<DreamTunnelEntry>(player.Position + Vector2.UnitY) to
         * if (player.onGround && player.DashDir.X != 0f && player.DashDir.Y > 0f && player.Speed.Y > 0f && 
         *  (!player.Inventory.DreamDash || !player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitY)))
         */
        ILCursor cursor = new(il);
        // oof
        cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Callvirt &&
            ((MethodReference) instr.Operand).FullName == "System.Boolean Monocle.Entity::CollideCheck<Celeste.DreamBlock>(Microsoft.Xna.Framework.Vector2)");
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget().DeclaringType.GetField("<>4__this"));
        cursor.EmitDelegate<Func<bool, Player, bool>>((v, player) =>
        {
            return v || player.CollideCheck<DreamTunnelEntry>(player.Position + Vector2.UnitY);
        });
    }

    #region FootstepRipples

    private static void Player_ClimbBegin(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.MatchIsinst<DreamBlock>());
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Platform, Player, Platform>>((platform, player) =>
        {
            foreach (StaticMover sm in DynamicData.For(platform).Get<List<StaticMover>>("staticMovers"))
            {
                Vector2 origin = player.Position + new Vector2((float) player.Facing * 3, -4f);
                if (sm.Entity is DreamTunnelEntry entry
                    && (entry.Orientation is Spikes.Directions.Left or Spikes.Directions.Right)
                    && entry.CollidePoint(origin + (Vector2.UnitX * (float) player.Facing)))
                {
                    entry.FootstepRipple(origin);
                }
            }
            return platform;
        });
    }

    private static void Player_OnCollideV(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.MatchIsinst<DreamBlock>());
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Platform, Player, Platform>>((platform, player) =>
        {
            foreach (StaticMover sm in DynamicData.For(platform).Get<List<StaticMover>>("staticMovers"))
            {
                if (sm.Entity is DreamTunnelEntry entry && entry.Orientation == Spikes.Directions.Up
                    && player.CollideCheck(entry, player.Position + Vector2.UnitY))
                {
                    entry.FootstepRipple(player.Position);
                }
            }
            return platform;
        });
    }

    private static void Player_orig_WallJump(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.MatchIsinst<DreamBlock>());
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<Platform, Player, int, Platform>>((platform, player, dir) =>
        {
            foreach (StaticMover sm in DynamicData.For(platform).Get<List<StaticMover>>("staticMovers"))
            {
                if (sm.Entity is DreamTunnelEntry entry
                    && (entry.Orientation is Spikes.Directions.Left or Spikes.Directions.Right)
                    && entry.CollidePoint(player.Position - (Vector2.UnitX * dir * 4f)))
                {
                    entry.FootstepRipple(player.Position + new Vector2(0, -4f));
                }
            }
            return platform;
        });
    }

    #endregion

    private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self)
    {
        self.Level.Add(new DreamTunnelEntryRenderer()); // must add before calling orig, has shown to possibly crash otherwise.
        orig(self);
    }

    #endregion

}
