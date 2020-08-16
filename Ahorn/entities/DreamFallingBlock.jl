module CommunalHelperDreamFallingBlock
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamFallingBlock" DreamFallingBlock(x::Integer, y::Integer, 
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
	featherMode::Bool = false, oneUse::Bool = false)
        
            
const placements = Ahorn.PlacementDict(
    "Dream Falling Block (Communal Helper)" => Ahorn.EntityPlacement(
        DreamFallingBlock,
		"rectangle"
    )
)

Ahorn.minimumSize(entity::DreamFallingBlock) = 8, 8
Ahorn.resizable(entity::DreamFallingBlock) = true, true

function Ahorn.selection(entity::DreamFallingBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
	
    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamFallingBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.CommunalHelper.renderCustomDreamBlock(ctx, 0, 0, width, height, get(entity.data, "featherMode", false), get(entity.data, "oneUse", false))
end

end