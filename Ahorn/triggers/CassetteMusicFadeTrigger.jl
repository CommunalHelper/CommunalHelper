module CommunalHelperCassetteMusicFadeTrigger

using ..Ahorn, Maple

@mapdef Trigger "CommunalHelper/CassetteMusicFadeTrigger" CassetteMusicFadeTrigger(
    x::Integer, 
    y::Integer, 
    width::Integer=Maple.defaultTriggerWidth, 
    height::Integer=Maple.defaultTriggerHeight,
    fadeA::Number=0.0,
    fadeB::Number=1.0,
    parameter::String="",
    direction::String="leftToRight"
)

const placements = Ahorn.PlacementDict(
    "Cassette Music Fade Trigger (Communal Helper)" => Ahorn.EntityPlacement(
        CassetteMusicFadeTrigger,
        "rectangle",
    ),
)

Ahorn.editingOptions(trigger::CassetteMusicFadeTrigger) = Dict{String, Any}(
    "direction" => Maple.music_fade_trigger_directions
)

end