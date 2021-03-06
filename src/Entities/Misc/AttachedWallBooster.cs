﻿using Celeste.Mod.Entities;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/AttachedWallBooster")]
    [Tracked(false)]
    public class AttachedWallBooster : WallBooster {
        public Vector2 Shake = Vector2.Zero;

        private DynData<WallBooster> baseData;

        public AttachedWallBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Height, data.Bool("left"), data.Bool("notCoreMode")) {
            baseData = new DynData<WallBooster>(this);

            Remove(Get<StaticMover>());
            Add(new StaticMover {
                OnShake = OnShake,
                SolidChecker = IsRiding,
                OnEnable = OnEnable,
                OnDisable = OnDisable
            });
        }

        public void SetColor(Color color) {
            foreach (Image img in baseData.Get<List<Sprite>>("tiles")) {
                img.Color = color;
            }
        }

        private void OnDisable() {
            SetColor(Color.Gray);
            Collidable = false;
        }

        private void OnEnable() {
            SetColor(Color.White);
            Collidable = true;
        }

        private bool IsRiding(Solid solid) {
            return Facing switch {
                Facings.Right => CollideCheckOutside(solid, Position + Vector2.UnitX),
                Facings.Left => CollideCheckOutside(solid, Position - Vector2.UnitX),
                _ => false,
            };
        }

        private void OnShake(Vector2 amount) {
            Shake += amount;
        }

        public override void Render() {
            Vector2 p = Position;
            Position += Shake;
            base.Render();
            Position = p;
        }

        #region Hooks

        private const float WallBoosterSpeed = -160f;

        private static float attachedWallBoosterCurrentSpeed = 0f;
        private static bool attachedWallBoosting = false;
        private static EventInstance conveyorLoopSfx;
        private static float playerWallBoostTimer = 0f;

        internal static void Hook() {
            On.Celeste.Player.ClimbBegin += Player_ClimbBegin;
            On.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
            IL.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
            On.Celeste.Player.ClimbEnd += Player_ClimbEnd;
            On.Celeste.Player.ClimbJump += Player_ClimbJump;
            On.Celeste.Player.WallJump += Player_WallJump;
            On.Celeste.Player.Update += Player_Update;
        }

        internal static void Unhook() {
            On.Celeste.Player.ClimbBegin -= Player_ClimbBegin;
            On.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
            IL.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
            On.Celeste.Player.ClimbEnd -= Player_ClimbEnd;
            On.Celeste.Player.ClimbJump -= Player_ClimbJump;
            On.Celeste.Player.WallJump -= Player_WallJump;
            On.Celeste.Player.Update -= Player_Update;
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);
            if (playerWallBoostTimer > 0)
                playerWallBoostTimer -= Engine.DeltaTime;
        }

        private static void Player_WallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir) {
            PlayerWallBoost(self);
            orig(self, dir);
        }

        private static void Player_ClimbJump(On.Celeste.Player.orig_ClimbJump orig, Player self) {
            PlayerWallBoost(self);
            orig(self);
        }

        private static void PlayerWallBoost(Player player) {
            if (playerWallBoostTimer > 0) {
                player.LiftSpeed += Vector2.UnitY * Calc.Max(attachedWallBoosterCurrentSpeed, -80f);
                attachedWallBoosterCurrentSpeed = playerWallBoostTimer = 0f;
            }
        }

        private static void Player_ClimbEnd(On.Celeste.Player.orig_ClimbEnd orig, Player self) {
            orig(self);
            if (attachedWallBoosting) {
                attachedWallBoosting = false;
                conveyorLoopSfx.setParameterValue("end", 1f);
                conveyorLoopSfx.release();
            }
        }

        private static void Player_ClimbBegin(On.Celeste.Player.orig_ClimbBegin orig, Player self) {
            orig(self);
            attachedWallBoosterCurrentSpeed = 0f;
            attachedWallBoosting = false;
        }

        private static int Player_ClimbUpdate(On.Celeste.Player.orig_ClimbUpdate orig, Player self) {
            if (self.AttachedWallBoosterCheck()) {
                if (!attachedWallBoosting) {
                    attachedWallBoosting = true;
                    attachedWallBoosterCurrentSpeed = self.Speed.Y;
                    conveyorLoopSfx = Audio.Play(SFX.game_09_conveyor_activate, self.Position, "end", 0f);
                }
                playerWallBoostTimer = .25f;
                Audio.Position(conveyorLoopSfx, self.Position);

                attachedWallBoosterCurrentSpeed = Calc.Approach(attachedWallBoosterCurrentSpeed, WallBoosterSpeed, 600f * Engine.DeltaTime);
                self.Speed.Y = attachedWallBoosterCurrentSpeed;

                Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
            } else if (attachedWallBoosting) {
                attachedWallBoosting = false;
                conveyorLoopSfx.setParameterValue("end", 1f);
                conveyorLoopSfx.release();
            }

            int ret = orig(self);

            if (attachedWallBoosting) {
                // This is unset during orig so we have to set it here instead
                self.GetData()["wallBoosting"] = true;
            }

            return ret;
        }

        private static void Player_ClimbUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchLdstr(SFX.char_mad_grab_letgo));
            cursor.GotoNext(MoveType.After, instr => instr.Match(OpCodes.Brfalse_S));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(PlayerWallBoost);
        }

        #endregion

    }
}
