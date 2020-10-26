module CommunalHelper

using ..Ahorn, Maple
using Cairo

export renderDreamBlock
export renderCassetteBlock, cassetteColorNames, getCassetteColor
export getExtensionRectangles, notAdjacent

function renderDreamBlock(ctx::CairoContext, x::Number, y::Number, width::Number, height::Number, featherMode::Bool=false, oneUse::Bool=false)
	save(ctx)

	set_antialias(ctx, 1)
	set_line_width(ctx, 1)

	fillColor = featherMode ? (0.31, 0.69, 1.0, 0.4) : (0.0, 0.0, 0.0, 0.4)
	lineColor = oneUse ? (1.0, 0.0, 0.0, 1.0) : (1.0, 1.0, 1.0, 1.0)
	Ahorn.drawRectangle(ctx, x, y, width, height, fillColor, lineColor)

	restore(ctx)
end

const cassetteBlock = "objects/cassetteblock/solid"
const cassetteColors = Dict{Int, Ahorn.colorTupleType}(
   1 => (240, 73, 190, 255) ./ 255,
	2 => (252, 220, 58, 255) ./ 255,
	3 => (56, 224, 78, 255) ./ 255
)
const defaultCassetteColor = (73, 170, 240, 255) ./ 255

const cassetteColorNames = Dict{String, Int}(
    "Blue" => 0,
    "Rose" => 1,
    "Bright Sun" => 2,
    "Malachite" => 3
)

getCassetteColor(index::Int) = get(cassetteColors, index, defaultCassetteColor)

function renderCassetteBlock(ctx::CairoContext, x, y, width, height, index)
   tileWidth = ceil(Int, width / 8)
   tileHeight = ceil(Int, height / 8)

   color = get(cassetteColors, index, defaultCassetteColor)

   for i in 1:tileWidth, j in 1:tileHeight
		tx = (i == 1) ? 0 : ((i == tileWidth) ? 16 : 8)
      ty = (j == 1) ? 0 : ((j == tileHeight) ? 16 : 8)

      Ahorn.drawImage(ctx, cassetteBlock, x + (i - 1) * 8, y + (j - 1) * 8, tx, ty, 8, 8, tint=color)
    end
end

"Get Rectangles from SolidExtensions present in the `room`."
function getExtensionRectangles(room::Room)
	entities = filter(e -> e.name == "CommunalHelper/SolidExtension", room.entities)
	rects = []

	for e in entities
		 push!(rects, Ahorn.Rectangle(
			  Int(get(e.data, "x", 0)),
			  Int(get(e.data, "y", 0)),
			  Int(get(e.data, "width", 8)),
			  Int(get(e.data, "height", 8))
		 ))
	end
		 
	return rects
end

"Check for collision with an array of rectangles at specified tile position"
function notAdjacent(x, y, ox, oy, rects)
	rect = Ahorn.Rectangle(x + ox + 4, y + oy + 4, 1, 1)

	for r in rects
		 if Ahorn.checkCollision(r, rect)
			  return false
		 end
	end

	return true
end
notAdjacent(entity::Entity, ox, oy, rects) = notAdjacent(Ahorn.getEntityPosition(entity)..., ox, oy, rects)

function detectMod(mod)
	any(s -> occursin(mod, lowercase(s)), Ahorn.getCelesteModZips()) ||
		any(s -> occursin(mod, lowercase(s)), Ahorn.getCelesteModDirs())
end

end