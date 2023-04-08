local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local connectedEntities = require("helpers.connected_entities")

local connectedSolidExtension = {}

connectedSolidExtension.name = "CommunalHelper/SolidExtension"
connectedSolidExtension.depth = 0
connectedSolidExtension.minimumSize = {16, 16}

connectedSolidExtension.placements = {
    {
        name = "collidable",
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            collidable = true
        }
    },
    {
        name = "uncollidable",
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            collidable = false
        }
    }
}

local connectTo = {
    "CommunalHelper/SolidExtension",
    "CommunalHelper/ConnectedZipMover",
    "CommunalHelper/ConnectedSwapBlock",
    "CommunalHelper/ConnectedMoveBlock",
    "CommunalHelper/ConnectedTempleCrackedBlock",
    "CommunalHelper/EquationMoveBlock"
}

local frame = "objects/CommunalHelper/connectedZipMover/extension_outline"
local frameAlt = "objects/CommunalHelper/connectedZipMover/extension_outline_alt"

local function getSearchPredicate()
    return function(target)
        for _, name in ipairs(connectTo) do
            if target._name == name then
                return true
            end
        end
        return false
    end
end

local function getTileSprite(entity, x, y, tileset, rectangles, color)
    local hasAdjacent = connectedEntities.hasAdjacent

    local drawX, drawY = (x - 1) * 8, (y - 1) * 8

    local closedLeft = hasAdjacent(entity, drawX - 8, drawY, rectangles)
    local closedRight = hasAdjacent(entity, drawX + 8, drawY, rectangles)
    local closedUp = hasAdjacent(entity, drawX, drawY - 8, rectangles)
    local closedDown = hasAdjacent(entity, drawX, drawY + 8, rectangles)
    local completelyClosed = closedLeft and closedRight and closedUp and closedDown

    local quadX, quadY = nil, nil

    if completelyClosed then
        if not hasAdjacent(entity, drawX + 8, drawY - 8, rectangles) then
            quadX, quadY = 0, 16
            drawX = drawX + 7
            drawY = drawY - 7
        elseif not hasAdjacent(entity, drawX - 8, drawY - 8, rectangles) then
            quadX, quadY = 16, 16
            drawX = drawX - 7
            drawY = drawY - 7
        elseif not hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
            quadX, quadY = 0, 0
            drawX = drawX + 7
            drawY = drawY + 7
        elseif not hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
            quadX, quadY = 16, 0
            drawX = drawX - 7
            drawY = drawY + 7
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
        local sprite = drawableSprite.fromTexture(tileset, entity)

        sprite:addPosition(drawX, drawY)
        sprite:useRelativeQuad(quadX, quadY, 8, 8)
        sprite.color = color

        return sprite
    end
end

function connectedSolidExtension.sprite(room, entity)
    local sprites = {}

    local width, height = entity.width or 16, entity.height or 16
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local relevantBlocks = utils.filter(getSearchPredicate(), room.entities)
    connectedEntities.appendIfMissing(relevantBlocks, entity)
    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    local block = entity.collidable and frame or frameAlt
    local color = entity.collidable and {1.0, 1.0, 1.0, 1.0} or {0.6, 0.6, 0.6, 1.0}

    for x = 1, tileWidth do
        for y = 1, tileHeight do
            local sprite = getTileSprite(entity, x, y, block, rectangles, color)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end

    return sprites
end

return connectedSolidExtension
