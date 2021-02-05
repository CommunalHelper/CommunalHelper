module CommunalHelperDreamRefill

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamRefill" DreamRefill(x::Integer, y::Integer, oneUse::Bool=false)

const placements = Ahorn.PlacementDict(
    "Dream Refill (Communal Helper)" => Ahorn.EntityPlacement(
        DreamRefill,
        "point"
    )
)

const sprite = "objects/CommunalHelper/dreamRefill/idle02"

function Ahorn.selection(entity::DreamRefill)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamRefill) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end