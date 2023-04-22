module CommunalHelperSJExpiringDashRefill

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/ExpiringDashRefill" ExpiringDashRefill(
    x::Integer,
    y::Integer,
    oneUse::Bool=false,
    dashExpirationTime::Number=5.0,
    hairFlashThreshold::Number=0.2
)

const placements = Ahorn.PlacementDict(
    "Expiring Dash Refill (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        ExpiringDashRefill
    )
)

const spriteOneDash = "objects/refill/idle00"

function Ahorn.selection(entity::ExpiringDashRefill)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(spriteOneDash, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ExpiringDashRefill) = Ahorn.drawSprite(ctx, spriteOneDash, 0, 0)

end