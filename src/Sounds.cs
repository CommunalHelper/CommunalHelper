using FMOD.Studio;

namespace Celeste.Mod.CommunalHelper;

public static class CustomSFX
{
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

    public const string game_customBoosters_heldBooster_move = "event:/CommunalHelperEvents/game/customBoosters/heldBooster/move";
    public const string game_customBoosters_heldBooster_move_cue = "event:/CommunalHelperEvents/game/customBoosters/heldBooster/move_cue";
    public const string game_customBoosters_heldBooster_blink = "event:/CommunalHelperEvents/game/customBoosters/heldBooster/blink";

    public const string game_chainedFallingBlock_chain_rattle = "event:/CommunalHelperEvents/game/chainedFallingBlock/chain_rattle";
    public const string game_chainedFallingBlock_chain_tighten_ceiling = "event:/CommunalHelperEvents/game/chainedFallingBlock/chain_tighten_ceiling";
    public const string game_chainedFallingBlock_chain_tighten_block = "event:/CommunalHelperEvents/game/chainedFallingBlock/chain_tighten_block";
    public const string game_chainedFallingBlock_attenuatedImpacts_boss_impact = "event:/CommunalHelperEvents/game/chainedFallingBlock/attenuatedImpacts/boss_impact";
    public const string game_chainedFallingBlock_attenuatedImpacts_wood_impact = "event:/CommunalHelperEvents/game/chainedFallingBlock/attenuatedImpacts/wood_impact";
    public const string game_chainedFallingBlock_attenuatedImpacts_ice_impact = "event:/CommunalHelperEvents/game/chainedFallingBlock/attenuatedImpacts/ice_impact";

    public const string game_chain_move = "event:/CommunalHelperEvents/game/chain/move";

    public const string game_dreamJellyfish_jelly_refill = "event:/CommunalHelperEvents/game/dreamJellyfish/jelly_refill";
    public const string game_dreamJellyfish_jelly_use = "event:/CommunalHelperEvents/game/dreamJellyfish/jelly_use";

    public const string game_berries_redless_break = "event:/CommunalHelperEvents/game/berries/redless/break";
    public const string game_berries_redless_warning = "event:/CommunalHelperEvents/game/berries/redless/warning";

    public const string game_seekerDashRefill_seeker_refill_return = "event:/CommunalHelperEvents/game/seekerDashRefill/seeker_refill_return";
    public const string game_seekerDashRefill_seeker_refill_touch = "event:/CommunalHelperEvents/game/seekerDashRefill/seeker_refill_touch";

    #region partial strawberry jam sfx bank port

    public const string game_strawberryJam_bee_fireball_idle = "event:/CommunalHelperEvents/game/strawberryJam/game/bee/fireball_idle";
    public const string game_strawberryJam_boost_block_boost = "event:/CommunalHelperEvents/game/strawberryJam/game/boost_block/boost";
    public const string game_strawberryJam_bubble_emitter_bubble_pop = "event:/CommunalHelperEvents/game/strawberryJam/game/bubble_emitter/bubble_pop";
    public const string game_strawberryJam_bubble_emitter_emitter_generate = "event:/CommunalHelperEvents/game/strawberryJam/game/bubble_emitter/emitter_generate";
    public const string game_strawberryJam_dash_seq_fail = "event:/CommunalHelperEvents/game/strawberryJam/game/dash_seq/fail";
    public const string game_strawberryJam_dash_zip_mover_zip_mover = "event:/CommunalHelperEvents/game/strawberryJam/game/dash_zip_mover/zip_mover";
    public const string game_strawberryJam_drum_swapblock_drum_swapblock_move = "event:/CommunalHelperEvents/game/strawberryJam/game/drum_swapblock/drum_swapblock_move";
    public const string game_strawberryJam_drum_swapblock_drum_swapblock_move_end = "event:/CommunalHelperEvents/game/strawberryJam/game/drum_swapblock/drum_swapblock_move_end";
    public const string game_strawberryJam_loop_block_sideboost = "event:/CommunalHelperEvents/game/strawberryJam/game/loop_block/sideboost";
    public const string game_strawberryJam_solar_elevator_elevate = "event:/CommunalHelperEvents/game/strawberryJam/game/solar_elevator/elevate";
    public const string game_strawberryJam_solar_elevator_halt = "event:/CommunalHelperEvents/game/strawberryJam/game/solar_elevator/halt";
    public const string game_strawberryJam_solar_express_rock_stream = "event:/CommunalHelperEvents/game/strawberryJam/game/solar_express/rock_stream";
    public const string game_strawberryJam_triple_boost_flower_boost_1 = "event:/CommunalHelperEvents/game/strawberryJam/game/triple_boost_flower/boost_1";
    public const string game_strawberryJam_triple_boost_flower_boost_2 = "event:/CommunalHelperEvents/game/strawberryJam/game/triple_boost_flower/boost_2";
    public const string game_strawberryJam_triple_boost_flower_boost_3 = "event:/CommunalHelperEvents/game/strawberryJam/game/triple_boost_flower/boost_3";
    public const string game_strawberryJam_triple_boost_flower_glider_movement = "event:/CommunalHelperEvents/game/strawberryJam/game/triple_boost_flower/glider_movement";

