local drawableLine = require("structs.drawable_line")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local utils = require("utils")
local connectedEntities = require("helpers.connected_entities")

local communalHelper = {}

-- utils

function communalHelper.hexToColor(hex, default)
    local success, r, g, b, a = utils.parseHexColor(hex)
    local color = default or {1.0, 1.0, 1.0, 1.0}
    if success then
        color = {r, g, b, a}
    end
    return color
end

-- cassette blocks

communalHelper.cassetteBlockColors = {
    {73 / 255, 170 / 255, 240 / 255},
    {240 / 255, 73 / 255, 190 / 255},
    {252 / 255, 220 / 255, 58 / 255},
    {56 / 255, 224 / 255, 78 / 255}
}

communalHelper.cassetteBlockColorNames = {
    ["0 - Blue"] = 0,
    ["1 - Rose"] = 1,
    ["2 - Bright Sun"] = 2,
    ["3 - Malachite"] = 3
}

communalHelper.cassetteBlockHexColors = {
    "49aaf0",
    "f049be",
    "fcdc3a",
    "38e04e"
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

    local quadX, quadY = -1, -1

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

    if quadX ~= -1 and quadY ~= -1 then
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
    return (entity.customColor ~= "" and communalHelper.hexToColor(entity.customColor)) or communalHelper.cassetteBlockColors[index + 1] or communalHelper.cassetteBlockColors[1]
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

local function addNodeSprites(sprites, cogTexture, cogColor, ropeColor, centerX, centerY, centerNodeX, centerNodeY, depth)
    local nodeCogSprite = drawableSprite.fromTexture(cogTexture)
    nodeCogSprite:setColor(cogColor)

    nodeCogSprite:setPosition(centerNodeX, centerNodeY)
    nodeCogSprite:setJustification(0.5, 0.5)

    local points = {centerX, centerY, centerNodeX, centerNodeY}
    local leftLine = drawableLine.fromPoints(points, ropeColor, 1)
    local rightLine = drawableLine.fromPoints(points, ropeColor, 1)

    leftLine:setOffset(0, 4.5)
    rightLine:setOffset(0, -4.5)

    leftLine.depth = depth
    rightLine.depth = depth

    for _, sprite in ipairs(leftLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    for _, sprite in ipairs(rightLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, nodeCogSprite)
end

function communalHelper.getZipMoverNodeSprites(x, y, width, height, nodes, cogTexture, cogColor, ropeColor, pathDepth)
    local sprites = {}

    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)
    local centerX, centerY = x + halfWidth, y + halfHeight

    local depth = pathDepth or 5000

    local cx, cy = centerX, centerY
    for _, node in ipairs(nodes) do
        local centerNodeX, centerNodeY = node.x + halfWidth, node.y + halfHeight
        addNodeSprites(sprites, cogTexture, cogColor, ropeColor, cx, cy, centerNodeX, centerNodeY, depth)
        cx, cy = centerNodeX, centerNodeY
    end

    return sprites
end

-- swap block

local trailNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    useRealSize = true
}

function communalHelper.getTrailSprites(x, y, nodeX, nodeY, width, height, trailTexture, trailColor, trailDepth)
    local sprites = {}

    local drawWidth, drawHeight = math.abs(x - nodeX) + width, math.abs(y - nodeY) + height
    x, y = math.min(x, nodeX), math.min(y, nodeY)

    local frameNinePatch = drawableNinePatch.fromTexture(trailTexture, trailNinePatchOptions, x, y, drawWidth, drawHeight)
    local frameSprites = frameNinePatch:getDrawableSprite()

    local depth = trailDepth or 8999
    local color = trailColor or {1, 1, 1, 1}
    for _, sprite in ipairs(frameSprites) do
        sprite.depth = depth
        sprite:setColor(color)

        table.insert(sprites, sprite)
    end

    return sprites
end

function communalHelper.addTrailSprites(sprites, x, y, nodeX, nodeY, width, height, trailTexture, trailColor, trailDepth)
    for _, sprite in ipairs(communalHelper.getTrailSprites(x, y, nodeX, nodeY, width, height, trailTexture, trailColor, trailDepth)) do
        table.insert(sprites, sprite)
    end
end

-- dream blocks

local featherColor = {0, 0.5, 0.5}
local oneUseColor = {178 / 255, 34 / 255, 34 / 255}

