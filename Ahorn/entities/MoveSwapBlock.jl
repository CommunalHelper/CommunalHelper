module CommunalHelperMoveSwapBlock
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/MoveSwapBlock" MoveSwapBlock(x::Integer, y::Integer, width::Integer=16, height::Integer=16,
    direction::String="Left", canSteer::Bool=false, MoveSpeed::Number=60.0, Accel::Number=300.0,
    SwapSpeedMult::Number=1.0
)

function swapFinalizer(entity)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    entity.data["nodes"] = [(x + width, y)]
end
const directions = Maple.move_block_directions
const placements = Ahorn.PlacementDict(
    "Move Swap Block (Communal Helper)" => Ahorn.EntityPlacement(
        MoveSwapBlock,
        "rectangle",
        Dict{String, Any}(
        ),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + 16, Int(entity.data["y"]))]
        end
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

const arrows = Dict{String, String}(
    "up" => "objects/CommunalHelper/entities/arrow02",
    "left" => "objects/CommunalHelper/entities/arrow04",
    "right" => "objects/CommunalHelper/entities/arrow00",
    "down" => "objects/CommunalHelper/entities/arrow06",
)

const button = "objects/moveBlock/button"
const buttonColor = (71, 64, 112, 255) ./ 255


getTextures(entity::MoveSwapBlock) = "objects/swapblock/blockRed", "objects/swapblock/target", "objects/swapblock/midBlockRed00"

function renderTrail(ctx, x::Number, y::Number, width::Number, height::Number, trail::String)
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, trail, x + (i - 1) * 8, y + 2, 6, 0, 8, 6)
        Ahorn.drawImage(ctx, trail, x + (i - 1) * 8, y + height - 8, 6, 14, 8, 6)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, trail, x + 2, y + (i - 1) * 8, 0, 6, 6, 8)
        Ahorn.drawImage(ctx, trail, x + width - 8, y + (i - 1) * 8, 14, 6, 6, 8)
    end

    for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, trail, x + (i - 1) * 8, y + (j - 1) * 8, 6, 6, 8, 8)
    end

    Ahorn.drawImage(ctx, trail, x + width - 8, y + 2, 14, 0, 6, 6)
    Ahorn.drawImage(ctx, trail, x + width - 8, y + height - 8, 14, 14, 6, 6)
    Ahorn.drawImage(ctx, trail, x + 2, y + 2, 0, 0, 6, 6)
    Ahorn.drawImage(ctx, trail, x + 2, y + height - 8, 0, 14, 6, 6)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::MoveSwapBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    stopX, stopY = Int.(entity.data["nodes"][1])

    renderTrail(ctx, min(x, stopX), min(y, stopY), abs(x - stopX) + width, abs(y - stopY) + height, "objects/swapblock/target")

    renderMoveBlock(ctx, entity, x, y, width, height)
    renderMoveBlock(ctx, entity, stopX, stopY, width, height)



end

function renderMoveBlock(ctx, entity::MoveSwapBlock, x::Integer, y::Integer, width::Integer, height::Integer)
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    canSteer = get(entity.data, "canSteer", false)
    direction = lowercase(get(entity.data, "direction", "up"))
    arrowSprite = Ahorn.getSprite(arrows[lowercase(direction)], "Gameplay")

    frame = "objects/moveBlock/base"
    if canSteer
        if direction == "up" || direction == "down"
            frame = "objects/moveBlock/base_v"

        else
            frame = "objects/moveBlock/base_h"
        end
    end

    Ahorn.drawRectangle(ctx, x + 2,y + 2, width - 4, height - 4, highlightColor, highlightColor)
    Ahorn.drawRectangle(ctx, x + 8, y + 8, width - 16, height - 16, midColor)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)

        if canSteer && (direction != "up" && direction != "down")
            Ahorn.drawImage(ctx, button, x + (i - 1) * 8, y - 2, 6, 0, 8, 6, tint=buttonColor)
        end
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)

        if canSteer && (direction == "up" || direction == "down")
            Ahorn.Cairo.save(ctx)

            Ahorn.rotate(ctx, -pi / 2)
            Ahorn.drawImage(ctx, button, i * 8 - height - 8 - y, x-2, 6, 0, 8, 6, tint=buttonColor)
            Ahorn.scale(ctx, 1, -1)
            Ahorn.drawImage(ctx, button, i * 8 - height - 8 - y, -2 - width - x, 6, 0, 8, 6, tint=buttonColor)

            Ahorn.Cairo.restore(ctx)
        end
    end

    Ahorn.drawImage(ctx, frame, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, frame, x + width - 8, y + height - 8, 16, 16, 8, 8)

    if canSteer && (direction != "up" && direction != "down")
        Ahorn.Cairo.save(ctx)

        Ahorn.drawImage(ctx, button, x+2, y-2, 0, 0, 6, 6, tint=buttonColor)
        Ahorn.scale(ctx, -1, 1)
        Ahorn.drawImage(ctx, button, 2 - width - x, y-2, 0, 0, 6, 6, tint=buttonColor)

        Ahorn.Cairo.restore(ctx)
    end

    if canSteer && (direction == "up" || direction == "down")
        Ahorn.Cairo.save(ctx)

        Ahorn.rotate(ctx, -pi / 2)
        Ahorn.drawImage(ctx, button, 2-height - y, x-2, 0, 0, 8, 6, tint=buttonColor)
        Ahorn.drawImage(ctx, button, -10-y, x-2, 14, 0, 8, 6, tint=buttonColor)
        Ahorn.scale(ctx, 1, -1)
        Ahorn.drawImage(ctx, button, 2-height-y, - 2 -width - x, 0, 0, 8, 6, tint=buttonColor)
        Ahorn.drawImage(ctx, button, -10-y, -2 -width - x, 14, 0, 8, 6, tint=buttonColor)

        Ahorn.Cairo.restore(ctx)
    end

    Ahorn.drawRectangle(ctx, x + div(width - arrowSprite.width, 2) + 1, y + div(height - arrowSprite.height, 2) + 1, 8, 8, highlightColor, highlightColor)
    Ahorn.drawImage(ctx, arrowSprite, x + div(width - arrowSprite.width, 2), y + div(height - arrowSprite.height, 2))
end

end
