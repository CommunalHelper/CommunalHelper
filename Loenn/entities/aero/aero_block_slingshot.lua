local drawableNinePatch = require "structs.drawable_nine_patch"
local drawableRectangle = require "structs.drawable_rectangle"
local utils = require "utils"

local aeroBlockSlingshot = {}

aeroBlockSlingshot.name = "CommunalHelper/AeroBlockSlingshot"
aeroBlockSlingshot.depth = 4999
aeroBlockSlingshot.minimumSize = {16, 16}

aeroBlockSlingshot.nodeLineRenderType = "fan"

aeroBlockSlingshot.nodeLimits = {1, 2}

aeroBlockSlingshot.fieldInformation = {
    pushSpeed = {
        fieldType = "integer",
    },
    launchTime = {
        fieldType = "number",
    },
    cooldownTime = {
        fieldType = "number",
    },
    setTime = {
        fieldType = "number",
    },
    delayTime = {
        fieldType = "number",
    },
    startColor = {
        fieldType = "color",
    },
    endColor = {
        fieldType = "color",
    },
    pushActions = {
        fieldType = "string",
        editable = false,
        options = {
            "None",
            "Push",
            "Pull",
            "Both",
        }
    }
}

aeroBlockSlingshot.fieldOrder = {
    "x", "y", "width", "height",
    "launchTime", "cooldownTime", "setTime", "delayTime",
    "pushSpeed", "pushActions",
    "startColor", "endColor",
    "allowAdjustments",
}

aeroBlockSlingshot.placements = {
    {
        name = "normal",
        data = {
            width = 16,
            height = 16,
            launchTime = 0.5,
            cooldownTime = 0.5,
            setTime = 0.4,
            delayTime = 0.75,
            pushSpeed = 35,
            pushActions = "Push",
            allowAdjustments = true,
            startColor = "4BC0C8",
            endColor = "FEAC5E",
        },
    },
}

local backgroundColor = {20 / 255, 3 / 255, 3 / 255}
local outlineColor = {0, 0, 0}
local blockTexture = "objects/CommunalHelper/aero_block/blocks/nnn"

local blockNinePatchOptions = {
    mode = "border",
    borderMode = "repeat"
}

function aeroBlockSlingshot.sprite(room, entity)
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

function aeroBlockSlingshot.selection(room, entity)
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

return aeroBlockSlingshot
