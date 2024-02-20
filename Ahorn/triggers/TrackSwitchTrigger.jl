module CommunalHelperTrackSwitchTrigger

using ..Ahorn, Maple

modes = ["Alternate", "On", "Off", "Reverse"]

@mapdef Trigger "CommunalHelper/TrackSwitchTrigger" TrackSwitchTrigger(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultTriggerWidth,
    height::Integer=Maple.defaultTriggerHeight,
    oneUse::Bool=true,
    flash::Bool=false,
    globalSwitch::Bool=false,
    mode::String="Alternate",
)

const placements = Ahorn.PlacementDict(
    "Track Switch Trigger (Communal Helper)" => Ahorn.EntityPlacement(
        TrackSwitchTrigger, 
        "rectangle"
    ),
)

Ahorn.editingOptions(trigger::TrackSwitchTrigger) = Dict{String,Any}(
    "mode" => modes,
)

end
