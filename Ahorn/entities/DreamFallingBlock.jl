module CommunalHelperDreamFallingBlock
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamFallingBlock" DreamFallingBlock(
			x::Integer, y::Integer,
			width::Integer=16, height::Integer=16,
			featherMode::Bool = false,
            oneUse::Bool = false)
        
            
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

function renderSpaceJam(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number, fly::Bool, once::Bool)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)
	
	fillColor = fly ? (0.31, 0.69, 1.0, 0.4) : (0.0, 0.0, 0.0, 0.4)
	lineColor = once ? (1.0, 0.0, 0.0, 1.0) : (1.0, 1.0, 1.0, 1.0)
	
    Ahorn.drawRectangle(ctx, x, y, width, height, fillColor, lineColor)

    Ahorn.restore(ctx)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamFallingBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderSpaceJam(ctx, 0, 0, width, height, get(entity.data, "featherMode", false), get(entity.data, "oneUse", false))
end

end