module CommunalHelperMoveBlockRedirect

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/MoveBlockRedirect" MoveBlockRedirect(x::Integer, y::Integer, 
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, 
	direction::String="Up", fastRedirect::Bool=false, deleteBlock::Bool=false, oneUse::Bool=false,
	modifier::Number=0.0, operation::String="Add")

const placements = Ahorn.PlacementDict(
	"Move Block Redirect (Communal Helper)" => Ahorn.EntityPlacement(
		MoveBlockRedirect,
		"rectangle"
	),
	"Move Block Redirect (One Use) (Communal Helper)" => Ahorn.EntityPlacement(
		MoveBlockRedirect,
		"rectangle",
		Dict{String, Any}(
			"oneUse" => true
		)
	),
	"Move Block Redirect (Delete Block) (Communal Helper)" => Ahorn.EntityPlacement(
		MoveBlockRedirect,
		"rectangle",
		Dict{String, Any}(
			"deleteBlock" => true
		)
	)
)

const opTypes = ["Add", "Subtract", "Multiply"]

Ahorn.editingIgnored(entity::MoveBlockRedirect, multiple::Bool=false) = multiple ? String["direction", "x", "y", "width", "height"] : String[]

Ahorn.editingOptions(entity::MoveBlockRedirect) = Dict{String, Any}(
    "direction" => Maple.move_block_directions,
	"operation" => opTypes
)
Ahorn.minimumSize(entity::MoveBlockRedirect) = Maple.defaultBlockWidth, Maple.defaultBlockHeight 
Ahorn.resizable(entity::MoveBlockRedirect) = true, true

Ahorn.selection(entity::MoveBlockRedirect) = Ahorn.getEntityRectangle(entity)

const defaultColor = (251, 206, 54, 255) ./ 255
const deleteColor = (204, 37, 65, 255) ./ 255
const fastColor = (41, 195, 47, 255) ./ 255
const slowColor = (28, 91, 179, 255) ./ 255

const block = "objects/CommunalHelper/moveBlockRedirect/block"

function Ahorn.rotated(entity::MoveBlockRedirect, steps::Int)
	if steps == 0
		return entity
	end

	dir = get(entity.data, "direction", "Up")
	
	if steps > 0
		if dir == "Up"
			dir = "Right"
		elseif dir == "Right"
			dir = "Down"
		elseif dir == "Down"
			dir = "Left"
		elseif dir == "Left"
			dir = "Up"
		end
	elseif steps < 0
		if dir == "Up"
			dir = "Left"
		elseif dir == "Right"
			dir = "Up"
		elseif dir == "Down"
			dir = "Right"
		elseif dir == "Left"
			dir = "Down"
		end
	end

	return MoveBlockRedirect(entity.x, entity.y, entity.width, entity.height, dir, get(entity.data, "fastRedirect", false))
end

function getRotation(dir::String)
	if dir == "Up"
		return pi * 1.5
	elseif dir == "Right"
		return 0
	elseif dir == "Down"
		return pi/2
	elseif dir == "Left"
		return pi
	elseif (fAngle = tryparse(Float64, dir)) !== nothing
		return fAngle
	else 
		return 0
	end
end

function getIconTextureAndColor(entity::MoveBlockRedirect)
	path = "objects/CommunalHelper/moveBlockRedirect/"
	deleteBlock = Bool(get(entity.data, "deleteBlock", false))
	if deleteBlock
		return path * "x", deleteColor
	end

	operation = String(get(entity.data, "operation", "Add"))
	modifier = abs(get(entity.data, "modifier", 0.0))
	if operation == "Add"
		if modifier == 0.0
			return path * "arrow", defaultColor
		end
		return path * "fast", fastColor
	elseif operation == "Subtract"
		if modifier == 0.0
			return path * "arrow", defaultColor
		end
		return path * "slow", slowColor
	elseif operation == "Multiply"
		if modifier == 0.0
			return path * "x", deleteColor
		elseif modifier > 1.0
			return path * "fast", fastColor
		elseif modifier < 1.0
			return path * "slow", slowColor
		end
	end

	return path * "arrow", defaultColor
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::MoveBlockRedirect) 
	width = Int(get(entity.data, "width", 32))
   	height = Int(get(entity.data, "height", 32))
	
	direction = String(get(entity.data, "direction", "Up"))
	sprite, color = getIconTextureAndColor(entity)

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

	for i in -1 : tileWidth, j in -1 : tileHeight
        tx = (i == -1) ? 0 : ((i == tileWidth) ? 16 : 8)
        ty = (j == -1) ? 0 : ((j == tileHeight) ? 16 : 8)
		if i == -1 || i == tileWidth || j == -1 || j == tileHeight
			Ahorn.drawImage(ctx, block, i * 8, j * 8, tx, ty, 8, 8, tint=color)
		end
	end
	
	# finicky
	Ahorn.Cairo.save(ctx)
	Ahorn.Cairo.translate(ctx, width/2, height/2)
	Ahorn.Cairo.rotate(ctx, getRotation(direction))
	Ahorn.Cairo.translate(ctx, -8, -8) # sprite always has a 16 x 16 size
	Ahorn.drawImage(ctx, sprite, 0, 0, tint=color)
	Ahorn.Cairo.restore(ctx)
end

end 