    #endregion

    public const string game_elytra_deploy = "event:/CommunalHelperEvents/game/elytra/deploy";
    public const string game_elytra_gliding = "event:/CommunalHelperEvents/game/elytra/gliding";
    public const string game_elytra_refill = "event:/CommunalHelperEvents/game/elytra/refill";
    public const string game_elytra_rings_boost = "event:/CommunalHelperEvents/game/elytra/rings/boost";
    public const string game_elytra_rings_stop = "event:/CommunalHelperEvents/game/elytra/rings/stop";
    public const string game_elytra_rings_refill = "event:/CommunalHelperEvents/game/elytra/rings/refill";
    public const string game_elytra_rings_booster_ambience = "event:/CommunalHelperEvents/game/elytra/rings/booster_ambience";
    public const string game_elytra_rings_note = "event:/CommunalHelperEvents/game/elytra/rings/note";

    public const string game_aero_block_deploy_propeller = "event:/CommunalHelperEvents/game/aero_block/deploy_propeller";
    public const string game_aero_block_failure = "event:/CommunalHelperEvents/game/aero_block/failure";
    public const string game_aero_block_launch_sequence = "event:/CommunalHelperEvents/game/aero_block/launch_sequence";
    public const string game_aero_block_loop = "event:/CommunalHelperEvents/game/aero_block/loop";
    public const string game_aero_block_morse = "event:/CommunalHelperEvents/game/aero_block/morse";
    public const string game_aero_block_retract_propeller = "event:/CommunalHelperEvents/game/aero_block/retract_propeller";
    public const string game_aero_block_smash = "event:/CommunalHelperEvents/game/aero_block/smash";
    public const string game_aero_block_static = "event:/CommunalHelperEvents/game/aero_block/static";
    public const string game_aero_block_success = "event:/CommunalHelperEvents/game/aero_block/success";
    public const string game_aero_block_warn = "event:/CommunalHelperEvents/game/aero_block/warn";
    public const string game_aero_block_button_charge = "event:/CommunalHelperEvents/game/aero_block/button_charge";
    public const string game_aero_block_button_let_go = "event:/CommunalHelperEvents/game/aero_block/button_let_go";
    public const string game_aero_block_button_press = "event:/CommunalHelperEvents/game/aero_block/button_press";
    public const string game_aero_block_push = "event:/CommunalHelperEvents/game/aero_block/push";
    public const string game_aero_block_lock = "event:/CommunalHelperEvents/game/aero_block/lock";
    public const string game_aero_block_ding = "event:/CommunalHelperEvents/game/aero_block/ding";
    public const string game_aero_block_wind_up = "event:/CommunalHelperEvents/game/aero_block/wind_up";
    public const string game_aero_block_impact = "event:/CommunalHelperEvents/game/aero_block/impact";

    public const string game_shapeshifter_shake = "event:/CommunalHelperEvents/game/shapeshifter/shake";
    public const string game_shapeshifter_supermove = "event:/CommunalHelperEvents/game/shapeshifter/supermove";
    public const string game_shapeshifter_move = "event:/CommunalHelperEvents/game/shapeshifter/move";
}

public static class CustomBanks
{
    public static Bank CommunalHelper { get; } = Audio.Banks.Banks["bank:/CommunalHelperBank"];
}
