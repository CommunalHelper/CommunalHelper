module CommunalHelperCassetteZipMover

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/CassetteZipMover" CassetteZipMover(x::Integer, y::Integer, 
    width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    index::Integer=0, tempo::Number=1.0, permanent::Bool=false,
    waiting::Bool=false, ticking::Bool=false, noReturn::Bool=false,
    customColor="") 

const ropeColors = Dict{Int, Ahorn.colorTupleType}(
    1 => (194, 116, 171, 255) ./ 255,
	2 => (227, 214, 148, 255) ./ 255,
	3 => (128, 224, 141, 255) ./ 255
)

const defaultRopeColor = (110, 189, 245, 255) ./ 255

const placements = Ahorn.PlacementDict(
    "Cassette Zip Mover ($index - $color) (Communal Helper)" => Ahorn.EntityPlacement(
        CassetteZipMover,
        "rectangle",
        Dict{String, Any}(
            "index" => index,
        ),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
        end
    ) for (color, index) in cassetteColorNames
)

Ahorn.editingOptions(entity::CassetteZipMover) = Dict{String, Any}(
    "index" => cassetteColorNames
)

Ahorn.nodeLimits(entity::CassetteZipMover) = 1, -1

Ahorn.minimumSize(entity::CassetteZipMover) = 16, 16
Ahorn.resizable(entity::CassetteZipMover) = true, true

function Ahorn.selection(entity::CassetteZipMover)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    res = [Ahorn.Rectangle(x, y, width, height)]
	
	for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)
		
		push!(res, Ahorn.Rectangle(nx + floor(Int, width / 2) - 5, ny + floor(Int, height / 2) - 5, 10, 10))
	end
	
	return res
end

const textures = "objects/cassetteblock/solid", "objects/CommunalHelper/cassetteZipMover/cog"
const crossSprite = "objects/CommunalHelper/cassetteMoveBlock/x"

function renderCassetteZipMover(ctx::Ahorn.Cairo.CairoContext, entity::CassetteZipMover)
    x, y = Ahorn.position(entity)
    px, py = x, y

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    block, cog = textures

    index = Int(get(entity.data, "index", 0))

    color = getCassetteColor(index)
    hasCustomColor = false

    hexColor = String(get(entity.data, "customColor", ""))
    if hexColor != "" && length(hexColor) == 6
        color = tuple(Ahorn.argb32ToRGBATuple(parse(Int, hexColor, base=16))[1:3] ./ 255..., 1.0)
        hasCustomColor = true
    end

    ropeColor = hasCustomColor ? color : get(ropeColors, index, defaultRopeColor)

    # Iteration through all the nodes
	for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)
		cx, cy = px + width / 2, py + height / 2
		cnx, cny = nx + width / 2, ny + height / 2

		length = sqrt((px - nx)^2 + (py - ny)^2)
		theta = atan(cny - cy, cnx - cx)

		Ahorn.Cairo.save(ctx)

		Ahorn.translate(ctx, cx, cy)
		Ahorn.rotate(ctx, theta)

		Ahorn.setSourceColor(ctx, ropeColor)
		Ahorn.set_antialias(ctx, 1)
		Ahorn.set_line_width(ctx, 1);

		# Offset for rounding errors
		Ahorn.move_to(ctx, 0, 4 + (theta <= 0))
		Ahorn.line_to(ctx, length, 4 + (theta <= 0))

		Ahorn.move_to(ctx, 0, -4 - (theta > 0))
		Ahorn.line_to(ctx, length, -4 - (theta > 0))

		Ahorn.stroke(ctx)

		Ahorn.Cairo.restore(ctx)
		
		Ahorn.drawSprite(ctx, cog, cnx, cny, tint=color)
		
		px, py = nx, ny
	end
    
    renderCassetteBlock(ctx, x, y, width, height, index, color)

    if Bool(get(entity.data, "noReturn", false))
        noReturnSprite = Ahorn.getSprite(crossSprite, "Gameplay")
        Ahorn.drawImage(ctx, noReturnSprite, x + div(width - noReturnSprite.width, 2), y + div(height - noReturnSprite.height, 2), tint=color)
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CassetteZipMover)
    renderCassetteZipMover(ctx, entity)
end

end