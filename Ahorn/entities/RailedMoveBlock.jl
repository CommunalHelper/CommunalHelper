module CommunalHelperRailedMoveBlock

using ..Ahorn, Maple

const steeringModes = ["Horizontal", "Vertical", "Both"]

@mapdef Entity "CommunalHelper/RailedMoveBlock" RailedMoveBlock(
			x::Integer, y::Integer,
			width::Integer=16, height::Integer=16,
			nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[],
            steeringMode::String="Horizontal")

const placements = Ahorn.PlacementDict(
    "Railed Move Block ($steeringMode) (Communal Helper)" => Ahorn.EntityPlacement(
        RailedMoveBlock,
		"rectangle",
		Dict{String, Any}(
            "steeringMode" => steeringMode
        ),
		function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
        end
    ) for steeringMode in steeringModes
)

Ahorn.nodeLimits(entity::RailedMoveBlock) = 1, 1

Ahorn.minimumSize(entity::RailedMoveBlock) = 16, 16
Ahorn.resizable(entity::RailedMoveBlock) = true, true

Ahorn.editingOptions(entity::RailedMoveBlock) = Dict{String, Any}(
   "steeringMode" => steeringModes
)

const midColor = (4, 3, 23) ./ 255
const highlightColor = (59, 50, 101) ./ 255
const buttonColor = (71, 64, 112, 255) ./ 255

const button = "objects/moveBlock/button"

function Ahorn.selection(entity::RailedMoveBlock)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(nx + floor(Int, width / 2) - 5, ny + floor(Int, height / 2) - 5, 10, 10)]
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::RailedMoveBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))
    nx, ny = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    cog = "objects/zipmover/cog"
    cx, cy = x + width / 2, y + height / 2
    cnx, cny = nx + width / 2, ny + height / 2

    length = sqrt((x - nx)^2 + (y - ny)^2)
    theta = atan(cny - cy, cnx - cx)

    Ahorn.Cairo.save(ctx)

    Ahorn.translate(ctx, cx, cy)
    Ahorn.rotate(ctx, theta)

    Ahorn.setSourceColor(ctx, midColor)
    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1);

    # Offset for rounding errors
    Ahorn.move_to(ctx, 0, 4 + (theta <= 0))
    Ahorn.line_to(ctx, length, 4 + (theta <= 0))

    Ahorn.move_to(ctx, 0, -4 - (theta > 0))
    Ahorn.line_to(ctx, length, -4 - (theta > 0))

    Ahorn.stroke(ctx)

    Ahorn.Cairo.restore(ctx)

    Ahorn.drawSprite(ctx, cog, cnx, cny)

    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    steeringMode = get(entity.data, "steeringMode", "Horizontal")
    both = true
    horizontal = false
    vertical = false

    frame = "objects/CommunalHelper/railedMoveBlock/base_both"
    if steeringMode == "Horizontal"
        frame = "objects/moveBlock/base_v"
        both = false
        horizontal = true
    elseif steeringMode == "Vertical"
        frame = "objects/moveBlock/base_h"
        both = false
        vertical = true
    end

    Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, highlightColor, highlightColor)
    Ahorn.drawRectangle(ctx, x + 8, y + 8, width - 16, height - 16, midColor)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)

        if both || vertical
            Ahorn.drawImage(ctx, button, x + (i - 1) * 8, y - 2, 6, 0, 8, 6, tint=buttonColor)
        end
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)

        if both || horizontal
            Ahorn.Cairo.save(ctx)

            Ahorn.rotate(ctx, -pi / 2)
            Ahorn.drawImage(ctx, button, -y + i * 8 - height - 8, x - 2, 6, 0, 8, 6, tint=buttonColor)
            Ahorn.scale(ctx, 1, -1)
            Ahorn.drawImage(ctx, button, -y + i * 8 - height - 8, -x - 2 - width, 6, 0, 8, 6, tint=buttonColor)

            Ahorn.Cairo.restore(ctx)
        end
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8)

    if both || vertical
        Ahorn.Cairo.save(ctx)

        Ahorn.drawImage(ctx, button, x + 2, y - 2, 0, 0, 6, 6, tint=buttonColor)
        Ahorn.scale(ctx, -1, 1)
        Ahorn.drawImage(ctx, button, -x + 2 - width, y - 2, 0, 0, 6, 6, tint=buttonColor)

        Ahorn.Cairo.restore(ctx)
    end

    if both || horizontal
        Ahorn.Cairo.save(ctx)

        Ahorn.rotate(ctx, -pi / 2)
        Ahorn.drawImage(ctx, button, -y - height + 2, x - 2, 0, 0, 8, 6, tint=buttonColor)
        Ahorn.drawImage(ctx, button, -y - 10, x - 2, 14, 0, 8, 6, tint=buttonColor)
        Ahorn.scale(ctx, 1, -1)
        Ahorn.drawImage(ctx, button, -y - height + 2, -x - 2 -width, 0, 0, 8, 6, tint=buttonColor)
        Ahorn.drawImage(ctx, button, -y - 10, -x - 2 - width, 14, 0, 8, 6, tint=buttonColor)

        Ahorn.Cairo.restore(ctx)
    end

    # Ahorn.drawRectangle(ctx, div(width - arrowSprite.width, 2) + 1, div(height - arrowSprite.height, 2) + 1, 8, 8, highlightColor, highlightColor)
    # Ahorn.drawImage(ctx, arrowSprite, div(width - arrowSprite.width, 2), div(height - arrowSprite.height, 2))
end

end