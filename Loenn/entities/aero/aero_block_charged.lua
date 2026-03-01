local drawableNinePatch = require "structs.drawable_nine_patch"
local drawableRectangle = require "structs.drawable_rectangle"
local utils = require "utils"
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local cassetteIndexOptions = {
    ["None"] = -1,
    ["0 - Blue"] = 0,
    ["1 - Rose"] = 1,
    ["2 - Bright Sun"] = 2,
    ["3 - Malachite"] = 3
}
local cassetteIndexColors = communalHelper.cassetteBlockHexColors

local aeroBlockCharged = {}

aeroBlockCharged.name = "CommunalHelper/AeroBlockCharged"
aeroBlockCharged.depth = 4999
aeroBlockCharged.minimumSize = { 16, 16 }

aeroBlockCharged.nodeLimits = { 0, -1 }
aeroBlockCharged.nodeLineRenderType = "line"

aeroBlockCharged.fieldInformation = {
    activeColor = {
        fieldType = "color"
    },
    inactiveColor = {
        fieldType = "color"
    },
    cassetteIndex = {
        options = cassetteIndexOptions,
        editable = false,
        fieldType = "integer",
    },
    cassetteTempo = {
        minimumValue = 0
    }
}

aeroBlockCharged.placements = {
    {
        name = "aero_block_charged",
        data = {
            width = 16,
            height = 16,
            buttonSequence = "left -> right+top",
            hover = true,
            loop = false,
            activeColor = "4BC0C8",
            inactiveColor = "FF6347",
            -- SpirialisBug = false,
            SpirialisBugV2 = false,
            wallbounceLeniency = true,
            cassetteIndex = -1,
            cassetteTempo = 1,
            moveOnCassetteTick = false
        }
    }
}
for i = 0, 3 do
    table.insert(aeroBlockCharged.placements, {
        name = string.format("aero_block_charged_%s", i),
        data = {
            width = 16,
            height = 16,
            buttonSequence = "left -> right+top",
            hover = true,
            loop = false,
            activeColor = cassetteIndexColors[i + 1],
            inactiveColor = "FF6347",
            -- SpirialisBug = false,
            SpirialisBugV2 = false,
            wallbounceLeniency = true,
            cassetteIndex = i,
            cassetteTempo = 1,
            moveOnCassetteTick = false
        }
    })
end

local backgroundColor = { 20 / 255, 3 / 255, 3 / 255 }
local outlineColor = { 0, 0, 0 }
local blockTexture = "objects/CommunalHelper/aero_block/blocks/nnn"

local blockNinePatchOptions = {
    mode = "border",
    borderMode = "repeat"
}

function aeroBlockCharged.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16

    local outline = drawableRectangle.fromRectangle("fill", x - 1, y - 1, width + 2, height + 2, backgroundColor)
    local rectangle = drawableRectangle.fromRectangle("fill", x, y, width, height, outlineColor)

    table.insert(sprites, outline:getDrawableSprite())
    table.insert(sprites, rectangle:getDrawableSprite())

    if entity.cassetteIndex and entity.cassetteIndex ~= -1 then
        local cassetteIndicatorColor = entity.activeColor ~= "" and communalHelper.hexToColor(entity.activeColor) or cassetteIndexColors[entity.cassetteIndex + 1]
        local cassetteIndicator = drawableRectangle.fromRectangle("fill", x + 4, y + 4, width - 8, height - 8, cassetteIndicatorColor)
        table.insert(sprites, cassetteIndicator:getDrawableSprite())
    end

    local frameNinePatch = drawableNinePatch.fromTexture(blockTexture, blockNinePatchOptions, x, y, width, height)
    local frameSprites = frameNinePatch:getDrawableSprite()

    for _, sprite in ipairs(frameSprites) do
        table.insert(sprites, sprite)
    end

    return sprites
end

function aeroBlockCharged.selection(room, entity)
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

return aeroBlockCharged