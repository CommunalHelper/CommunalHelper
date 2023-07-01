local drawableNinePatch = require "structs.drawable_nine_patch"
local drawableRectangle = require "structs.drawable_rectangle"
local utils = require "utils"

local aeroBlock = {}

aeroBlock.name = "CommunalHelper/AeroBlockFlying"
aeroBlock.depth = 4999
aeroBlock.minimumSize = {16, 16}

aeroBlock.nodeLimits = {0, 1}

aeroBlock.placements = {
    {
        name = "aero_block_flying",
        data = {
            width = 16,
            height = 16,
            inactive = false,
        }
    }
}

local backgroundColor = {20 / 255, 3 / 255, 3 / 255}
local outlineColor = {0, 0, 0}
local blockTexture = "objects/CommunalHelper/aero_block/block"

local blockNinePatchOptions = {
    mode = "border",
    borderMode = "repeat"
}

function aeroBlock.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16

    local outline = drawableRectangle.fromRectangle("fill", x - 1, y - 1, width + 2, height + 2, backgroundColor)
    local rectangle = drawableRectangle.fromRectangle("fill", x, y, width, height, outlineColor)

    table.insert(sprites, outline:getDrawableSprite())
    table.insert(sprites, rectangle:getDrawableSprite())

    local frameNinePatch = drawableNinePatch.fromTexture(blockTexture, blockNinePatchOptions, x, y, width, height)
    local frameSprites = frameNinePatch:getDrawableSprite()

    for _, sprite in ipairs(frameSprites) do
        table.insert(sprites, sprite)
    end

    return sprites
end

function aeroBlock.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16

    local rect = utils.rectangle(x, y, width, height)

    local nodes = entity.nodes or {}
    if #nodes == 1 then
        local nx, ny = nodes[1].x or 0, nodes[1].y or 0
        return rect, {utils.rectangle(nx, ny, width, height)}
    end

    return rect
end

return aeroBlock