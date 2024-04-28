local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local cassetteSwapBlock = {}

local colorNames = communalHelper.cassetteBlockColorNames
local colors = communalHelper.cassetteBlockHexColors

cassetteSwapBlock.name = "CommunalHelper/CassetteSwapBlock"
cassetteSwapBlock.minimumSize = { 16, 16 }
cassetteSwapBlock.nodeLimits = { 1, 1 }
cassetteSwapBlock.fieldInformation = {
    index = {
        options = colorNames,
        editable = false,
        fieldType = "integer"
    },
    customColor = {
        fieldType = "color"
    },
    tempo = {
        minimumValue = 0.0
    }
}

cassetteSwapBlock.placements = {}
for i = 1, 4 do
    cassetteSwapBlock.placements[i] = {
        name = string.format("cassette_block_%s", i - 1),
        data = {
            index = i - 1,
            tempo = 1.0,
            width = 16,
            height = 16,
            customColor = colors[i],
            noReturn = false,
            oldConnectionBehavior = false,
        }
    }
end

local function getBlockSprites(room, entity)
    local sprites = communalHelper.getCustomCassetteBlockSprites(room, entity, true, entity.oldConnectionBehavior)

    local color = communalHelper.getCustomCassetteBlockColor(entity)

    if entity.noReturn then
        local cross = drawableSprite.fromTexture("objects/CommunalHelper/cassetteMoveBlock/x", entity)
        cross:addPosition(math.floor(entity.width / 2), math.floor(entity.height / 2))
        cross:setColor(color)
        cross.depth = -11

        table.insert(sprites, cross)
    end

    return sprites
end

function cassetteSwapBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8

    local sprites = getBlockSprites(room, entity)

    local color = communalHelper.getCustomCassetteBlockColor(entity)
    communalHelper.addTrailSprites(sprites, x, y, nodeX, nodeY, width, height, "objects/swapblock/target", color)

    return sprites
end

function cassetteSwapBlock.nodeSprite(room, entity)
    local sprites = getBlockSprites(room, entity)

    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y

    for _, sprite in ipairs(sprites) do
        sprite:addPosition(nodeX - x, nodeY - y)
    end

    return sprites
end

function cassetteSwapBlock.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8

    return utils.rectangle(x, y, width, height), { utils.rectangle(nodeX, nodeY, width, height) }
end

return cassetteSwapBlock
