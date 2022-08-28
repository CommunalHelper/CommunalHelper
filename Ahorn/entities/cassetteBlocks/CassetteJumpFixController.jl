module CommunalHelperCassetteJumpFixController

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/CassetteJumpFixController" CassetteJumpFixController(
    x::Integer,
    y::Integer,
    persistent::Bool=false,
    off::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Cassette Jump Fix Controller (Communal Helper)" => Ahorn.EntityPlacement(
        CassetteJumpFixController,
    ),
)

const sprite = "objects/CommunalHelper/cassetteJumpFixController/icon"

function Ahorn.selection(entity::CassetteJumpFixController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteJumpFixController)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end
