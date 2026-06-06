module CommunalHelperSJShowHitboxTrigger
using ..Ahorn, Maple

@mapdef Trigger "CommunalHelper/SJ/ShowHitboxTrigger" ShowHitboxTrigger(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight, typeNames::String="")

const placements = Ahorn.PlacementDict(
    "Show Hitbox Trigger (Communal Helper)" => Ahorn.EntityPlacement(
        ShowHitboxTrigger,
        "rectangle"
    )
)

end