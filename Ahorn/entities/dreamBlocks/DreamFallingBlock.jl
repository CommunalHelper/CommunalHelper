module CommunalHelperDreamFallingBlock

using ..Ahorn, Maple
using Ahorn.Cairo
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
    fallDistance::Integer=64,
    centeredChain::Bool=false,
    chainOutline::Bool=true,
    indicator::Bool=false,
    indicatorAtStart::Bool=false,
    chained::Bool=false,
    quickDestroy::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Dream Falling Block (Communal Helper)" => Ahorn.EntityPlacement(
        DreamFallingBlock,
        "rectangle",
    ),
    "Dream Falling Block (Chained) (Communal Helper)" => Ahorn.EntityPlacement(
        DreamFallingBlock,
        "rectangle",
        Dict{String, Any}(
            "chained" => true,
        ),
    ),
)

Ahorn.minimumSize(entity::DreamFallingBlock) = 8, 8
Ahorn.resizable(entity::DreamFallingBlock) = true, true

Ahorn.selection(entity::DreamFallingBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.editingIgnored(entity::DreamFallingBlock, multiple::Bool=false) =
    multiple ? 
    String["x", "y", "width", "height", "chained"] : (Bool(get(entity.data, "chained", false)) ?
    String["chained"] : String["chained", "fallDistance", "centeredChain", "chainOutline", "indicator", "indicatorAtStart"])

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamFallingBlock)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    if Bool(get(entity.data, "chained", false))
        save(ctx)
    
        set_dash(ctx, [0.6, 0.2])
        set_antialias(ctx, 1)
        set_line_width(ctx, 1)
    
        # width & height extend up and left for some reason.
        Ahorn.drawRectangle(ctx, 0, 0, width, height + Integer(get(entity.data, "fallDistance", 64)), (0.0, 0.0, 0.0, 0.0), (1.0, 1.0, 1.0, 0.5))
    
        restore(ctx)
    end

    renderDreamBlock(ctx, 0, 0, width, height, entity.data)
end

end