function communalHelper.getCustomDreamBlockSprites(x, y, width, height, feather, oneUse)
    local fill = feather and featherColor or {0, 0, 0}
    local border = oneUse and oneUseColor or {1, 1, 1}

    local rectangleSprite = drawableRectangle.fromRectangle("bordered", x, y, width, height, fill, border)
    rectangleSprite.depth = 0

    return rectangleSprite
end

function communalHelper.getCustomDreamBlockSpritesByEntity(entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local feather = entity.featherMode
    local oneUse = entity.oneUse
    return communalHelper.getCustomDreamBlockSprites(x, y, width, height, feather, oneUse)
end

-- panels

communalHelper.panelDirectionsOmitDown = {
    "Up",
    "Left",
    "Right"
}

communalHelper.panelDirections = {
    "Up",
    "Down",
    "Left",
    "Right"
}

function communalHelper.getPanelSprite(entity, color)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8

    local orientation = entity.orientation or "Up"
    local down = orientation == "Down"
    local left = orientation == "Left"
    local right = orientation == "Right"
    local vertical = left or right

    local rect = drawableRectangle.fromRectangle("fill", x, y, vertical and 8 or width, vertical and height or 8, color or {1.0, 0.5, 0.0, 0.4}):getDrawableSprite()
    local border = drawableRectangle.fromRectangle("fill", right and (x + 7) or x, down and (y + 7) or y, vertical and 1 or width, vertical and height or 1):getDrawableSprite()

    return {rect, border}
end

function communalHelper.fixAndGetPanelRectangle(entity)
    local orientation = entity.orientation or "Up"
    local left = orientation == "Left"
    local right = orientation == "Right"
    local vertical = left or right

    if vertical then
        entity.width = 8
    else
        entity.height = 8
    end

    return utils.rectangle(entity.x or 0, entity.y or 0, entity.width or 8, entity.height or 8)
end

-- custom curves (cubic bézier)

function communalHelper.getCubicCurvePoint(start, stop, controlA, controlB, t)
    local t2 = t * t
    local t3 = t2 * t
    local mt = 1 - t
    local mt2 = mt * mt
    local mt3 = mt2 * mt

    local aMul = 3 * mt2 * t
    local bMul = 3 * mt * t2

    local x = mt3 * start[1] + aMul * controlA[1] + bMul * controlB[1] + t3 * stop[1]
    local y = mt3 * start[2] + aMul * controlA[2] + bMul * controlB[2] + t3 * stop[2]

    return x, y
end

function communalHelper.getCubicCurve(start, stop, controlA, controlB, resolution)
    resolution = resolution or 16

    local res = {}

    for i = 0, resolution do
        local x, y = communalHelper.getCubicCurvePoint(start, stop, controlA, controlB, i / resolution)

        table.insert(res, x)
        table.insert(res, y)
    end

    return res
end

-- dream boosters

communalHelper.dreamBoosterPathStyles = {
    ["Arrows"] = "Arrow",
    ["Perpendicular lines"] = "Line",
    ["Dotted line"] = "DottedLine",
    ["Points"] = "Point"
}

-- lerp triggers

communalHelper.lerpDirections = {
    "TopToBottom",
    "BottomToTop",
    "LeftToRight",
    "RightToLeft"
}

-- easers

communalHelper.easers = {
    ["Linear"] = "Linear",
    ["Sine In"] = "SineIn",
    ["Sine Out"] = "SineOut",
    ["Sine In Out"] = "SineInOut",
    ["Quad In"] = "QuadIn",
    ["Quad Out"] = "QuadOut",
    ["Quad In Out"] = "QuadInOut",
    ["Cube In"] = "CubeIn",
    ["Cube Out"] = "CubeOut",
    ["Cube In Out"] = "CubeInOut",
    ["Quint In"] = "QuintIn",
    ["Quint Out"] = "QuintOut",
    ["Quint In Out"] = "QuintInOut",
    ["Expo In"] = "ExpoIn",
    ["Expo Out"] = "ExpoOut",
    ["Expo In Out"] = "ExpoInOut",
    ["Back In"] = "BackIn",
    ["Back Out"] = "BackOut",
    ["Back In Out"] = "BackInOut",
    ["BigBack In"] = "BigBackIn",
    ["BigBack Out"] = "BigBackOut",
    ["BigBack In Out"] = "BigBackInOut",
    ["Elastic In"] = "ElasticIn",
    ["Elastic Out"] = "ElasticOut",
    ["Elastic In Out"] = "ElasticInOut",
    ["Bounce In"] = "BounceIn",
    ["Bounce Out"] = "BounceOut",
    ["Bounce In Out"] = "BounceInOut",
}

return communalHelper
