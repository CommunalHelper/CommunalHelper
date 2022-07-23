using FMOD.Studio;

namespace Celeste.Mod.CommunalHelper {
    public static class CustomSFX {
        public const string game_connectedDreamBlock_dreamblock_fly_travel = "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_fly_travel";
        public const string game_connectedDreamBlock_dreamblock_shatter = "event:/CommunalHelperEvents/game/connectedDreamBlock/dreamblock_shatter";

        public const string game_dreamMoveBlock_dream_move_block_activate = "event:/CommunalHelperEvents/game/dreamMoveBlock/dream_move_block_activate";
        public const string game_dreamMoveBlock_dream_move_block_break = "event:/CommunalHelperEvents/game/dreamMoveBlock/dream_move_block_break";
        public const string game_dreamMoveBlock_dream_move_block_reappear = "event:/CommunalHelperEvents/game/dreamMoveBlock/dream_move_block_reappear";

        public const string game_dreamRefill_dream_refill_touch = "event:/CommunalHelperEvents/game/dreamRefill/dream_refill_touch";
        public const string game_dreamRefill_dream_refill_return = "event:/CommunalHelperEvents/game/dreamRefill/dream_refill_return";

        public const string game_dreamSwapBlock_dream_swap_block_move = "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_move";
        public const string game_dreamSwapBlock_dream_swap_block_move_end = "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_move_end";
        public const string game_dreamSwapBlock_dream_swap_block_return = "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_return";
        public const string game_dreamSwapBlock_dream_swap_block_return_end = "event:/CommunalHelperEvents/game/dreamSwapBlock/dream_swap_block_return_end";

        public const string game_stationBlock_station_block_seq = "event:/CommunalHelperEvents/game/stationBlock/station_block_seq";
        public const string game_stationBlock_moon_block_seq = "event:/CommunalHelperEvents/game/stationBlock/moon_block_seq";
        public const string game_stationBlock_force_cue = "event:/CommunalHelperEvents/game/stationBlock/force_cue";

        public const string game_trackSwitchBox_smash = "event:/CommunalHelperEvents/game/trackSwitchBox/smash";

        public const string game_zipMover_moon_start = "event:/CommunalHelperEvents/game/zipMover/moon/start";
        public const string game_zipMover_moon_impact = "event:/CommunalHelperEvents/game/zipMover/moon/impact";
        public const string game_zipMover_moon_return = "event:/CommunalHelperEvents/game/zipMover/moon/return";
        public const string game_zipMover_moon_finish = "event:/CommunalHelperEvents/game/zipMover/moon/finish";
        public const string game_zipMover_moon_tick = "event:/CommunalHelperEvents/game/zipMover/moon/tick";

        public const string game_zipMover_normal_start = "event:/CommunalHelperEvents/game/zipMover/normal/start";
        public const string game_zipMover_normal_impact = "event:/CommunalHelperEvents/game/zipMover/normal/impact";
        public const string game_zipMover_normal_return = "event:/CommunalHelperEvents/game/zipMover/normal/return";
        public const string game_zipMover_normal_finish = "event:/CommunalHelperEvents/game/zipMover/normal/finish";
        public const string game_zipMover_normal_tick = "event:/CommunalHelperEvents/game/zipMover/normal/tick";

        public const string game_dreamZipMover_return = "event:/CommunalHelperEvents/game/dreamZipMover/return";
        public const string game_dreamZipMover_finish = "event:/CommunalHelperEvents/game/dreamZipMover/finish";
        public const string game_dreamZipMover_start = "event:/CommunalHelperEvents/game/dreamZipMover/start";
        public const string game_dreamZipMover_tick = "event:/CommunalHelperEvents/game/dreamZipMover/tick";
        public const string game_dreamZipMover_impact = "event:/CommunalHelperEvents/game/dreamZipMover/impact";

        public const string game_seedCrystalHeart_shards_reform = "event:/CommunalHelperEvents/game/seedCrystalHeart/shards_reform";
        public const string game_seedCrystalHeart_shard_collect = "event:/CommunalHelperEvents/game/seedCrystalHeart/shard_collect";
        public const string game_seedCrystalHeart_collect_all_main = "event:/CommunalHelperEvents/game/seedCrystalHeart/collect_all_main";

