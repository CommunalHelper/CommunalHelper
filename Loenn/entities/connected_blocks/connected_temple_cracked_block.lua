local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local enums = require("consts.celeste_enums")
local connectedEntities = require("helpers.connected_entities")

local connectedTempleCrackedBlock = {}

connectedTempleCrackedBlock.name = "CommunalHelper/ConnectedTempleCrackedBlock"
connectedTempleCrackedBlock.depth = -1
connectedTempleCrackedBlock.minimumSize = {16, 16}
connectedTempleCrackedBlock.fieldInformation = {
    persistent = {
        fieldType = "boolean"
    }
}

connectedTempleCrackedBlock.placements = {}
connectedTempleCrackedBlock.placements[1] = {
    name = "normal",
    placementType = "rectangle",
    data = {
        width = 16,
        height = 16,
        persistent = false
    }
}
connectedTempleCrackedBlock.placements[2] = {
    name = "persistent",
    placementType = "rectangle",
    data = {
        width = 16,
        height = 16,
        persistent = true
    }
}

local highlightColor = {255 / 255, 255 / 255, 255 / 255}

local function getSearchPredicate()
    return function(target)
        return target._name == "CommunalHelper/SolidExtension"
    end
end

local function getTileSprite(entity, x, y, texturePath, rectangles)
    local hasAdjacent = connectedEntities.hasAdjacent

    local drawX, drawY = (x - 1) * 8, (y - 1) * 8

    local closedLeft = hasAdjacent(entity, drawX - 8, drawY, rectangles)
    local closedRight = hasAdjacent(entity, drawX + 8, drawY, rectangles)
    local closedUp = hasAdjacent(entity, drawX, drawY - 8, rectangles)
    local closedDown = hasAdjacent(entity, drawX, drawY + 8, rectangles)
    local completelyClosed = closedLeft and closedRight and closedUp and closedDown

    local quadX, quadY = false, false
    local isInnerCorner = false

    if completelyClosed then
        if not hasAdjacent(entity, drawX - 8, drawY - 8, rectangles) then
            if hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
                quadX, quadY = 6, 1
                isInnerCorner = true
            else
                quadX, quadY = 6, 4
                isInnerCorner = true
            end
        elseif not hasAdjacent(entity, drawX + 8, drawY - 8, rectangles) then
            if hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
                quadX, quadY = 6, 0
                isInnerCorner = true
            else
                quadX, quadY = 6, 5
                isInnerCorner = true
            end
        elseif not hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
            quadX, quadY = 6, 2
            isInnerCorner = true
        elseif not hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
            quadX, quadY = 6, 3
            isInnerCorner = true
        end
    end
    if not isInnerCorner then
        if not closedLeft then
            quadX = 0
        elseif not closedRight then
            quadX = 5
        elseif not hasAdjacent(entity, drawX - 16, drawY, rectangles) then
            quadX = 1
        elseif not hasAdjacent(entity, drawX + 16, drawY, rectangles) then
            quadX = 4
        else
            quadX = 2 + math.fmod(drawX / 8, 2)
        end
        if not closedUp then
            quadY = 0
        elseif not closedDown then
            quadY = 5
        elseif not hasAdjacent(entity, drawX, drawY - 16, rectangles) then
            quadY = 1
        elseif not hasAdjacent(entity, drawX, drawY + 16, rectangles) then
            quadY = 4
        else
            quadY = 2 + math.fmod(drawY / 8, 2)
        end
    end

    if quadX and quadY then
        local sprite = drawableSprite.fromTexture(texturePath, entity)

        sprite:addPosition(drawX, drawY)
        sprite:useRelativeQuad(quadX * 8, quadY * 8, 8, 8)

        return sprite
    end
end

local function getConnectedTempleCrackedBlockTexture(entity)
    return "objects/CommunalHelper/connectedTempleCrackedBlock/breakBlock00"
end

function connectedTempleCrackedBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local sprites = {}

    local highlightRectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, highlightColor)
    table.insert(sprites, highlightRectangle:getDrawableSprite())

    local relevantBlocks = utils.filter(getSearchPredicate(), room.entities)
    connectedEntities.appendIfMissing(relevantBlocks, entity)
    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    local texturePath = getConnectedTempleCrackedBlockTexture(entity)

    for i = 1, tileWidth do
        for j = 1, tileHeight do
            local sprite = getTileSprite(entity, i, j, texturePath, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end

    return sprites
end

return connectedTempleCrackedBlock
