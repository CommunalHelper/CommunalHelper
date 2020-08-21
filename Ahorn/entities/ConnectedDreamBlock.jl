module CommunalHelperConnectedDreamBlock
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/ConnectedDreamBlock" ConnectedDreamBlock( x::Integer, y::Integer,
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, 
	featherMode::Bool = false, oneUse::Bool = false, doubleRefill::Bool=false)
			
const placements = Ahorn.PlacementDict(
    "Connected Dream Block (Normal) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedDreamBlock,
		"rectangle"
    ),
	"Connected Dream Block (Feather Mode) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedDreamBlock,
		"rectangle",
        Dict{String, Any}(
            "featherMode" => true
        )
    ),
	"Connected Dream Block (Normal, One Use) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedDreamBlock,
		"rectangle",
        Dict{String, Any}(
            "oneUse" => true
        )
    ),
	"Connected Dream Block (Feather Mode, One Use) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedDreamBlock,
		"rectangle",
        Dict{String, Any}(
            "featherMode" => true,
			"oneUse" => true
        )
    )
)

Ahorn.minimumSize(entity::ConnectedDreamBlock) = 8, 8
Ahorn.resizable(entity::ConnectedDreamBlock) = true, true

function Ahorn.selection(entity::ConnectedDreamBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
	
    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ConnectedDreamBlock)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.CommunalHelper.renderCustomDreamBlock(ctx, 0, 0, width, height, get(entity.data, "featherMode", false), get(entity.data, "oneUse", false))
end

end
