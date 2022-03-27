local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dreamSwapBlock = {}

dreamSwapBlock.name = "CommunalHelper/DreamSwapBlock"
dreamSwapBlock.nodeLimits = {1, 1}
dreamSwapBlock.minimumSize = {16, 16}

function dreamSwapBlock.depth(room, entity)
    return entity.below and 5000 or -11000
end

dreamSwapBlock.placements = {
    {
        name = "dream_swap_block",
        data = {
            width = 16,
            height = 16,
            featherMode = false,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
            noReturn = false,
        }
    }
}

local trailNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    useRealSize = true
}
local trailDepth = 8999

local function addTrailSprites(sprites, x, y, nodeX, nodeY, width, height, trailTexture)
    local drawWidth, drawHeight = math.abs(x - nodeX) + width, math.abs(y - nodeY) + height

    x, y = math.min(x, nodeX), math.min(y, nodeY)

    local frameNinePatch = drawableNinePatch.fromTexture(trailTexture, trailNinePatchOptions, x, y, drawWidth, drawHeight)
    local frameSprites = frameNinePatch:getDrawableSprite()

    for _, sprite in ipairs(frameSprites) do
        sprite.depth = trailDepth

        table.insert(sprites, sprite)
    end
end

local function addBlockSprites(sprites, x, y, width, height, noReturn, feather)
    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)
    local centerX, centerY = x + halfWidth, y + halfHeight

    table.insert(sprites, communalHelper.getCustomDreamBlockSprites(x, y, width, height, feather))
    
    if noReturn then
        local cross = drawableSprite.fromTexture("objects/CommunalHelper/dreamMoveBlock/x")
        cross:setPosition(centerX, centerY)
        cross.depth = -1

        table.insert(sprites, cross)
    end
end

function dreamSwapBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local noReturn = entity.noReturn
    local feather = entity.featherMode

    local sprites = {}

    addTrailSprites(sprites, x, y, nodeX, nodeY, width, height, "objects/swapblock/target")
    addBlockSprites(sprites, x, y, width, height, noReturn, feather)

    return sprites
end

function dreamSwapBlock.nodeSprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local noReturn = entity.noReturn
    local feather = entity.featherMode

    local sprites = {}

    addBlockSprites(sprites, nodeX, nodeY, width, height, noReturn, feather)

    return sprites
end

function dreamSwapBlock.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 16, entity.height or 16

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return dreamSwapBlock
