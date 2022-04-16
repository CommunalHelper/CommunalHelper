module CommunalHelperDashStateTrigger

using ..Ahorn, Maple

@mapdef Trigger "CommunalHelper/DashStateTrigger" DashStateTrigger(
    x::Number,
    y::Number,
    width::Number=Maple.defaultTriggerWidth,
    height::Number=Maple.defaultTriggerHeight,
    mode::String="Trigger",
    dashState::String="DreamTunnel",
);

const Modes = [
    "OneUse",
    "Trigger",
    "Field",
]

const DashStates = [
    "DreamTunnel",
    "SeekerDash",
]

Ahorn.editingOptions(entity::DashStateTrigger) = Dict{String, Any}(
    "dashState" => DashStates,
    "mode" => Modes,
)

end
