local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local enums = require("consts.celeste_enums")
local connectedEntities = require("helpers.connected_entities")

local connectedMoveBlock = {}

local moveSpeeds = {
    ["Slow"] = 60.0,
    ["Fast"] = 75.0
}

local arrowTextures = {
    up = "objects/moveBlock/arrow02",
    left = "objects/moveBlock/arrow04",
    right = "objects/moveBlock/arrow00",
    down = "objects/moveBlock/arrow06"
}

connectedMoveBlock.name = "CommunalHelper/ConnectedMoveBlock"
connectedMoveBlock.depth = -1
connectedMoveBlock.minimumSize = {16, 16}
connectedMoveBlock.fieldInformation = {
    direction = {
        options = enums.move_block_directions,
        editable = false
    },
    moveSpeed = {
        options = moveSpeeds,
        minimumValue = 0.0
    },
    idleColor = {
        fieldType = "color"
    },
    pressedColor = {
        fieldType = "color"
    },
    breakColor = {
        fieldType = "color"
    }
}

connectedMoveBlock.placements = {}
for i, direction in ipairs(enums.move_block_directions) do
    connectedMoveBlock.placements[i] = {
        name = string.lower(direction),
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            direction = direction,
            moveSpeed = 60.0,
            customBlockTexture = "",
            customSoundEffect = "",
            idleColor = "474070",
            pressedColor = "30b335",
            breakColor = "cc2541",
            outline = true
        }
    }
end
connectedMoveBlock.placements[5] = {
    name = "reskinnable",
    placementType = "rectangle",
    data = {
        width = 16,
        height = 16,
        direction = "Right",
        moveSpeed = 60.0,
        customBlockTexture = "CommunalHelper/customConnectedBlock/customConnectedBlock",
        customSoundEffect = "",
        idleColor = "474070",
        pressedColor = "30b335",
        breakColor = "cc2541",
        outline = true
    }
}
connectedMoveBlock.placements[6] = {
    name = "flag_controlled",
    placementType = "rectangle",
    data = {
        width = 16,
        height = 16,
        direction = "Right",
        moveSpeed = 60.0,
        customBlockTexture = "",
        customSoundEffect = "",
        idleColor = "474070",
        pressedColor = "30b335",
        breakColor = "cc2541",
        outline = true,
        activatorFlags = "_pressed",
        breakerFlags = "_obstructed",
        onActivateFlags = "",
        onBreakFlags = "",
        barrierBlocksFlags = false,
        waitForFlags = false
    }
}

local highlightColor = {59 / 255, 50 / 255, 101 / 255}

local function getSearchPredicate()
    return function(target)
        return target._name == "CommunalHelper/SolidExtension"
    end
end

local function getTileSprite(entity, x, y, block, inner, txo, rectangles)
    local hasAdjacent = connectedEntities.hasAdjacent

    local drawX, drawY = (x - 1) * 8, (y - 1) * 8

    local closedLeft = hasAdjacent(entity, drawX - 8, drawY, rectangles)
    local closedRight = hasAdjacent(entity, drawX + 8, drawY, rectangles)
    local closedUp = hasAdjacent(entity, drawX, drawY - 8, rectangles)
    local closedDown = hasAdjacent(entity, drawX, drawY + 8, rectangles)
    local completelyClosed = closedLeft and closedRight and closedUp and closedDown

    local quadX, quadY = false, false
    local frame = block

    if completelyClosed then
        frame = inner
        if not hasAdjacent(entity, drawX + 8, drawY - 8, rectangles) then
            quadX, quadY = 8 + txo, 0
        elseif not hasAdjacent(entity, drawX - 8, drawY - 8, rectangles) then
            quadX, quadY = 0 + txo, 0
        elseif not hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
            quadX, quadY = 8 + txo, 8
        elseif not hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
            quadX, quadY = 0 + txo, 8
        else
            quadX, quadY = 8, 8
            frame = block
        end
    else
        if closedLeft and closedRight and not closedUp and closedDown then
            quadX, quadY = 8, 0
        elseif closedLeft and closedRight and closedUp and not closedDown then
            quadX, quadY = 8, 16
        elseif closedLeft and not closedRight and closedUp and closedDown then
            quadX, quadY = 16, 8
        elseif not closedLeft and closedRight and closedUp and closedDown then
            quadX, quadY = 0, 8
        elseif closedLeft and not closedRight and not closedUp and closedDown then
            quadX, quadY = 16, 0
        elseif not closedLeft and closedRight and not closedUp and closedDown then
            quadX, quadY = 0, 0
        elseif not closedLeft and closedRight and closedUp and not closedDown then
            quadX, quadY = 0, 16
        elseif closedLeft and not closedRight and closedUp and not closedDown then
            quadX, quadY = 16, 16
        end
    end

    if quadX and quadY then
        local sprite = drawableSprite.fromTexture(frame, entity)

        sprite:addPosition(drawX, drawY)
        sprite:useRelativeQuad(quadX, quadY, 8, 8)

        return sprite
    end
end

local function getConnectedMoveBlockThemeData(entity)
    local customBlockTexture = entity.customBlockTexture or ""
    if customBlockTexture ~= "" then
        local full = "objects/" .. customBlockTexture
        return {
            block = full,
            inner = full,
            txOffset = 24
        }
    end

    return {
        block = "objects/moveBlock/base",
        inner = "objects/CommunalHelper/connectedMoveBlock/innerCorners",
        txOffset = 0
    }
end

function connectedMoveBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local sprites = {}

    local highlightRectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, highlightColor)
    table.insert(sprites, highlightRectangle:getDrawableSprite())

    local relevantBlocks = utils.filter(getSearchPredicate(), room.entities)
    connectedEntities.appendIfMissing(relevantBlocks, entity)
    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    local themeData = getConnectedMoveBlockThemeData(entity)

    for i = 1, tileWidth do
        for j = 1, tileHeight do
            local sprite = getTileSprite(entity, i, j, themeData.block, themeData.inner, themeData.txOffset, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end

    local direction = string.lower(entity.direction or "right")

    local arrowTexture = arrowTextures[direction]
    local arrowSprite = drawableSprite.fromTexture(arrowTexture, entity)
    arrowSprite:addPosition(math.floor(width / 2), math.floor(height / 2))

    local arrowSpriteWidth, arrowSpriteHeight = arrowSprite.meta.width, arrowSprite.meta.height
    local arrowX, arrowY = x + math.floor((width - arrowSpriteWidth) / 2), y + math.floor((height - arrowSpriteHeight) / 2)
    local arrowRectangle = drawableRectangle.fromRectangle("fill", arrowX, arrowY, arrowSpriteWidth, arrowSpriteHeight, highlightColor)

    table.insert(sprites, arrowRectangle:getDrawableSprite())
    table.insert(sprites, arrowSprite)

    return sprites
end

return connectedMoveBlock
