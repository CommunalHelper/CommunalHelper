using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using static Celeste.Mod.CommunalHelper.Entities.DreamTunnelDash;
using DreamParticle = Celeste.Mod.CommunalHelper.Entities.CustomDreamBlock.DreamParticle;

/*
* Slow routine: Particles spray out from each end diagonally, moving inwards
* Fast routine: Particles spray outwards + diagonally from the ends
* Try to keep the timing on these the same as for DreamBlocks
* 
* Fix dash correction
*   Figure out dashing into corner
* Improve texture
*/

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/DreamTunnelEntry = LoadDreamTunnelEntry")]
    [Tracked]
    public class DreamTunnelEntry : Entity {

        #region Loading

        public static Entity LoadDreamTunnelEntry(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            Spikes.Directions orientation = entityData.Enum<Spikes.Directions>("orientation");
            return new DreamTunnelEntry(entityData.Position + offset, 
                GetSize(entityData, orientation), 
                orientation, 
                entityData.Bool("overrideAllowStaticMovers"), 
                entityData.Bool("below"),
                entityData.Bool("featherMode"));
        }

        private static int GetSize(EntityData data, Spikes.Directions dir) {
            if (dir <= Spikes.Directions.Down) {
                return data.Width;
            }
            return data.Height;
        }

        #endregion

        public bool PlayerHasDreamDash;

        public Spikes.Directions Orientation;

        private bool overrideAllowStaticMovers;

        private StaticMover staticMover;
        private LightOcclude occlude;

        private DashCollision platformDashCollide;
        private int surfaceSoundIndex;

        private Shaker shaker;
        public Vector2 shake;
        public Vector2 start => new Vector2(
                X + (Orientation is Spikes.Directions.Right or Spikes.Directions.Down ? Width : 0),
                Y + (Orientation is Spikes.Directions.Left or Spikes.Directions.Down ? Height : 0));
        public Vector2 end => new Vector2(
                X + (Orientation is Spikes.Directions.Up or Spikes.Directions.Right ? Width : 0),
                Y + (Orientation is Spikes.Directions.Right or Spikes.Directions.Down ? Height : 0));
        public Vector2 platformShake;
        public float whiteFill;
        public float whiteHeight;

        private float animTimer;
        private float wobbleEase;
        private float wobbleFrom;
        private float wobbleTo;

        public float Alpha = 1f;
        private Vector2? lockedCamera;

        private DreamParticle[] particles;
        private MTexture[] particleTextures;

        private Level level;

        public DreamTunnelEntry(Vector2 position, float size, Spikes.Directions orientation, bool overrideAllowStaticMovers, bool below, bool featherMode)
            : base(position) {
            Depth = (below ? Depths.Solids : Depths.FakeWalls) - 10;
            Orientation = orientation;
            this.overrideAllowStaticMovers = overrideAllowStaticMovers;

            Collider = orientation switch {
                Spikes.Directions.Up => new Hitbox(size, 8f),
                Spikes.Directions.Down => new Hitbox(size, 8f),
                Spikes.Directions.Left => new Hitbox(8f, size),
                Spikes.Directions.Right => new Hitbox(8f, size)
            };

            Add(staticMover = new StaticMover {
                OnAttach = OnAttach,
                OnShake = v => platformShake += v,
                SolidChecker = IsRiding,
                OnEnable = () => Active = Visible = Collidable = true,
                OnDisable = () => Active = Visible = Collidable = false,
                OnDestroy = Destroy
            });

            surfaceSoundIndex = SurfaceIndex.DreamBlockInactive;

            particleTextures = new MTexture[]
            {
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7, null),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7, null),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7, null),
                GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7, null)
            };
        }

        private void OnAttach(Platform platform) {
            platformDashCollide = platform.OnDashCollide;
            platform.OnDashCollide = OnDashCollide;
        }

        private DashCollisionResults OnDashCollide(Player player, Vector2 dir) {
            // Correct position/ignore if only partially intersecting
            // Have to use `player.DashDir` instead of `dir` because we need the actual dash direction, not the collision direction
            if (PlayerHasDreamDash) {
                switch (Orientation) {
                    case Spikes.Directions.Up:
                        if (dir.Y > 0 && TryCollidePlayer(player, Vector2.UnitY, player.DashDir)) {
                            return DashCollisionResults.Ignore;
                        }
                        break;
                    case Spikes.Directions.Down:
                        if (dir.Y < 0 && TryCollidePlayer(player, -Vector2.UnitY, player.DashDir)) {
                            return DashCollisionResults.Ignore;
                        }
                        break;
                    case Spikes.Directions.Left:
                        if (dir.X > 0 && TryCollidePlayer(player, Vector2.UnitX, player.DashDir)) {
                            return DashCollisionResults.Ignore;
                        }
                        break;
                    case Spikes.Directions.Right:
                        if (dir.X < 0 && TryCollidePlayer(player, -Vector2.UnitX, player.DashDir)) {
                            return DashCollisionResults.Ignore;
                        }
                        break;
                }
            }
            return platformDashCollide?.Invoke(player, dir) ?? DashCollisionResults.NormalCollision;
        }

        private bool TryCollidePlayer(Player player, Vector2 offset, Vector2 dir) {
            Vector2 at = player.Position + offset;
            if (!player.CollideCheck(this, at))
                return false;

            bool changeState = true;
            if (Orientation is Spikes.Directions.Left or Spikes.Directions.Right) {
                if (player.Top < Top) {
                    if (Top - player.Top <= 4)
                        player.Top = Top;
                    else if (dir.Y == 0 && TryCorrectPlayerPosition(player, new Vector2(at.X, Top)))
                        changeState = false;
                    else if (dir.Y != 0 && !Scene.CollideCheck<Solid>(TopCenter - Vector2.UnitY))
                        ; // Messy
                    else
                        return false;
                } else if (player.Bottom > Bottom) {
                    if (player.Bottom - Bottom <= 4)
                        player.Bottom = Bottom;
                    else if (dir.Y == 0 && TryCorrectPlayerPosition(player, new Vector2(at.X, Bottom + player.Height)))
                        changeState = false;
                    else if (dir.Y != 0 && !Scene.CollideCheck<Solid>(BottomCenter + Vector2.UnitY))
                        ; // Messy
                    else
                        return false;
                }
            } else if (Orientation is Spikes.Directions.Up or Spikes.Directions.Down) { 
                if (player.Left < Left) {
                    // Sorry for my jank
                    if (!(player.OnGround() && !player.CollideCheck<Solid>(new Vector2(Left - player.Width / 2, at.Y)))) {
                        if (Left - player.Left <= 4)
                            player.Left = Left;
                        else if (dir.X == 0 && TryCorrectPlayerPosition(player, new Vector2(Left - player.Width / 2, at.Y)))
                            changeState = false;
                        else if (dir.Y != 0 && !Scene.CollideCheck<Solid>(CenterLeft - Vector2.UnitX))
                            ; // Messy
                        else
                            return false;
                    }
                } else if (player.Right > Right) {
                    if (!(player.OnGround() && !player.CollideCheck<Solid>(new Vector2(Left - player.Width / 2, at.Y)))) {
                        if (player.Right - Right <= 4)
                            player.Right = Right;
                        else if (dir.X == 0 && TryCorrectPlayerPosition(player, new Vector2(Right + player.Width / 2, at.Y)))
                            changeState = false;
                        else if (dir.Y != 0 && !Scene.CollideCheck<Solid>(CenterRight + Vector2.UnitX))
                            ; // Messy
                        else
                            return false;
                    }
                }
            }

            if (changeState) {
                player.StateMachine.State = StDreamTunnelDash;
            }

            return true;
        }

        private bool TryCorrectPlayerPosition(Player player, Vector2 at) {
            if (!player.CollideCheck<Solid>(at)) {
                player.Position = at;
                return true;
            }
            return false;
        }

        // Make sure at least one side aligns, and the rest are contained within the solid
        private bool IsRiding(Solid solid) {
            return Orientation switch {
                Spikes.Directions.Up => this.CollideCheckOutsideInside(solid, TopCenter - Vector2.UnitY * Height),
                Spikes.Directions.Down => this.CollideCheckOutsideInside(solid, BottomCenter + Vector2.UnitY),
                Spikes.Directions.Left => this.CollideCheckOutsideInside(solid, CenterLeft - Vector2.UnitX * Width),
                Spikes.Directions.Right => this.CollideCheckOutsideInside(solid, CenterRight + Vector2.UnitX),
                _ => false,
            };
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = scene as Level;
            PlayerHasDreamDash = level.Session.Inventory.DreamDash;

            scene.Add(new DreamBlockDummy() {
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

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (overrideAllowStaticMovers) {
                foreach (Entity entity in scene.GetEntitiesByTagMask(Tags.Global | Tags.Persistent)) {
                    if (entity is Solid solid && entity.Scene == scene) {
                        ForceAttachStaticMovers(solid, scene);
                    }
                }
            }

            scene.Tracker.GetEntity<DreamTunnelEntryRenderer>().Track(this);
        }

        private void OneUseDestroy() {
            Collidable = Visible = false;
            RemoveSelf();
        }

        private void Destroy() {
            Collidable = false;
            Scene.Tracker.GetEntity<DreamTunnelEntryRenderer>().Untrack(this);
            lockedCamera = SceneAs<Level>().Camera.Position;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.Linear, 1, true);
            tween.OnUpdate = t => Alpha = 1 - t.Percent;
            tween.OnComplete = delegate { RemoveSelf(); };
            Add(tween);
        }

        public override void Update() {
            if (staticMover.Platform == null) {
                RemoveSelf();
                return;
            }

            base.Update();
            if (PlayerHasDreamDash) {
                animTimer += 6f * Engine.DeltaTime;
                wobbleEase += Engine.DeltaTime * 2f;
                if (wobbleEase > 1f) {
                    wobbleEase = 0f;
                    wobbleFrom = wobbleTo;
                    wobbleTo = Calc.Random.NextFloat(Calc.Circle);
                }
                surfaceSoundIndex = SurfaceIndex.DreamBlockActive;
            }
        }

        public override void Removed(Scene scene) {
            if (staticMover.Platform != null && (staticMover.Platform.TagCheck(Tags.Global) || staticMover.Platform.TagCheck(Tags.Persistent))) {
                List<StaticMover> movers = new DynData<Platform>(staticMover.Platform).Get<List<StaticMover>>("staticMovers");
                // Iterate backwards so we can remove stuff
                for (int i = movers.Count - 1; i >= 0; i--) {
                    if (movers[i].Entity is DreamTunnelEntry entry) {
                        movers[i].Platform.OnDashCollide = entry.platformDashCollide;
                        movers.RemoveAt(i);
                    }
                }
            }
            base.Removed(scene);

            scene.Tracker.GetEntity<DreamTunnelEntryRenderer>().Untrack(this);
        }

        public void FootstepRipple(Vector2 position) {
            if (PlayerHasDreamDash) {
                DisplacementRenderer.Burst burst = level.Displacement.AddBurst(position, 0.5f, 0f, 40f, 1f);
                burst.WorldClipCollider = Collider;
                burst.WorldClipPadding = 1;
            }
        }

        public override void Render() {
            Camera camera = SceneAs<Level>().Camera;
            if (Right < camera.Left || Left > camera.Right || Bottom < camera.Top || Top > camera.Bottom)
                return;
            Vector2 position = lockedCamera ?? camera.Position;
            for (int i = 0; i < particles.Length; i++) {
                int layer = particles[i].Layer;
                Vector2 drawPos = particles[i].Position;
                drawPos += position * (0.3f + 0.25f * layer);
                drawPos = this.PutInside(drawPos);
                MTexture mtexture;
                if (layer == 0) {
                    int num = (int) ((particles[i].TimeOffset * 4f + animTimer) % 4f);
                    mtexture = particleTextures[3 - num];
                } else if (layer == 1) {
                    int num2 = (int) ((particles[i].TimeOffset * 2f + animTimer) % 2f);
                    mtexture = particleTextures[1 + num2];
                } else {
                    mtexture = particleTextures[2];
                }
                if (drawPos.X >= X + 2f && drawPos.Y >= Y + 2f && drawPos.X < Right - 2f && drawPos.Y < Bottom - 2f) {
                    mtexture.DrawCentered(drawPos + shake, particles[i].Color * Alpha);
                }
            }
        }

        // is custom, edited a few things.
        public void WobbleLine(Vector2 from, Vector2 to, float offset, bool line, bool back) {
            float length = (to - from).Length();
            Vector2 vector = Vector2.Normalize(to - from);
            Vector2 vector2 = new Vector2(vector.Y, -vector.X);
            Color lineColor = PlayerHasDreamDash ? CustomDreamBlock.ActiveLineColor : CustomDreamBlock.DisabledLineColor;
            Color backColor = PlayerHasDreamDash ? CustomDreamBlock.ActiveBackColor : CustomDreamBlock.DisabledBackColor;
            if (whiteFill > 0f) {
                lineColor = Color.Lerp(lineColor, Color.White, whiteFill);
                backColor = Color.Lerp(backColor, Color.White, whiteFill);
            }
            float scaleFactor = 0f;
            int interval = 8;
            for (int i = 0; i < length; i += interval) {
                float lerp = MathHelper.Lerp(LineAmplitude(wobbleFrom + offset, i), LineAmplitude(wobbleTo + offset, i), wobbleEase);
                if (i + interval >= length) {
                    lerp = 0f;
                }
                float num5 = Math.Min(interval, length - i);
                Vector2 vector3 = from + vector * i + vector2 * scaleFactor;
                Vector2 vector4 = from + vector * (i + num5) + vector2 * lerp;
                if (back) {
                    Draw.Line(vector3 - vector2, vector4 - vector2, backColor);
                    Draw.Line(vector3 - vector2 * 2f, vector4 - vector2 * 2f, backColor);
                    Draw.Line(vector3 - vector2 * 8f, vector4 - vector2 * 8f, backColor * 0.95f);
                    Draw.Line(vector3 - vector2 * 9f, vector4 - vector2 * 9f, backColor * 0.7f);
                    Draw.Line(vector3 - vector2 * 10f, vector4 - vector2 * 10f, backColor * 0.4f);
                    Draw.Line(vector3 - vector2 * 11f, vector4 - vector2 * 11f, backColor * 0.2f);
                }
                if(line)
                    Draw.Line(vector3, vector4, lineColor);
                scaleFactor = lerp;
            }
        }

        private float LineAmplitude(float seed, float index) {
            return (float) (Math.Sin(seed + index / 16f + Math.Sin(seed * 2f + index / 32f) * Calc.Circle) + 1.0) * 1.5f;
        }

        public void Setup() {
            particles = new DreamParticle[(int) ((Width / 4f) * (Height / 4f) * 0.5f)];
            for (int i = 0; i < particles.Length; i++) {
                particles[i].Position = new Vector2(Calc.Random.NextFloat(Width), Calc.Random.NextFloat(Height));
                particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[i].TimeOffset = Calc.Random.NextFloat();
                particles[i].Color = Color.LightGray * (0.5f + particles[i].Layer / 2f * 0.5f);
                if (PlayerHasDreamDash) {
                    switch (particles[i].Layer) {
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

        public IEnumerator Activate() {
            Level level = SceneAs<Level>();
            yield return 1f;
            Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
            Add(shaker = new Shaker(true, delegate (Vector2 t) {
                shake = t;
            }));
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime) {
                whiteFill = Ease.CubeIn(p);
                yield return null;
            }
            shaker.On = false;
            yield return 0.5f;
            ActivateNoRoutine();
            whiteHeight = 1f;
            whiteFill = 1f;
            for (float p = 1f; p > 0f; p -= Engine.DeltaTime * 0.5f) {
                whiteHeight = p;
                if (level.OnInterval(0.1f)) {
                    int num = 0;
                    while (num < Width) {
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, new Vector2(X + num, Y + Height * whiteHeight + 1f));
                        num += 4;
                    }
                }
                if (level.OnInterval(0.1f)) {
                    level.Shake(0.3f);
                }
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                yield return null;
            }
            while (whiteFill > 0f) {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
            yield break;
        }

        public void ActivateNoRoutine() {
            if (!PlayerHasDreamDash) {
                PlayerHasDreamDash = true;
                Setup();
                Remove(occlude);
                whiteHeight = 0f;
                whiteFill = 0f;
                if (shaker != null) {
                    shaker.On = false;
                }
            }
        }

        public void DeactivateNoRoutine() {
            if (PlayerHasDreamDash) {
                PlayerHasDreamDash = false;
                Setup();
                if (occlude == null) {
                    occlude = new LightOcclude(1f);
                }
                Add(occlude);
                whiteHeight = 1f;
                whiteFill = 0f;
                if (shaker != null) {
                    shaker.On = false;
                }
                surfaceSoundIndex = SurfaceIndex.DreamBlockInactive;
            }
        }

        public IEnumerator Deactivate() {
            Level level = SceneAs<Level>();
            yield return 1f;
            Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
            if (shaker == null) {
                shaker = new Shaker(true, delegate (Vector2 t) {
                    shake = t;
                });
            }
            Add(shaker);
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float alpha = 0f; alpha < 1f; alpha += Engine.DeltaTime) {
                whiteFill = Ease.CubeIn(alpha);
                yield return null;
            }
            shaker.On = false;
            yield return 0.5f;
            DeactivateNoRoutine();
            whiteHeight = 1f;
            whiteFill = 1f;
            for (float alpha = 1f; alpha > 0f; alpha -= Engine.DeltaTime * 0.5f) {
                whiteHeight = alpha;
                if (level.OnInterval(0.1f)) {
                    int num = 0;
                    while (num < Width) {
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, new Vector2(X + num, Y + Height * whiteHeight + 1f));
                        num += 4;
                    }
                }
                if (level.OnInterval(0.1f)) {
                    level.Shake(0.3f);
                }
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                yield return null;
            }
            while (whiteFill > 0f) {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
            yield break;
        }

        public IEnumerator FastDeactivate() {
            Level level = SceneAs<Level>();
            yield return null;
            Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
            if (shaker == null) {
                shaker = new Shaker(true, delegate (Vector2 t) {
                    shake = t;
                });
            }
            Add(shaker);
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float alpha = 0f; alpha < 1f; alpha += Engine.DeltaTime * 3f) {
                whiteFill = Ease.CubeIn(alpha);
                yield return null;
            }
            shaker.On = false;
            yield return 0.1f;
            DeactivateNoRoutine();
            whiteHeight = 1f;
            whiteFill = 1f;
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Width, TopCenter, Vector2.UnitX * Width / 2f, Color.White, 3.14159274f);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Width, BottomCenter, Vector2.UnitX * Width / 2f, Color.White, 0f);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Height, CenterLeft, Vector2.UnitY * Height / 2f, Color.White, 4.712389f);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Height, CenterRight, Vector2.UnitY * Height / 2f, Color.White, 1.57079637f);
            level.Shake(0.3f);
            yield return 0.1f;
            while (whiteFill > 0f) {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
            yield break;
        }

        public IEnumerator FastActivate() {
            Level level = SceneAs<Level>();
            yield return null;
            Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
            if (shaker == null) {
                shaker = new Shaker(true, delegate (Vector2 t) {
                    shake = t;
                });
            }
            Add(shaker);
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float alpha = 0f; alpha < 1f; alpha += Engine.DeltaTime * 3f) {
                whiteFill = Ease.CubeIn(alpha);
                yield return null;
            }
            shaker.On = false;
            yield return 0.1f;
            ActivateNoRoutine();
            whiteHeight = 1f;
            whiteFill = 1f;
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Width, TopCenter, Vector2.UnitX * Width / 2f, Color.White, 3.14159274f);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Width, BottomCenter, Vector2.UnitX * Width / 2f, Color.White, 0f);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Height, CenterLeft, Vector2.UnitY * Height / 2f, Color.White, 4.712389f);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Height, CenterRight, Vector2.UnitY * Height / 2f, Color.White, 1.57079637f);
            level.Shake(0.3f);
            yield return 0.1f;
            while (whiteFill > 0f) {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
            yield break;
        }

        #endregion

        #region Hooks

        private static List<IDetour> hook_Platform_GetLandOrStepSoundIndex = new List<IDetour>();
        private static List<IDetour> hook_Platform_GetWallSoundIndex = new List<IDetour>();
        private static IDetour hook_Player_DashCoroutine;
        private static IDetour hook_Player_orig_WallJump;

        internal static void LoadDelayed() {
            // Land and Step sound are identical
            MethodInfo Platform_GetLandOrStepSoundIndex = typeof(DreamTunnelEntry).GetMethod("Platform_GetLandOrStepSoundIndex", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (MethodInfo method in typeof(Platform).GetMethod("GetLandSoundIndex").GetOverrides(true)) {
                Logger.Log(LogLevel.Info, "Communal Helper", $"Hooking {method.DeclaringType}.{method.Name} to override when DreamTunnelEntry present.");
                hook_Platform_GetLandOrStepSoundIndex.Add(
                    new Hook(method, Platform_GetLandOrStepSoundIndex)
                );
            }
            foreach (MethodInfo method in typeof(Platform).GetMethod("GetStepSoundIndex").GetOverrides(true)) {
                Logger.Log(LogLevel.Info, "Communal Helper", $"Hooking {method.DeclaringType}.{method.Name} to override when DreamTunnelEntry present.");
                hook_Platform_GetLandOrStepSoundIndex.Add(
                    new Hook(method, Platform_GetLandOrStepSoundIndex)
                );
            }
            MethodInfo Platform_GetWallSoundIndex = typeof(DreamTunnelEntry).GetMethod("Platform_GetWallSoundIndex", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (MethodInfo method in typeof(Platform).GetMethod("GetWallSoundIndex").GetOverrides(true)) {
                Logger.Log(LogLevel.Info, "Communal Helper", $"Hooking {method.DeclaringType}.{method.Name} to override when DreamTunnelEntry present.");
                hook_Platform_GetWallSoundIndex.Add(
                    new Hook(method, Platform_GetWallSoundIndex)
                );
            }

            hook_Player_DashCoroutine = new ILHook(
                typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
                Player_DashCoroutine);

            // Footstep Ripples
            IL.Celeste.Player.ClimbBegin += Player_ClimbBegin;
            IL.Celeste.Player.OnCollideV += Player_OnCollideV;
            hook_Player_orig_WallJump = new ILHook(
                typeof(Player).GetMethod("orig_WallJump", BindingFlags.NonPublic | BindingFlags.Instance),
                Player_orig_WallJump);

            On.Celeste.Solid.Awake += Solid_Awake;

            On.Celeste.DashBlock.Break_Vector2_Vector2_bool_bool += DashBlock_Break_Vector2_Vector2_bool_bool;

            // Add Renderer
            On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
        }

        internal static void Unload() {
            hook_Platform_GetLandOrStepSoundIndex.ForEach(h => h.Dispose());
            hook_Platform_GetWallSoundIndex.ForEach(h => h.Dispose());

            hook_Player_DashCoroutine.Dispose();

            IL.Celeste.Player.ClimbBegin -= Player_ClimbBegin;
            IL.Celeste.Player.OnCollideV -= Player_OnCollideV;
            hook_Player_orig_WallJump.Dispose();

            On.Celeste.Solid.Awake -= Solid_Awake;

            On.Celeste.DashBlock.Break_Vector2_Vector2_bool_bool -= DashBlock_Break_Vector2_Vector2_bool_bool;

            On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
        }

        private static int Platform_GetLandOrStepSoundIndex(Func<Platform, Entity, int> orig, Platform self, Entity entity) {
            foreach (StaticMover sm in new DynData<Platform>(self).Get<List<StaticMover>>("staticMovers")) {
                if (sm.Entity is DreamTunnelEntry entry && entry.Orientation == Spikes.Directions.Up && entity.CollideCheck(entry, entity.Position + Vector2.UnitY)) { 
                    return entry.surfaceSoundIndex;
                }
            }
            return orig(self, entity);
        }

        private static int Platform_GetWallSoundIndex(Func<Platform, Player, int, int> orig, Platform self, Player player, int side) {
            foreach (StaticMover sm in new DynData<Platform>(self).Get<List<StaticMover>>("staticMovers")) {
                if (sm.Entity is DreamTunnelEntry entry) {
                    if (side == (int) Facings.Left && entry.Orientation == Spikes.Directions.Right && player.CollideCheck(entry, player.Position - Vector2.UnitX)) {
                            return entry.surfaceSoundIndex;
                    } 
                    if (side == (int) Facings.Right && entry.Orientation == Spikes.Directions.Left && player.CollideCheck(entry, player.Position + Vector2.UnitX)) { 
                            return entry.surfaceSoundIndex;
                    }
                }
            }
            return orig(self, player, side);
        }

        /// <summary>
        /// Handle down-diagonal dashing when standing on DreamTunnelEntry
        /// </summary>
        private static void Player_DashCoroutine(ILContext il) {
            /*
             * adds a check for !player.CollideCheck<DreamTunnelEntry>(player.Position + Vector2.UnitY) to
             * if (player.onGround && player.DashDir.X != 0f && player.DashDir.Y > 0f && player.Speed.Y > 0f && 
             *  (!player.Inventory.DreamDash || !player.CollideCheck<DreamBlock>(player.Position + Vector2.UnitY)))
             */
            ILCursor cursor = new ILCursor(il);
            // oof
            cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Callvirt && 
                ((MethodReference) instr.Operand).FullName == "System.Boolean Monocle.Entity::CollideCheck<Celeste.DreamBlock>(Microsoft.Xna.Framework.Vector2)");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(Player).GetNestedType("<DashCoroutine>d__423", BindingFlags.NonPublic).GetField("<>4__this"));
            cursor.EmitDelegate<Func<bool, Player, bool>>((v, player) => {
                return v || player.CollideCheck<DreamTunnelEntry>(player.Position + Vector2.UnitY);
            });
        }

        #region FootstepRipples

        private static void Player_ClimbBegin(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.MatchIsinst<DreamBlock>());
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Platform, Player, Platform>>((platform, player) => {
                foreach (StaticMover sm in new DynData<Platform>(platform).Get<List<StaticMover>>("staticMovers")) {
                    Vector2 origin = player.Position + new Vector2((float) player.Facing * 3, -4f);
                    if (sm.Entity is DreamTunnelEntry entry 
                        && (entry.Orientation == Spikes.Directions.Left || entry.Orientation == Spikes.Directions.Right) 
                        && entry.CollidePoint(origin + Vector2.UnitX * (float) player.Facing)) {
                        entry.FootstepRipple(origin);
                    }
                }
                return platform;
            });
        }

        private static void Player_OnCollideV(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.MatchIsinst<DreamBlock>());
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Platform, Player, Platform>>((platform, player) => {
                foreach (StaticMover sm in new DynData<Platform>(platform).Get<List<StaticMover>>("staticMovers")) {
                    if (sm.Entity is DreamTunnelEntry entry && entry.Orientation == Spikes.Directions.Up 
                        && player.CollideCheck(entry, player.Position + Vector2.UnitY)) {
                        entry.FootstepRipple(player.Position);
                    }
                }
                return platform;
            });
        }

        private static void Player_orig_WallJump(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.MatchIsinst<DreamBlock>());
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Platform, Player, int, Platform>>((platform, player, dir) => {
                foreach (StaticMover sm in new DynData<Platform>(platform).Get<List<StaticMover>>("staticMovers")) {
                    if (sm.Entity is DreamTunnelEntry entry
                        && (entry.Orientation == Spikes.Directions.Left || entry.Orientation == Spikes.Directions.Right)
                        && entry.CollidePoint(player.Position - Vector2.UnitX * dir * 4f)) {
                        entry.FootstepRipple(player.Position + new Vector2(0, -4f));
                    }
                }
                return platform;
            });
        }

        #endregion

        private static void Solid_Awake(On.Celeste.Solid.orig_Awake orig, Solid self, Scene scene) {
            orig(self, scene);

            if (!self.AllowStaticMovers)
                ForceAttachStaticMovers(self, scene);
        }

        private static void ForceAttachStaticMovers(Solid solid, Scene scene) {
            bool collidable = solid.Collidable;
            solid.Collidable = true;
            DynData<Solid> solidData = null;
            foreach (Component component in scene.Tracker.GetComponents<StaticMover>()) {
                StaticMover staticMover = (StaticMover) component;
                if (staticMover.Entity is DreamTunnelEntry entry && entry.overrideAllowStaticMovers && staticMover.IsRiding(solid) && staticMover.Platform == null) {
                    solidData ??= new DynData<Solid>(solid);
                    solidData.Get<List<StaticMover>>("staticMovers").Add(staticMover);
                    staticMover.Platform = solid;
                    staticMover.OnAttach?.Invoke(solid);
                }
            }
            solid.Collidable = collidable;
        }

        private static FieldInfo f_Platform_staticMovers = typeof(Platform).GetField("staticMovers", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void DashBlock_Break_Vector2_Vector2_bool_bool(On.Celeste.DashBlock.orig_Break_Vector2_Vector2_bool_bool orig, DashBlock self, Vector2 from, Vector2 direction, bool playSound, bool playDebrisSound) {
            orig(self, from, direction, playSound, playDebrisSound);
            List<StaticMover> staticMovers = (List<StaticMover>) f_Platform_staticMovers.GetValue(self);
            foreach (StaticMover mover in staticMovers) {
                if (mover.Entity is DreamTunnelEntry)
                    mover.OnDestroy();
            }
        }

        private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {
            orig(self);
            self.Level.Add(new DreamTunnelEntryRenderer());
        }

        #endregion

    }
}
