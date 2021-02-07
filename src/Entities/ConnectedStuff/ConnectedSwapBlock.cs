using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper {

    [CustomEntity("CommunalHelper/ConnectedSwapBlock")]
    [Tracked(false)]
    class ConnectedSwapBlock : ConnectedSolid {

        private class PathRenderer : Entity {

            private ConnectedSwapBlock block;
            private float timer;

            public PathRenderer(ConnectedSwapBlock block)
                : base(block.Position) {
                this.block = block;
                Depth = 8999;
                timer = Calc.Random.NextFloat();
            }

            public override void Update() {
                base.Update();
                timer += Engine.DeltaTime * 4f;
            }

            public override void Render() {
                float scale = 0.5f * (0.5f + ((float) Math.Sin(timer) + 1f) * 0.25f);
                DrawBlockStyle(new Vector2(block.moveRect.X, block.moveRect.Y), block.moveRect.Width, block.moveRect.Height, block.nineSliceTarget, Color.White * scale);
            }
            private void DrawBlockStyle(Vector2 pos, float width, float height, MTexture[,] ninSlice, Color color) {
                int num = (int) (width / 8f);
                int num2 = (int) (height / 8f);
                ninSlice[0, 0].Draw(pos + new Vector2(0f, 0f), Vector2.Zero, color);
                ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0f), Vector2.Zero, color);
                ninSlice[0, 2].Draw(pos + new Vector2(0f, height - 8f), Vector2.Zero, color);
                ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
                for (int i = 1; i < num - 1; i++) {
                    ninSlice[1, 0].Draw(pos + new Vector2(i * 8, 0f), Vector2.Zero, color);
                    ninSlice[1, 2].Draw(pos + new Vector2(i * 8, height - 8f), Vector2.Zero, color);
                }
                for (int j = 1; j < num2 - 1; j++) {
                    ninSlice[0, 1].Draw(pos + new Vector2(0f, j * 8), Vector2.Zero, color);
                    ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, j * 8), Vector2.Zero, color);
                }
                for (int k = 1; k < num - 1; k++) {
                    for (int l = 1; l < num2 - 1; l++) {
                        ninSlice[1, 1].Draw(pos + new Vector2(k, l) * 8f, Vector2.Zero, color);
                    }
                }
            }
        }

        public Vector2 Direction;
        public bool Swapping;
        public SwapBlock.Themes Theme;

        private static MTexture[,]
            GreenEdgeTiles, GreenInnerCornerTiles,
            RedEdgeTiles, RedInnerCornerTiles,
            MoonGreenEdgeTiles, MoonGreenInnerCornerTiles,
            MoonRedEdgeTiles, MoonRedInnerCornerTiles,
            TargetTiles, MoonTargetTiles;

        private MTexture[,]
            customGreenEdgeTiles, customGreenInnerCornerTiles,
            customRedEdgeTiles, customRedInnerCornerTiles;
        private bool customRedTextures = false, customGreenTextures = false;

        private Vector2 start, end, offset;
        private float lerp;
        private int target;
        private Rectangle moveRect;

        private float speed;
        private float maxForwardSpeed;
        private float maxBackwardSpeed;
        private float returnTimer;
        private float redAlpha = 1f;

        private MTexture[,] nineSliceTarget = new MTexture[3, 3];

        private List<Image> greenTiles, redTiles;

        private Sprite middleGreen;
        private Sprite middleRed;

        private EventInstance moveSfx;
        private EventInstance returnSfx;

        private DisplacementRenderer.Burst burst;

        private float particlesRemainder;

        public ConnectedSwapBlock(Vector2 position, int width, int height, Vector2 node, SwapBlock.Themes theme, string greenCustomBlockPath, string redCustomBlockPath)
            : base(position, width, height, safe: false) {
            Theme = theme;
            start = Position;
            end = node;
            offset = node - start;
            maxForwardSpeed = 360f / Vector2.Distance(start, end);
            maxBackwardSpeed = maxForwardSpeed * 0.4f;
            Direction.X = Math.Sign(end.X - start.X);
            Direction.Y = Math.Sign(end.Y - start.Y);
            Add(new DashListener {
                OnDash = OnDash
            });

            MTexture mTexture;
            if (Theme == SwapBlock.Themes.Moon) {
                mTexture = GFX.Game["objects/swapblock/moon/target"];
            } else {
                mTexture = GFX.Game["objects/swapblock/target"];
            }

            if (redCustomBlockPath != "") {
                Tuple<MTexture[,], MTexture[,]> customRedTiles = SetupCustomTileset(redCustomBlockPath);
                customRedEdgeTiles = customRedTiles.Item1;
                customRedInnerCornerTiles = customRedTiles.Item2;
                customRedTextures = true;
            }
            if (greenCustomBlockPath != "") {
                Tuple<MTexture[,], MTexture[,]> customGreenTiles = SetupCustomTileset(greenCustomBlockPath);
                customGreenEdgeTiles = customGreenTiles.Item1;
                customGreenInnerCornerTiles = customGreenTiles.Item2;
                customGreenTextures = true;
            }

            if (Theme == SwapBlock.Themes.Normal) {
                middleGreen = GFX.SpriteBank.Create("swapBlockLight");
                middleRed = GFX.SpriteBank.Create("swapBlockLightRed");
                nineSliceTarget = TargetTiles;
            } else if (Theme == SwapBlock.Themes.Moon) {
                middleGreen = GFX.SpriteBank.Create("swapBlockLightMoon");
                middleRed = GFX.SpriteBank.Create("swapBlockLightRedMoon");
                nineSliceTarget = MoonTargetTiles;
            }

            middleRed.Position = middleGreen.Position = new Vector2(width, height) / 2f;

            Add(new LightOcclude(0.2f));
            Depth = -9999;
        }

        public ConnectedSwapBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Enum("theme", SwapBlock.Themes.Normal),
                  data.Attr("customGreenBlockTexture").Trim(), data.Attr("customRedBlockTexture").Trim()) {
        }

        public override void Awake(Scene scene) {
            /* 
             * Yes, I'm doing this before base.Awake, because ConnectedSolid will 
             * change the original width and height of this entity, because 
             * of how the game handles multiple colliders, and adds them.
             */
            scene.Add(new PathRenderer(this));

            base.Awake(scene);
            if (Theme == SwapBlock.Themes.Normal) {
                greenTiles = AutoTile(customGreenTextures ? customGreenEdgeTiles : GreenEdgeTiles, customGreenTextures ? customGreenInnerCornerTiles : GreenInnerCornerTiles, false, false);
                redTiles = AutoTile(customRedTextures ? customRedEdgeTiles : RedEdgeTiles, customRedTextures ? customRedInnerCornerTiles : RedInnerCornerTiles, false, false);
            } else {
                greenTiles = AutoTile(customGreenTextures ? customGreenEdgeTiles : MoonGreenEdgeTiles, customGreenTextures ? customGreenInnerCornerTiles : MoonGreenInnerCornerTiles, false, false);
                redTiles = AutoTile(customRedTextures ? customRedEdgeTiles : MoonRedEdgeTiles, customRedTextures ? customRedInnerCornerTiles : MoonRedInnerCornerTiles, false, false);
            }

            Add(middleRed);
            Add(middleGreen);

            // Making the track rectangle always contain the connected swap block, entirely.
            int x1 = (int) MathHelper.Min(GroupBoundsMin.X, offset.X + GroupBoundsMin.X);
            int y1 = (int) MathHelper.Min(GroupBoundsMin.Y, offset.Y + GroupBoundsMin.Y);
            int x2 = (int) MathHelper.Max(GroupBoundsMax.X, offset.X + GroupBoundsMax.X);
            int y2 = (int) MathHelper.Max(GroupBoundsMax.Y, offset.Y + GroupBoundsMax.Y);
            moveRect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            Audio.Stop(moveSfx);
            Audio.Stop(returnSfx);
        }

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            Audio.Stop(moveSfx);
            Audio.Stop(returnSfx);
        }

        private void OnDash(Vector2 direction) {
            Swapping = (lerp < 1f);
            target = 1;
            returnTimer = 0.8f;
            burst = (Scene as Level).Displacement.AddBurst(MasterCenter, 0.2f, 0f, 16f);
            if (lerp >= 0.2f) {
                speed = maxForwardSpeed;
            } else {
                speed = MathHelper.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
            }
            Audio.Stop(returnSfx);
            Audio.Stop(moveSfx);
            if (!Swapping) {
                Audio.Play(SFX.game_05_swapblock_move_end, MasterCenter);
            } else {
                moveSfx = Audio.Play(SFX.game_05_swapblock_move, MasterCenter);
            }
        }

        public override void Update() {
            base.Update();
            if (returnTimer > 0f) {
                returnTimer -= Engine.DeltaTime;
                if (returnTimer <= 0f) {
                    target = 0;
                    speed = 0f;
                    returnSfx = Audio.Play(SFX.game_05_swapblock_return, MasterCenter);
                }
            }
            if (burst != null) {
                burst.Position = MasterCenter;
            }
            redAlpha = Calc.Approach(redAlpha, (target != 1) ? 1 : 0, Engine.DeltaTime * 32f);
            if (target == 0 && lerp == 0f) {
                middleRed.SetAnimationFrame(0);
                middleGreen.SetAnimationFrame(0);
            }
            if (target == 1) {
                speed = Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);
            } else {
                speed = Calc.Approach(speed, maxBackwardSpeed, maxBackwardSpeed / 1.5f * Engine.DeltaTime);
            }
            float num = lerp;
            lerp = Calc.Approach(lerp, target, speed * Engine.DeltaTime);
            if (lerp != num) {
                Vector2 liftSpeed = (end - start) * speed;
                Vector2 position = Position;
                if (target == 1) {
                    liftSpeed = (end - start) * maxForwardSpeed;
                }
                if (lerp < num) {
                    liftSpeed *= -1f;
                }
                if (target == 1 && Scene.OnInterval(0.02f)) {
                    MoveParticles(end - start);
                }
                MoveTo(Vector2.Lerp(start, end, lerp), liftSpeed);
                if (position != Position) {
                    Audio.Position(moveSfx, Center);
                    Audio.Position(returnSfx, Center);
                    if (Position == start && target == 0) {
                        Audio.SetParameter(returnSfx, "end", 1f);
                        Audio.Play(SFX.game_05_swapblock_return_end, Center);
                    } else if (Position == end && target == 1) {
                        Audio.Play(SFX.game_05_swapblock_move_end, Center);
                    }
                }
            }
            if (Swapping && lerp >= 1f) {
                Swapping = false;
            }
            StopPlayerRunIntoAnimation = lerp is <= 0f or >= 1f;
        }

        public override void Render() {
            Vector2 vector = Position + Shake;
            if (lerp != target && speed > 0f) {
                Vector2 value = (end - start).SafeNormalize();
                if (target == 1) {
                    value *= -1f;
                }
                float num = speed / maxForwardSpeed;
                float num2 = 16f * num;
                for (int i = 2; i < num2; i += 2) {
                    DrawBlock(vector + value * i, greenTiles, middleGreen, Color.White * (1f - i / num2));
                }
            }
            if (redAlpha < 1f) {
                DrawBlock(vector, greenTiles, middleGreen, Color.White);
            }
            if (redAlpha > 0f) {
                DrawBlock(vector, redTiles, middleRed, Color.White * redAlpha);
            }
        }

        private void MoveParticles(Vector2 normal) {
            foreach (Hitbox hitbox in Colliders) {
                Vector2 position;
                Vector2 positionRange;
                float direction;
                float num;
                if (normal.X > 0f) {
                    position = hitbox.CenterLeft;
                    positionRange = Vector2.UnitY * (hitbox.Height - 6f);
                    direction = (float) Math.PI;
                    num = Math.Max(2f, hitbox.Height / 14f);
                } else if (normal.X < 0f) {
                    position = hitbox.CenterRight;
                    positionRange = Vector2.UnitY * (hitbox.Height - 6f);
                    direction = 0f;
                    num = Math.Max(2f, hitbox.Height / 14f);
                } else if (normal.Y > 0f) {
                    position = hitbox.TopCenter;
                    positionRange = Vector2.UnitX * (hitbox.Width - 6f);
                    direction = -(float) Math.PI / 2f;
                    num = Math.Max(2f, hitbox.Width / 14f);
                } else {
                    position = hitbox.BottomCenter;
                    positionRange = Vector2.UnitX * (hitbox.Width - 6f);
                    direction = (float) Math.PI / 2f;
                    num = Math.Max(2f, hitbox.Width / 14f);
                }
                particlesRemainder += num;
                int num2 = (int) particlesRemainder;
                particlesRemainder -= num2;
                positionRange *= 0.5f;
                SceneAs<Level>().Particles.Emit(SwapBlock.P_Move, num2, position + Position, positionRange, direction);
            }
        }

        private void DrawBlock(Vector2 pos, List<Image> ninSlice, Sprite middle, Color color) {
            foreach (Image tile in ninSlice) {
                tile.RenderPosition += pos;
                tile.Color = color;
                tile.Render();
                tile.RenderPosition -= pos;
            }
            if (middle != null) {
                middle.Color = color;
                middle.Render();
            }
        }

        public static void InitializeTextures() {
            // normal theme
            GreenEdgeTiles = new MTexture[3, 3];
            MTexture greenEdges = GFX.Game["objects/swapblock/block"];
            RedEdgeTiles = new MTexture[3, 3];
            MTexture redEdges = GFX.Game["objects/swapblock/blockRed"];
            GreenInnerCornerTiles = new MTexture[2, 2];
            MTexture greenInnerCorners = GFX.Game["objects/CommunalHelper/connectedSwapBlock/innerCornersGreen"];
            RedInnerCornerTiles = new MTexture[2, 2];
            MTexture redInnerCorners = GFX.Game["objects/CommunalHelper/connectedSwapBlock/innerCornersRed"];
            TargetTiles = new MTexture[3, 3];
            MTexture targetTiles = GFX.Game["objects/swapblock/target"];

            // moon theme
            MoonGreenEdgeTiles = new MTexture[3, 3];
            MTexture moonGreenEdges = GFX.Game["objects/swapblock/moon/block"];
            MoonRedEdgeTiles = new MTexture[3, 3];
            MTexture moonRedEdges = GFX.Game["objects/swapblock/moon/blockRed"];
            MoonGreenInnerCornerTiles = new MTexture[2, 2];
            MTexture moonGreenInnerCorners = GFX.Game["objects/CommunalHelper/connectedSwapBlock/moon/innerCornersGreen"];
            MoonRedInnerCornerTiles = new MTexture[2, 2];
            MTexture moonRedInnerCorners = GFX.Game["objects/CommunalHelper/connectedSwapBlock/moon/innerCornersRed"];
            MoonTargetTiles = new MTexture[3, 3];
            MTexture moonTargetTiles = GFX.Game["objects/swapblock/moon/target"];

            // edges
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    int x = i * 8, y = j * 8;
                    GreenEdgeTiles[i, j] = greenEdges.GetSubtexture(x, y, 8, 8);
                    RedEdgeTiles[i, j] = redEdges.GetSubtexture(x, y, 8, 8);
                    MoonGreenEdgeTiles[i, j] = moonGreenEdges.GetSubtexture(x, y, 8, 8);
                    MoonRedEdgeTiles[i, j] = moonRedEdges.GetSubtexture(x, y, 8, 8);

                    TargetTiles[i, j] = targetTiles.GetSubtexture(x, y, 8, 8);
                    MoonTargetTiles[i, j] = moonTargetTiles.GetSubtexture(x, y, 8, 8);
                }
            }

            // inner corners
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < 2; j++) {
                    int x = i * 8, y = j * 8;
                    GreenInnerCornerTiles[i, j] = greenInnerCorners.GetSubtexture(x, y, 8, 8);
                    RedInnerCornerTiles[i, j] = redInnerCorners.GetSubtexture(x, y, 8, 8);
                    MoonGreenInnerCornerTiles[i, j] = moonGreenInnerCorners.GetSubtexture(x, y, 8, 8);
                    MoonRedInnerCornerTiles[i, j] = moonRedInnerCorners.GetSubtexture(x, y, 8, 8);
                }
            }
        }
    }

    public class ConnectedSwapBlockHooks {

        private static MethodInfo Player_DashCoroutine = typeof(Player).GetMethod("DashCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo DashCoroutine_Hook_F_This /*, DashCoroutine_Hook_F_SwapCancel */ ;

        private static ILHook Player_DashCoroutine_Hook;

        public static void Hook() {

            // The "this" field defined in the compiler-generated type
            DashCoroutine_Hook_F_This = Player_DashCoroutine.GetStateMachineTarget().DeclaringType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);

            // What would usually be a local variable, but is instead stored in the compiler-generated type
            //DashCoroutine_Hook_F_SwapCancel = Player_DashCoroutine.GetStateMachineTarget().DeclaringType.GetField("<swapCancel>5__2", BindingFlags.NonPublic | BindingFlags.Instance);

            Player_DashCoroutine_Hook = new ILHook(Player_DashCoroutine.GetStateMachineTarget(), DashCoroutineILHook);
        }
        public static void Unhook() {
            Player_DashCoroutine_Hook.Dispose();
        }

        private static void DashCoroutineILHook(ILContext il) {
            // Used to emit new instructions into the method
            ILCursor cursor = new ILCursor(il);

            // There's only one Input.Grab check in the method, so go there, then to the next Brfalse_S opcode (right before swapcheck block)
            cursor.GotoNext(instr => instr.MatchLdsfld("Celeste.Input", "Grab") || instr.MatchCall("Celeste.Input", "get_GrabCheck"));
            cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Brfalse_S);

            // Load the actual "this" (the instance of the coroutine)
            cursor.Emit(OpCodes.Ldarg_0);

            // And then load the Player object
            cursor.Emit(OpCodes.Ldfld, DashCoroutine_Hook_F_This);

            // Emit a call to a function that takes in the player object, and returns a boolean
            cursor.EmitDelegate<Func<Player, bool>>(Player_ClimbConnectedSwapBlockCheck);

            // If the returned value is false, skip the return we're about to emit, and continue on with the rest of the method
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);

            // Perform the equivalent of "yield break"
            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Ret);

            // Next bit to modify
            cursor.GotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Stfld && ((FieldReference) instr.Operand).Name.Contains("swapCancel"));

            // Load the actual "this" (the instance of the coroutine)
            cursor.Emit(OpCodes.Ldarg_0);

            // And then load the Player object
            cursor.Emit(OpCodes.Ldfld, DashCoroutine_Hook_F_This);

            // Emit a call to a function that takes in the Vector2 and Player objects, and returns a Vector2
            // The Vector2 is already loaded by the previous instructions and stored by the following ones
            cursor.Emit(OpCodes.Call, typeof(ConnectedSwapBlockHooks).GetMethod("Player_CancelDashAgainstConnectedSwapBlock", BindingFlags.NonPublic | BindingFlags.Static));
        }

        /*
         * Appears to set the player to its climbing state (player.StateMachine.State = 1),
         * therefore cancelling the dash, and resetting the speed back to Vector.Zero.
         * Does that if the Connected Swap Blocks and the player are moving in the same direction (?)
         * (swapBlock.Direction.X == Math.Sign(player.DashDir.X))
         */
        private static bool Player_ClimbConnectedSwapBlockCheck(Player player) {
            ConnectedSwapBlock swapBlock = player.CollideFirst<ConnectedSwapBlock>(player.Position + Vector2.UnitX * Math.Sign(player.DashDir.X));
            if (swapBlock != null && swapBlock.Direction.X == Math.Sign(player.DashDir.X)) {
                player.StateMachine.State = 1;
                player.Speed = Vector2.Zero;
                return true;
            }
            return false;
        }

        /*
         * Looks like this cancels the player's dash direction, or somewhat change its direction.
         * So in theory you wouldn't be able to dialogonal down dash on top of a Connected Swap Block,
         * therefore sticking to it, and getting able to jump off of it, with the lift speed & stuff.
         * That's probably done so that it is a little easier for the player to interact with those.
         */
        private static Vector2 Player_CancelDashAgainstConnectedSwapBlock(Vector2 swapCancel, Player player) {
            foreach (ConnectedSwapBlock swapBlock2 in player.Scene.Tracker.GetEntities<ConnectedSwapBlock>()) {

                if (player.CollideCheck(swapBlock2, player.Position + Vector2.UnitY) && swapBlock2 != null && swapBlock2.Swapping) {
                    if (player.DashDir.X != 0f && swapBlock2.Direction.X == Math.Sign(player.DashDir.X)) {
                        player.Speed.X = (swapCancel.X = 0f);
                    }
                    if (player.DashDir.Y != 0f && swapBlock2.Direction.Y == Math.Sign(player.DashDir.Y)) {
                        player.Speed.Y = (swapCancel.Y = 0f);
                    }
                }
            }
            return swapCancel;
        }
    }
}
