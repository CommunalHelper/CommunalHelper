local drawableNinePatch = require "structs.drawable_nine_patch"
local drawableRectangle = require "structs.drawable_rectangle"
local utils = require "utils"

local aeroBlock = {}

aeroBlock.name = "CommunalHelper/AeroBlockFlying"
aeroBlock.depth = 4999
aeroBlock.minimumSize = {16, 16}
aeroBlock.nodeLineRenderType = "line"

function aeroBlock.nodeLimits(room, entity)
    return (entity.inactive and 1 or 0), -1
end

aeroBlock.ignoredFields = {"_name", "_id", "_type", "inactive"}

aeroBlock.fieldInformation = {
    travelSpeed = {
        fieldType = "number",
        minimumValue = 0.0
    },
    travelMode = {
        editable = false,
        options = {
            ["Loop"] = "Loop",
            ["Back and Forth"] = "BackAndForth",
            ["With Player"] = "WithPlayer",
            ["With Player Once"] = "WithPlayerOnce",
        }
    },
    startColor = {
        fieldType = "color"
    },
    endColor = {
        fieldType = "color"
    }
}

aeroBlock.placements = {
    {
        name = "active",
        data = {
            width = 16,
            height = 16,
            inactive = false,
            travelSpeed = 32.0,
            travelMode = "Loop",
            startColor = "FEAC5E",
            endColor = "4BC0C8",
        }
    },
    {
        name = "inactive",
        data = {
            width = 16,
            height = 16,
            inactive = true,
            travelSpeed = 32.0,
            travelMode = "Loop",
            startColor = "FEAC5E",
            endColor = "4BC0C8",
        }
    }
}

local backgroundColor = {20 / 255, 3 / 255, 3 / 255}
local outlineColor = {0, 0, 0}
local blockTexture = "objects/CommunalHelper/aero_block/blocks/nnn"

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
    local nodes = entity.nodes or {}

    local rect = utils.rectangle(x, y, width, height)
    local noderects = {}

    for _, node in ipairs(nodes) do
        table.insert(noderects, utils.rectangle(node.x, node.y, width, height))
    end

    return rect, noderects
end

return aeroBlock