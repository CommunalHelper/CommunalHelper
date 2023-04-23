module CommunalHelperChainedFallingBlock

using ..Ahorn, Maple
using Cairo

@mapdef Entity "CommunalHelper/ChainedFallingBlock" ChainedFallingBlock(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    height::Integer=Maple.defaultBlockWidth,
    tiletype::String="3",
    climbFall::Bool=true,
    behind::Bool=false,
    fallDistance::Integer=64,
    centeredChain::Bool=false,
    chainOutline::Bool=true,
    indicator::Bool=false,
    indicatorAtStart::Bool=false,
    chainTexture::String="objects/CommunalHelper/chains/chain",
)

const placements = Ahorn.PlacementDict(
    "Falling Block (Chained) (Communal Helper)" => Ahorn.EntityPlacement(
        ChainedFallingBlock,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.tileEntityFinalizer,
    ),
)

Ahorn.editingOptions(entity::ChainedFallingBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::ChainedFallingBlock) = 8, 8
Ahorn.resizable(entity::ChainedFallingBlock) = true, true

Ahorn.selection(entity::ChainedFallingBlock) = Ahorn.getEntityRectangle(entity)

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ChainedFallingBlock, room::Maple.Room) 
    x, y = Ahorn.position(entity)
    w = Int(get(entity.data, "width", 8))
    h = Int(get(entity.data, "height", 8)) + Int(get(entity.data, "fallDistance", 64))

    save(ctx)

    set_dash(ctx, [0.6, 0.2])
    set_antialias(ctx, 1)
    set_line_width(ctx, 1)

    # width & height extend up and left for some reason.
    Ahorn.drawRectangle(ctx, x + 1, y + 1, w - 1, h - 1, (0.0, 0.0, 0.0, 0.0), (1.0, 1.0, 1.0, 0.5))

    restore(ctx)

    Ahorn.drawTileEntity(ctx, room, entity)
end

end
