using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;

namespace Celeste.Mod.CommunalHelper.Entities {
    [Tracked]
    public class ManualCassetteController : Entity {

        private int roomBeats;
        private int currentIndex;

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (scene.Tracker.GetEntity<CassetteBlockManager>() != null)
                throw new Exception("CassetteBlockManager detected in same room as ManualCassetteController");

            roomBeats = SceneAs<Level>().CassetteBlockBeats;
        }

        public override void Update() {
            base.Update();
            if (CommunalHelperModule.Settings.CycleCassetteBlocks.Pressed) {
                currentIndex++;
                currentIndex %= roomBeats;
                SetActiveIndex(currentIndex);
                Audio.Play("event:/game/general/cassette_block_switch_" + ((currentIndex % 2) + 1));
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Short); 
            }

        }

        public void SetActiveIndex(int index) {
            foreach (CassetteBlock entity in Scene.Tracker.GetEntities<CassetteBlock>()) {
                entity.Activated = entity.Index == index;
            }
        }

        private static IDetour hook_Level_orig_LoadLevel;
        public static void Load() {
            hook_Level_orig_LoadLevel = new ILHook(typeof(Level).GetMethod("orig_LoadLevel"), Level_orig_LoadLevel);
        }

        public static void Unload() {
            hook_Level_orig_LoadLevel.Dispose();
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
                if (level.Session.LevelData.Entities.Exists(entityData => entityData.Name == "CommunalHelper/ManualCassetteController")) {
                    level.Tracker.GetEntity<CassetteBlockManager>()?.RemoveSelf();
                    level.Add(new ManualCassetteController());
                    // Lists were just updated so there's no harm in doing it again (hopefully)
                    level.Entities.UpdateLists(); 
                }
                return level;
            });
        }
    }
}
