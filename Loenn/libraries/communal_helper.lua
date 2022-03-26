local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawableRectangle = require("structs.drawable_rectangle")
local connectedEntities = require("helpers.connected_entities")
local utils = require("utils")

local communalHelper = {}

-- utils

function communalHelper.hexToColor(hex)
    local success, r, g, b, a = utils.parseHexColor(hex)
    local color = {1.0, 1.0, 1.0, 1.0}
    if success then
        color = {r, g, b, a}
    end
    return color
end

-- cassette blocks

local cassetteBlockColors = {
    {73 / 255, 170 / 255, 240 / 255},
    {240 / 255, 73 / 255, 190 / 255},
    {252 / 255, 220 / 255, 58 / 255},
    {56 / 255, 224 / 255, 78 / 255},
}

local function getSearchPredicate(entity)
    return function(target)
        return entity._name == target._name and entity.index == target.index
    end
end

local function getTileSprite(entity, x, y, frame, color, depth, rectangles)
    local hasAdjacent = connectedEntities.hasAdjacent

    local drawX, drawY = (x - 1) * 8, (y - 1) * 8

    local closedLeft = hasAdjacent(entity, drawX - 8, drawY, rectangles)
    local closedRight = hasAdjacent(entity, drawX + 8, drawY, rectangles)
    local closedUp = hasAdjacent(entity, drawX, drawY - 8, rectangles)
    local closedDown = hasAdjacent(entity, drawX, drawY + 8, rectangles)
    local completelyClosed = closedLeft and closedRight and closedUp and closedDown

    local quadX, quadY = false, false

    if completelyClosed then
        if not hasAdjacent(entity, drawX + 8, drawY - 8, rectangles) then
            quadX, quadY = 24, 0

        elseif not hasAdjacent(entity, drawX - 8, drawY - 8, rectangles) then
            quadX, quadY = 24, 8

        elseif not hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
            quadX, quadY = 24, 16

        elseif not hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
            quadX, quadY = 24, 24

        else
            quadX, quadY = 8, 8
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
        sprite:setColor(color)

        sprite.depth = depth

        return sprite
    end
end

function communalHelper.getCustomCassetteBlockColor(entity)
    local index = entity.index or 0
    return (entity.customColor ~= "" and communalHelper.hexToColor(entity.customColor)) or cassetteBlockColors[index + 1] or cassetteBlockColors[1]
end

function communalHelper.getCustomCassetteBlockSprites(room, entity)
    local relevantBlocks = utils.filter(getSearchPredicate(entity), room.entities)

    connectedEntities.appendIfMissing(relevantBlocks, entity)

    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    local sprites = {}

    local width, height = entity.width or 32, entity.height or 32
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local color = communalHelper.getCustomCassetteBlockColor(entity)
    local frame = "objects/cassetteblock/solid"
    local depth = -10

    for x = 1, tileWidth do
        for y = 1, tileHeight do
            local sprite = getTileSprite(entity, x, y, frame, color, depth, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end

    return sprites
end

-- zip movers

local function addNodeSprites(sprites, cogTexture, cogColor, ropeColor, centerX, centerY, centerNodeX, centerNodeY)
    local nodeCogSprite = drawableSprite.fromTexture(cogTexture)
    nodeCogSprite:setColor(cogColor)

    nodeCogSprite:setPosition(centerNodeX, centerNodeY)
    nodeCogSprite:setJustification(0.5, 0.5)

    local points = {centerX, centerY, centerNodeX, centerNodeY}
    local leftLine = drawableLine.fromPoints(points, ropeColor, 1)
    local rightLine = drawableLine.fromPoints(points, ropeColor, 1)

    leftLine:setOffset(0, 4.5)
    rightLine:setOffset(0, -4.5)

    leftLine.depth = 5000
    rightLine.depth = 5000

    for _, sprite in ipairs(leftLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    for _, sprite in ipairs(rightLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, nodeCogSprite)
end

function communalHelper.getZipMoverNodeSprites(x, y, width, height, nodes, cogTexture, cogColor, ropeColor)
    local sprites = {}

    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)
    local centerX, centerY = x + halfWidth, y + halfHeight

    local cx, cy = centerX, centerY
    for _, node in ipairs(nodes) do
        local centerNodeX, centerNodeY = node.x + halfWidth, node.y + halfHeight
        addNodeSprites(sprites, cogTexture, cogColor, ropeColor, cx, cy, centerNodeX, centerNodeY)
        cx, cy = centerNodeX, centerNodeY
    end

    return sprites
end

-- dream blocks

function communalHelper.getCustomDreamBlockSprites(x, y, width, height, feather)
    local fill = feather and {0, 0.5, 0.5} or {0, 0, 0}

    local rectangleSprite = drawableRectangle.fromRectangle("bordered", x, y, width, height, fill, {1, 1, 1})
    rectangleSprite.depth = 0

    return rectangleSprite
end

function communalHelper.getCustomDreamBlockSpritesByEntity(entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local feather = entity.featherMode
    return communalHelper.getCustomDreamBlockSprites(x, y, width, height, feather)
end


return communalHelper
