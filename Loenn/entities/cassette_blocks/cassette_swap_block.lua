local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")
local utils = require("utils")

local cassetteSwapBlock = {}

cassetteSwapBlock.name = "CommunalHelper/CassetteSwapBlock"
cassetteSwapBlock.minimumSize = {16, 16}
cassetteSwapBlock.nodeLimits = {1, 1}
cassetteSwapBlock.fieldInformation = {
    index = {
        fieldType = "integer",
    },
    customColor = {
        fieldType = "color",
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
            customColor = "",
            noReturn = false
        }
    }
end

local trailNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    useRealSize = true
}

local function addTrailSprites(sprites, entity, trailTexture, color)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8
    local drawWidth, drawHeight = math.abs(x - nodeX) + width, math.abs(y - nodeY) + height

    x, y = math.min(x, nodeX), math.min(y, nodeY)

    local frameNinePatch = drawableNinePatch.fromTexture(trailTexture, trailNinePatchOptions, x, y, drawWidth, drawHeight)
    local frameSprites = frameNinePatch:getDrawableSprite()

    for _, sprite in ipairs(frameSprites) do
        sprite.depth = 8999
        sprite:setColor(color)

        table.insert(sprites, sprite)
    end
end


local function getBlockSprites(room, entity)
    local sprites = communalHelper.getCustomCassetteBlockSprites(room, entity)

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
    local sprites = getBlockSprites(room, entity)
    addTrailSprites(sprites, entity, "objects/swapblock/target", communalHelper.getCustomCassetteBlockColor(entity))
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

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return cassetteSwapBlock