        public const string game_usableSummitGems_gem_unlock_la = "event:/CommunalHelperEvents/game/usableSummitGems/gem_unlock_la";
        public const string game_usableSummitGems_gem_unlock_ti = "event:/CommunalHelperEvents/game/usableSummitGems/gem_unlock_ti";

        public const string game_redirectMoveBlock_arrowblock_move = "event:/CommunalHelperEvents/game/redirectMoveBlock/arrowblock_move";
        public const string game_redirectMoveBlock_arrowblock_break_fast = "event:/CommunalHelperEvents/game/redirectMoveBlock/arrowblock_break";

        public const string game_shieldedRefill_diamond_return = "event:/CommunalHelperEvents/game/shieldedRefill/diamond_return";
        public const string game_shieldedRefill_pinkdiamond_return = "event:/CommunalHelperEvents/game/shieldedRefill/pinkdiamond_return";
        public const string game_shieldedRefill_diamondbubble_pop = "event:/CommunalHelperEvents/game/shieldedRefill/diamondbubble_pop";

        public const string game_melvin_seen_player = "event:/CommunalHelperEvents/game/melvin/seen_player";
        public const string game_melvin_impact = "event:/CommunalHelperEvents/game/melvin/impact";
        public const string game_melvin_move_loop = "event:/CommunalHelperEvents/game/melvin/move_loop";

        public const string game_railedMoveBlock_railedmoveblock_move = "event:/CommunalHelperEvents/game/railedMoveBlock/railedmoveblock_move";
        public const string game_railedMoveBlock_railedmoveblock_impact = "event:/CommunalHelperEvents/game/railedMoveBlock/railedmoveblock_impact";

        public const string game_customBoosters_dreamBooster_dreambooster_enter = "event:/CommunalHelperEvents/game/customBoosters/dreamBooster/dreambooster_enter";
        public const string game_customBoosters_dreamBooster_dreambooster_enter_cue = "event:/CommunalHelperEvents/game/customBoosters/dreamBooster/dreambooster_enter_cue";
        public const string game_customBoosters_dreamBooster_dreambooster_move = "event:/CommunalHelperEvents/game/customBoosters/dreamBooster/dreambooster_move";

        public const string game_chainedFallingBlock_chain_rattle = "event:/CommunalHelperEvents/game/chainedFallingBlock/chain_rattle";
        public const string game_chainedFallingBlock_chain_tighten_ceiling = "event:/CommunalHelperEvents/game/chainedFallingBlock/chain_tighten_ceiling";
        public const string game_chainedFallingBlock_chain_tighten_block = "event:/CommunalHelperEvents/game/chainedFallingBlock/chain_tighten_block";
        public const string game_chainedFallingBlock_attenuatedImpacts_boss_impact = "event:/CommunalHelperEvents/game/chainedFallingBlock/attenuatedImpacts/boss_impact";
        public const string game_chainedFallingBlock_attenuatedImpacts_wood_impact = "event:/CommunalHelperEvents/game/chainedFallingBlock/attenuatedImpacts/wood_impact";
        public const string game_chainedFallingBlock_attenuatedImpacts_ice_impact = "event:/CommunalHelperEvents/game/chainedFallingBlock/attenuatedImpacts/ice_impact";

        public const string game_chain_move = "event:/CommunalHelperEvents/game/chain/move";

        public const string game_dreamJellyfish_jelly_refill = "event:/CommunalHelperEvents/game/dreamJellyfish/jelly_refill";
        public const string game_dreamJellyfish_jelly_use = "event:/CommunalHelperEvents/game/dreamJellyfish/jelly_use";

        public const string game_elytra_gliding = "event:/CommunalHelperEvents/game/elytra/gliding";
        public const string game_elytra_wings_tighten = "event:/CommunalHelperEvents/game/elytra/wings-tighten";
    }

    public static class CustomBanks {
        public static Bank CommunalHelper = Audio.Banks.Banks["bank:/CommunalHelperBank"];
    }
}
