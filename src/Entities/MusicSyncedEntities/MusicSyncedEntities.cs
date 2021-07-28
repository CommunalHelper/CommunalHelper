using FMOD;
using FMOD.Studio;
using System.Runtime.InteropServices;

namespace Celeste.Mod.CommunalHelper.Entities {
    public static class MusicSyncedEntities {
        internal static void Load() {
            On.Celeste.Audio.SetMusic += Audio_SetMusic;
        }

        internal static void Unload() {
            On.Celeste.Audio.SetMusic -= Audio_SetMusic;
        }

        private static bool Audio_SetMusic(On.Celeste.Audio.orig_SetMusic orig, string path, bool startPlaying, bool allowFadeOut) {
            bool success = orig(path, startPlaying, allowFadeOut);

            if (Audio.CurrentMusicEventInstance != null && CommunalHelperModule.Session != null) {
                Audio.CurrentMusicEventInstance.setCallback((type, eventPtr, paramPtr) => {
                    TIMELINE_BEAT_PROPERTIES parameters = (TIMELINE_BEAT_PROPERTIES) Marshal.PtrToStructure(paramPtr, typeof(TIMELINE_BEAT_PROPERTIES));
                    
                    CommunalHelperModule.Session.MusicBeat = parameters.beat + (parameters.bar - 1) * parameters.timesignatureupper;
                    
                    return RESULT.OK;
                }, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
            }

            return success;
        }
    }
}
