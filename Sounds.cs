using FMOD.Studio;

namespace Celeste.Mod.CommunalHelper {
    public static class CustomSFX {
        public const string game_connectedDreamBlock_dreamblock_fly_travel = "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_fly_travel";
        public const string game_connectedDreamBlock_dreamblock_shatter = "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_shatter";

        public const string game_dreamMoveBlock_dream_move_block_activate = "event:/CommunalHelperEvents/game/dreamMoveBlock/dream_move_block_activate";
        public const string game_dreamMoveBlock_dream_move_block_break = "event:/CommunalHelperEvents/game/dreamMoveBlock/dream_move_block_break";
        public const string game_dreamMoveBlock_dream_move_block_reappear = "event:/CommunalHelperEvents/game/dreamMoveBlock/dream_move_block_reappear";

        public const string game_dreamZipMover_dream_zip_mover = "event:/CommunalHelperEvents/game/dreamZipMover/dream_zip_mover";

        public const string game_dreamRefill_dream_refill_touch = "event:/CommunalHelperEvents/game/dreamRefill/dream_refill_touch";
        public const string game_dreamRefill_dream_refill_return = "event:/CommunalHelperEvents/game/dreamRefill/dream_refill_return";

        public const string game_dreamSwapBlock_dream_swap_block_move = "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_move";
        public const string game_dreamSwapBlock_dream_swap_block_move_end = "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_move_end";
        public const string game_dreamSwapBlock_dream_swap_block_return = "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_return";
        public const string game_dreamSwapBlock_dream_swap_block_return_end = "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_return_end";

        public const string game_stationBlock_station_block_seq = "event:/CommunalHelperEvents/game/stationBlock/station_block_seq";
        public const string game_stationBlock_moon_block_seq = "event:/CommunalHelperEvents/game/stationBlock/moon_block_seq";
		
		public const string game_usableSummitGems_gem_unlock_la = "event:/CommunalHelperEvents/game/usableSummitGems/gem_unlock_la";
		public const string game_usableSummitGems_gem_unlock_ti = "event:/CommunalHelperEvents/game/usableSummitGems/gem_unlock_ti";
    }

    public static class CustomBanks {
        public static Bank CommunalHelper = Audio.Banks.Banks["bank:/CommunalHelperBank"];
    }

}
