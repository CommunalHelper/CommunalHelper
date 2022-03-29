local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local railedMoveBlock = {}

local steeringModes = {
    "Horizontal", "Vertical", "Both"
}

railedMoveBlock.name = "CommunalHelper/RailedMoveBlock"
railedMoveBlock.depth = 8995
railedMoveBlock.minimumSize = {16, 16}
railedMoveBlock.nodeLimits = {1, 1}
railedMoveBlock.nodeVisibility = "never"
railedMoveBlock.fieldInformation = {
    steeringMode = {
        options = steeringModes,
        editable = false
    }
}

railedMoveBlock.placements = {}
for i, steeringMode in ipairs(steeringModes) do
    railedMoveBlock.placements[i] = {
        name = string.lower(steeringMode),
        data = {
            width = 16,
            height = 16,
            steeringMode = steeringMode,
            speed = 120.0,
        }
    }
end

local ninePatchOptions = {
    mode = "border",
    borderMode = "repeat"
}

local midColor = {4 / 255, 3 / 255, 23 / 255}
local highlightColor = {59 / 255, 50 / 255, 101 / 255}
local buttonColor = {71 / 255, 64 / 255, 112 / 255}

local buttonTexture = "objects/moveBlock/button"

local arrowTextures = {
    horizontal = "objects/CommunalHelper/railedMoveBlock/h",
    vertical = "objects/CommunalHelper/railedMoveBlock/v",
    both = "objects/CommunalHelper/railedMoveBlock/both",
}
local steeringFrameTextures = {
    horizontal = "objects/moveBlock/base_v",
    vertical = "objects/moveBlock/base_h",
    both = "objects/CommunalHelper/railedMoveBlock/base_both",
}

-- How far the button peeks out of the block and offset to keep it in the "socket"
local buttonPopout = 3
local buttonOffset = 3

function railedMoveBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    local nodes = entity.nodes or {{x = 0, y = 0}}

    local steeringMode = string.lower(entity.steeringMode or "horizontal")
    local both = steeringMode == "both"
    local horizontal = steeringMode == "horizontal" or both
    local vertical = steeringMode == "vertical" or both

    local blockTexture = steeringFrameTextures[steeringMode]
    local arrowTexture = arrowTextures[steeringMode]

    local ninePatch = drawableNinePatch.fromTexture(blockTexture, ninePatchOptions, x, y, width, height)

    local highlightRectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, highlightColor)
    local midRectangle = drawableRectangle.fromRectangle("fill", x + 8, y + 8, width - 16, height - 16, midColor)

    local arrowSprite = drawableSprite.fromTexture(arrowTexture, entity)
    local arrowSpriteWidth, arrowSpriteHeight = arrowSprite.meta.width, arrowSprite.meta.height
    local arrowX, arrowY = x + math.floor((width - arrowSpriteWidth) / 2), y + math.floor((height - arrowSpriteHeight) / 2)
    local arrowRectangle = drawableRectangle.fromRectangle("fill", arrowX, arrowY, arrowSpriteWidth, arrowSpriteHeight, highlightColor)

    arrowSprite:addPosition(math.floor(width / 2), math.floor(height / 2))

    local sprites = communalHelper.getZipMoverNodeSprites(x, y, width, height, nodes, "objects/zipmover/cog", {1, 1, 1}, midColor, 8995)

    table.insert(sprites, highlightRectangle:getDrawableSprite())
    table.insert(sprites, midRectangle:getDrawableSprite())

    if horizontal then
        for oy = 4, height - 4, 8 do
            local leftQuadX = (oy == 4 and 16 or (oy == height - 4 and 0 or 8))
            local rightQuadX = (oy == 4 and 0 or (oy == height - 4 and 16 or 8))
            local spriteLeft = drawableSprite.fromTexture(buttonTexture, entity)
            local spriteRight = drawableSprite.fromTexture(buttonTexture, entity)

            spriteLeft.rotation = -math.pi / 2
            spriteLeft:addPosition(-buttonPopout, oy + buttonOffset)
            spriteLeft:useRelativeQuad(leftQuadX, 0, 8, 8)
            spriteLeft:setColor(buttonColor)

            spriteRight.rotation = math.pi / 2
            spriteRight:addPosition(width + buttonPopout, oy - buttonOffset)
            spriteRight:useRelativeQuad(rightQuadX, 0, 8, 8)
            spriteRight:setColor(buttonColor)

            table.insert(sprites, spriteLeft)
            table.insert(sprites, spriteRight)
        end
    end

    if vertical then
        for ox = 4, width - 4, 8 do
            local quadX = (ox == 4 and 0 or (ox == width - 4 and 16 or 8))
            local sprite = drawableSprite.fromTexture(buttonTexture, entity)

            sprite:addPosition(ox - buttonOffset, -buttonPopout)
            sprite:useRelativeQuad(quadX, 0, 8, 8)
            sprite:setColor(buttonColor)

            table.insert(sprites, sprite)
        end
    end

    for _, sprite in ipairs(ninePatch:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, arrowRectangle:getDrawableSprite())
    table.insert(sprites, arrowSprite)

    return sprites
end

function railedMoveBlock.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local mainRectangle = utils.rectangle(x, y, width, height)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nx, ny = nodes[1].x, nodes[1].y

    return mainRectangle, {utils.rectangle(nx + halfWidth - 5, ny + halfHeight - 5, 10, 10)}
end

return railedMoveBlock
