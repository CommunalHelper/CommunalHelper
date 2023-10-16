module CommunalHelperSJOshiroAttackTimeTrigger

using ..Ahorn, Maple

@mapdef Trigger "CommunalHelper/SJ/OshiroAttackTimeTrigger" OshiroAttackTimeTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, Enable::Bool=true)

const placements = Ahorn.PlacementDict(
    "Stable Oshiro Attack Time (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        OshiroAttackTimeTrigger,
        "rectangle"
    )
)

end