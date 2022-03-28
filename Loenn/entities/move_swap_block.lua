local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local utils = require("utils")
local enums = require("consts.celeste_enums")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local moveSwapBlock = {}

moveSwapBlock.name = "CommunalHelper/MoveSwapBlock"
moveSwapBlock.minimumSize = {16, 16}
moveSwapBlock.nodeLimits = {1, 1}
moveSwapBlock.fieldInformation = {
    theme = {
        editable = false,
        --options = enums.swap_block_themes -- Move Swap Blocks don't have the 'Moon' theme
    },
    direction = {
        editable = false,
        options = enums.move_block_directions
    }
}

moveSwapBlock.placements = {
    {
        name = "move_swap_block",
        data = {
            width = 16,
            height = 16,
            theme = "Normal",
            direction = "Left",
            canSteer = false,
            returns = true,
            freezeOnSwap = true,
            moveSpeed = 60.0,
            moveAcceleration = 300.0,
            swapSpeedMultiplier = 1.0,
        }
    }
}

local themeTextures = {
    normal = {
        frame = "objects/swapblock/blockRed",
        trail = "objects/swapblock/target",
        middle = "objects/CommunalHelper/moveSwapBlock/midBlockCardinal",
        gem = "objects/CommunalHelper/moveSwapBlock/midBlockOrange",
        path = true
    },
    moon = {
        frame = "objects/swapblock/moon/blockRed",
        trail = "objects/swapblock/moon/target",
        middle = "objects/CommunalHelper/moveSwapBlock/midBlockCardinal",
        gem = "objects/CommunalHelper/moveSwapBlock/midBlockOrange",
        path = false
    }
}

local middleSpriteData = {
    ["Up"] = {rot = 0, ox = 0, oy = -1},
    ["Right"] = {rot = math.pi / 2, ox = 1, oy = 0},
    ["Down"] = {rot = math.pi, ox = 0, oy = 1},
    ["Left"] = {rot = math.pi / 2 * 3, ox = -1, oy = 0},
}

local nodeFrameColor = {1.0, 1.0, 1.0, 0.7}

local frameNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat"
}

local frameNodeNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    color = nodeFrameColor
}

local blockDepth = -9999

local function addBlockSprites(sprites, entity, position, frameTexture, middleTexture, gemTexture, isNode)
    local x, y = position.x or 0, position.y or 0
    local width, height = entity.width or 8, entity.height or 8

    local ninePatchOptions = isNode and frameNodeNinePatchOptions or frameNinePatchOptions
    local frameNinePatch = drawableNinePatch.fromTexture(frameTexture, ninePatchOptions, x, y, width, height)
    local frameSprites = frameNinePatch:getDrawableSprite()

    local direction = entity.direction or "Right"
    local middleData = middleSpriteData[direction]

    local arrowSprite = drawableSprite.fromTexture(middleTexture, {rotation = middleData.rot})
    arrowSprite:addPosition(x + math.floor(width / 2), y + math.floor(height / 2))
    arrowSprite.depth = blockDepth

    local gemSprite = drawableSprite.fromTexture(gemTexture, position)
    gemSprite:addPosition(math.floor(width / 2) + middleData.ox, math.floor(height / 2) + middleData.oy)
    gemSprite.depth = blockDepth - 1

    if isNode then
        arrowSprite:setColor(nodeFrameColor)
    end

    for _, sprite in ipairs(frameSprites) do
        sprite.depth = blockDepth

        table.insert(sprites, sprite)
    end

    table.insert(sprites, arrowSprite)
    table.insert(sprites, gemSprite)
end

function moveSwapBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8

    local sprites = {}

    local theme = string.lower(entity.theme or "normal")
    local themeData = themeTextures[theme] or themeTextures["normal"]

    communalHelper.addTrailSprites(sprites, x, y, nodeX, nodeY, width, height, themeData.trail)
    addBlockSprites(sprites, entity, entity, themeData.frame, themeData.middle, themeData.gem)

    return sprites
end

function moveSwapBlock.nodeSprite(room, entity, node)
    local sprites = {}

    local theme = string.lower(entity.theme or "normal")
    local themeData = themeTextures[theme] or themeTextures["normal"]

    addBlockSprites(sprites, entity, node, themeData.frame, themeData.middle, themeData.gem, true)

    return sprites
end

function moveSwapBlock.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return moveSwapBlock
