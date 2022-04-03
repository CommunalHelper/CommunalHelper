using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/AttachedWallBooster")]
    [TrackedAs(typeof(WallBooster))]
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

        private const string Player_attachedWallBoosterCurrentSpeed = "communalHelperAttachedWallBoosterCurrentSpeed";
        private const string Player_attachedWallBoosterLiftSpeedTimer = "communalHelperAttachedWallBoosterLiftSpeedTimer";

        internal static void Hook() {
            On.Celeste.Player.ClimbBegin += Player_ClimbBegin;
            IL.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
            On.Celeste.Player.ClimbJump += Player_ClimbJump;
            On.Celeste.Player.WallJump += Player_WallJump;

            On.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.Update += Player_Update;
        }

        internal static void Unhook() {
            On.Celeste.Player.ClimbBegin -= Player_ClimbBegin;
            IL.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
            On.Celeste.Player.ClimbJump -= Player_ClimbJump;
            On.Celeste.Player.WallJump -= Player_WallJump;
            On.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.Update -= Player_Update;
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);

            DynData<Player> data = self.GetData();
            data[Player_attachedWallBoosterCurrentSpeed] = 0f;
            data[Player_attachedWallBoosterLiftSpeedTimer] = 0f;
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            orig(self);

            DynData<Player> data = self.GetData();
            float timer = (float) data[Player_attachedWallBoosterLiftSpeedTimer];
            if (timer > 0)
                data[Player_attachedWallBoosterLiftSpeedTimer] = Calc.Approach(timer, 0f, Engine.DeltaTime);
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
            DynData<Player> data = player.GetData();

            float timer = (float) data[Player_attachedWallBoosterLiftSpeedTimer];
            float currentSpeed = (float) data[Player_attachedWallBoosterCurrentSpeed];

            if (timer > 0 && currentSpeed < 0) {
                player.LiftSpeed += Vector2.UnitY * Calc.Max(currentSpeed, -80f);
                data[Player_attachedWallBoosterCurrentSpeed] = data[Player_attachedWallBoosterLiftSpeedTimer] = 0f;
            }
        }

        private static void Player_ClimbBegin(On.Celeste.Player.orig_ClimbBegin orig, Player self) {
            orig(self);
            self.GetData()[Player_attachedWallBoosterLiftSpeedTimer] = 0f;
        }

        private static void Player_ClimbUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.MatchLdstr(SFX.char_mad_grab_letgo));
            cursor.GotoNext(MoveType.After, instr => instr.Match(OpCodes.Brfalse_S));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(PlayerWallBoost);

            cursor.GotoNext(instr => instr.MatchLdstr(SFX.game_09_conveyor_activate));
            cursor.GotoNext(instr => instr.MatchMul());
            cursor.GotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Stfld);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(self => {
                DynData<Player> data = self.GetData();
                data[Player_attachedWallBoosterCurrentSpeed] = self.Speed.Y;
                data[Player_attachedWallBoosterLiftSpeedTimer] = .25f;    
            });
        }

        #endregion

    }
}
