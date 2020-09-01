module CommunalHelperDreamRefill

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamRefill" DreamRefill(x::Integer, y::Integer, oneUse::Bool=false)

const placements = Ahorn.PlacementDict(
    "Dream Refill (Communal Helper)" => Ahorn.EntityPlacement(
        DreamRefill,
        "point"
    )
)

function Ahorn.selection(entity::DreamRefill)
    x, y = Ahorn.position(entity)

    sprite = "objects/CommunalHelper/dreamRefill/idle02"
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamRefill)
    sprite = "objects/CommunalHelper/dreamRefill/idle02"
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end