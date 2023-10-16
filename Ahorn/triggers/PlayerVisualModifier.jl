module CommunalHelperPlayerVisualModTrigger

using ..Ahorn, Maple

@mapdef Trigger "CommunalHelper/PlayerVisualModTrigger" PlayerVisualModTrigger(x::Integer, y::Integer, modifier::String="Skateboard", revertOnLeave::Bool=true)

const placements = Ahorn.PlacementDict(
    "Player Visual Modification Trigger (Communal Helper)" => Ahorn.EntityPlacement(
        PlayerVisualModTrigger,
        "rectangle"
    )
)

end