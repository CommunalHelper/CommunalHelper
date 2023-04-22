module CommunalHelperSJExplodingStrawberry
    
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/ExplodingStrawberry" ExplodingStrawberry(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Strawberry (Exploding) (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        ExplodingStrawberry
    )
)

const sprite = "collectables/strawberry/normal00";

function Ahorn.selection(entity::ExplodingStrawberry)
    x, y = Ahorn.position(entity)
    
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ExplodingStrawberry, room::Maple.Room)
    x, y = Ahorn.position(entity)
    
    Ahorn.drawSprite(ctx, sprite, x, y)
end

end