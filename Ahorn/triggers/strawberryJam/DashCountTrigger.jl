module CommunalHelperSJDashCountTrigger
using ..Ahorn, Maple

@mapdef Trigger "CommunalHelper/SJ/DashCountTrigger" DashCountTrigger(
    x::Integer,
    y::Integer,
    width::Integer=16,
    height::Integer=16,
    NumberOfDashes::Integer=1,
    DashAmountOnReset::Integer=1,
    ResetOnDeath::Bool=false
)

const placements = Ahorn.PlacementDict(
    "DashCount Trigger (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        DashCountTrigger,
        "rectangle"
    )
)

end