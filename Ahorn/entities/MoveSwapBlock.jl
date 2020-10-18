module CommunalHelperMoveSwapBlock
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/MoveSwapBlock" MoveSwapBlock(x::Integer, y::Integer, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
	direction::String="Left", canSteer::Bool=false, returns::Bool=true, freezeOnSwap::Bool=true,
	MoveSpeed::Number=60.0, Accel::Number=300.0, SwapSpeedMult::Number=1.0)

const placements = Ahorn.PlacementDict(
	"Move Swap Block (Communal Helper)" => Ahorn.EntityPlacement(
		MoveSwapBlock,
		"rectangle",
		Dict{String, Any}(),
		Ahorn.SwapBlock.swapFinalizer
	)
)

Ahorn.editingOptions(entity::MoveSwapBlock) = Dict{String, Any}(
	"direction" => Maple.move_block_directions
)

Ahorn.minimumSize(entity::MoveSwapBlock) = 16, 16
Ahorn.resizable(entity::MoveSwapBlock) = true, true
Ahorn.nodeLimits(entity::MoveSwapBlock) = 1, 1

function Ahorn.selection(entity::MoveSwapBlock)
	x, y = Ahorn.position(entity)
	stopX, stopY = Int.(entity.data["nodes"][1])

	width = Int(get(entity.data, "width", 8))
	height = Int(get(entity.data, "height", 8))

	return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(stopX, stopY, width, height)]
end

const midColor = (4, 3, 23) ./ 255
const highlightColor = (59, 50, 101) ./ 255

const arrow = "objects/CommunalHelper/moveSwapBlock/midBlockCardinal"
const arrowRotations = Dict{String, Number}(
	"Up" => 0,
	"Right" => pi / 2,
	"Down" => pi,
	"Left" => pi / 2 * 3
)

const gem = "objects/CommunalHelper/moveSwapBlock/midBlockOrange"
const gemOffsets = Dict{String, Tuple{Int, Int}}(
	"Up" => (0, -1),
	"Right" => (1, 0),
	"Down" => (0, 1),
	"Left" => (-1, 0),
)

const button = "objects/moveBlock/button"
const buttonColor = (255, 126, 16, 255) ./ 255


getTextures(entity::MoveSwapBlock) = "objects/swapblock/blockRed", "objects/swapblock/target", "objects/swapblock/midBlockRed00"

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::MoveSwapBlock)
   sprite = get(entity.data, "sprite", "block")
   startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
   stopX, stopY = Int.(entity.data["nodes"][1])

   width = Int(get(entity.data, "width", 32))
   height = Int(get(entity.data, "height", 32))
	direction = get(entity.data, "direction", "Up")

   renderSwapBlock(ctx, stopX, stopY, width, height, direction)
   Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::MoveSwapBlock)
	startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
	stopX, stopY = Int.(entity.data["nodes"][1])
	width = Int(get(entity.data, "width", 32))
	height = Int(get(entity.data, "height", 32))
	direction = get(entity.data, "direction", "Up")
	
	frame, trail, mid = getTextures(entity)
	Ahorn.SwapBlock.renderTrail(ctx, min(startX, stopX), min(startY, stopY), abs(startX - stopX) + width, abs(startY - stopY) + height, "objects/swapblock/moon/target")
	renderSwapBlock(ctx, startX, startY, width, height, direction)
	renderMoveBlockButtons(ctx, entity, startX, startY, width, height)
end

function renderSwapBlock(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number, dir::String)
	Ahorn.SwapBlock.renderSwapBlock(ctx, x, y, width, height, "objects/swapblock/midBlockRed00", "objects/swapblock/blockRed")
	
	# I don't really trust the way Ahorn handles rotation in sprites
	arrowSprite = Ahorn.getSprite(arrow, "Gameplay")
	Ahorn.Cairo.save(ctx)
	Ahorn.Cairo.translate(ctx, x + div(width, 2), y + div(height, 2))
	Ahorn.Cairo.rotate(ctx, arrowRotations[dir])
	Ahorn.Cairo.translate(ctx, -div(arrowSprite.width, 2), -div(arrowSprite.height, 2))
	Ahorn.drawImage(ctx, arrowSprite, 0, 0)
	Ahorn.Cairo.restore(ctx)
	
	gemSprite = Ahorn.getSprite(gem, "Gameplay")
	gemX, gemY = gemOffsets[dir]
	Ahorn.drawImage(ctx, gemSprite, x + div(width - gemSprite.width, 2) + gemX, y + div(height - gemSprite.height, 2) + gemY)
end

function renderMoveBlockButtons(ctx, entity::MoveSwapBlock, x::Integer, y::Integer, width::Integer, height::Integer)
	tilesWidth = div(width, 8)
	tilesHeight = div(height, 8)

	canSteer = get(entity.data, "canSteer", false)
	direction = (get(entity.data, "direction", "Up"))

	for i in 2:tilesWidth - 1
		if canSteer && (direction != "Up" && direction != "Down")
			Ahorn.drawImage(ctx, button, x + (i - 1) * 8, y - 2, 6, 0, 8, 6, tint=buttonColor)
		end
	end

	for i in 2:tilesHeight - 1
		if canSteer && (direction == "Up" || direction == "Down")
			Ahorn.Cairo.save(ctx)

			Ahorn.rotate(ctx, -pi / 2)
			Ahorn.drawImage(ctx, button, i * 8 - height - 8 - y, x-2, 6, 0, 8, 6, tint=buttonColor)
			Ahorn.scale(ctx, 1, -1)
			Ahorn.drawImage(ctx, button, i * 8 - height - 8 - y, -2 - width - x, 6, 0, 8, 6, tint=buttonColor)

			Ahorn.Cairo.restore(ctx)
		end
	end

	if canSteer && (direction != "Up" && direction != "Down")
		Ahorn.Cairo.save(ctx)

		Ahorn.drawImage(ctx, button, x+2, y-2, 0, 0, 6, 6, tint=buttonColor)
		Ahorn.scale(ctx, -1, 1)
		Ahorn.drawImage(ctx, button, 2 - width - x, y-2, 0, 0, 6, 6, tint=buttonColor)

		Ahorn.Cairo.restore(ctx)
	end

	if canSteer && (direction == "Up" || direction == "Down")
		Ahorn.Cairo.save(ctx)

		Ahorn.rotate(ctx, -pi / 2)
		Ahorn.drawImage(ctx, button, 2-height - y, x-2, 0, 0, 8, 6, tint=buttonColor)
		Ahorn.drawImage(ctx, button, -10-y, x-2, 14, 0, 8, 6, tint=buttonColor)
		Ahorn.scale(ctx, 1, -1)
		Ahorn.drawImage(ctx, button, 2-height-y, - 2 -width - x, 0, 0, 8, 6, tint=buttonColor)
		Ahorn.drawImage(ctx, button, -10-y, -2 -width - x, 14, 0, 8, 6, tint=buttonColor)

		Ahorn.Cairo.restore(ctx)
	end
end

end
