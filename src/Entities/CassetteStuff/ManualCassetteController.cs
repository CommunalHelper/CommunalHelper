using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked]
    public class ManualCassetteController : Entity {

        private int startIndex;

        private int roomBeats;
        private int currentIndex;

        public ManualCassetteController(EntityData data) {
            startIndex = data.Int("startIndex", 0);

            Visible = Collidable = false;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);

            if (Scene.Tracker.GetEntity<CassetteBlockManager>() != null)
                throw new Exception("CassetteBlockManager detected in same room as ManualCassetteController");

            roomBeats = SceneAs<Level>().CassetteBlockBeats;

            if (!(startIndex >= 0 && startIndex < roomBeats))
                throw new IndexOutOfRangeException("ManualCassetteController startIndex is outside of the number of CassetteBlock indices present");
            currentIndex = startIndex;

            SetActiveIndex(currentIndex);
        }

        public override void Update() {

            base.Update();
            if (CommunalHelperModule.Settings.CycleCassetteBlocks.Pressed) {
                Tick();
            }

        }

        public void Tick() {
            currentIndex++;
            currentIndex %= roomBeats;
            SetActiveIndex(currentIndex);
            Audio.Play("event:/game/general/cassette_block_switch_" + ((currentIndex % 2) + 1));
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        }

        public void SetActiveIndex(int index) {
            foreach (CassetteBlock entity in Scene.Tracker.GetEntities<CassetteBlock>()) {
                entity.Activated = entity.Index == index;
            }
        }

        private static IDetour hook_Level_orig_LoadLevel;
        public static void Load() {
            hook_Level_orig_LoadLevel = new ILHook(typeof(Level).GetMethod("orig_LoadLevel"), Level_orig_LoadLevel);

            On.Monocle.Engine.Update += Engine_Update;
        }

        public static void Unload() {
            hook_Level_orig_LoadLevel.Dispose();

            On.Monocle.Engine.Update -= Engine_Update;
        }

        private static void Level_orig_LoadLevel(ILContext il) {
            /*
            if (HasCassetteBlocks && ShouldCreateCassetteManager)
                // We're just after the `base`
                base.Tracker.GetEntity<CassetteBlockManager>()?.OnLevelStart();
            */

            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCallvirt<CassetteBlockManager>("OnLevelStart"));
            cursor.GotoPrev(instr => instr.MatchCall<Scene>("get_Tracker"));

            // Just gonna borrow the level object for a bit
            cursor.EmitDelegate<Func<Level, Level>>(level => {
                // This could be checked for as part of `Everest.Events.Level.OnLoadEntity` but meh
                EntityData data = level.Session.LevelData.Entities.FirstOrDefault(entityData => entityData.Name == "CommunalHelper/ManualCassetteController");
                if (data != null) {
                    level.Tracker.GetEntity<CassetteBlockManager>()?.RemoveSelf();
                    level.Add(new ManualCassetteController(data));
                    // Lists were just updated so there's no harm in doing it again (hopefully)
                    level.Entities.UpdateLists(); 
                }
                return level;
            });
        }

        private static void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, Microsoft.Xna.Framework.GameTime gameTime) {
            orig(self, gameTime);
            if (Engine.FreezeTimer > 0f && CommunalHelperModule.Settings.CycleCassetteBlocks.Pressed) {
                ManualCassetteController controller = Engine.Scene.Tracker.GetEntity<ManualCassetteController>();
                controller?.Tick();
            }
        }
    }
}
