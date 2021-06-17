module CommunalHelperDreamFallingBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/DreamFallingBlock" DreamFallingBlock(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    height::Integer=Maple.defaultBlockHeight,
    featherMode::Bool=false,
    oneUse::Bool=false,
    refillCount::Integer=-1,
    noCollide::Bool=false,
    below::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Dream Falling Block (Communal Helper)" => Ahorn.EntityPlacement(
        DreamFallingBlock,
        "rectangle",
    ),
)

Ahorn.minimumSize(entity::DreamFallingBlock) = 8, 8
Ahorn.resizable(entity::DreamFallingBlock) = true, true

Ahorn.selection(entity::DreamFallingBlock) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamFallingBlock)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    renderDreamBlock(ctx, 0, 0, width, height, entity.data)
end

end
