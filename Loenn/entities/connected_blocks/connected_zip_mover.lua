local connectedEntities = require("helpers.connected_entities")
local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local connectedZipMover = {}

local themes = {
    "Normal", "Moon", "Cliffside"
}

connectedZipMover.name = "CommunalHelper/ConnectedZipMover"
connectedZipMover.depth = 4999
connectedZipMover.minimumSize = {16, 16}
connectedZipMover.nodeVisibility = "never"
connectedZipMover.nodeLimits = {1, -1}
connectedZipMover.fieldInformation = {
    theme = {
        editable = false,
        options = themes
    }
}

connectedZipMover.placements = {}
for i, theme in ipairs(themes) do
    connectedZipMover.placements[i] = {
        name = string.lower(theme),
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            theme = theme,
            permanent = false,
            waiting = false,
            ticking = false,
            customSkin = "",
            colors = ""
        }
    }
end

local function getSearchPredicate()
    return function(target)
        return target._name == "CommunalHelper/SolidExtension"
    end
end

local function getTileSprite(entity, x, y, block, inner, rectangles)
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
            quadX, quadY = 8, 0
        elseif not hasAdjacent(entity, drawX - 8, drawY - 8, rectangles) then
            quadX, quadY = 0, 0
        elseif not hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
            quadX, quadY = 8, 8
        elseif not hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
            quadX, quadY = 0, 8
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

local function getConnectedZipMoverThemeData(entity)
    local theme = string.lower(entity.theme or "normal")
    local cliffside = theme == "cliffside"
    local folder = cliffside and "CommunalHelper/connectedZipMover" or "zipmover"
    local themePath = (theme == "normal") and "" or (theme .. "/")
    
    local customSkin = entity. customSkin or ""
    if customSkin ~= "" then
        return {
            block = customSkin .. "/block",
            light = customSkin .. "/light01",
            cog = customSkin .. "/cog",
            inner = customSkin .. "/innerCorners"
        }
    end
    
    return {
        block = "objects/" .. folder .. "/" .. themePath .. "block",
        light = "objects/" .. folder .. "/" .. themePath .. "light01",
        cog = "objects/" .. folder .. "/" .. themePath .. "cog",
        inner = "objects/" .. ((cliffside and "" or "CommunalHelper/") .. folder) .. "/" .. themePath .. "innerCorners"
    }
end

local zipMoverRoleColor = {102 / 255, 57 / 255, 49 / 255}

function connectedZipMover.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local rectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, {0, 0, 0})
    local sprites = {rectangle:getDrawableSprite()}

    local themeData = getConnectedZipMoverThemeData(entity)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeSprites = communalHelper.getZipMoverNodeSprites(x, y, width, height, nodes, themeData.cog, {1, 1, 1}, zipMoverRoleColor)
    for _, sprite in ipairs(nodeSprites) do
        table.insert(sprites, sprite)
    end

    local relevantBlocks = utils.filter(getSearchPredicate(), room.entities)
    connectedEntities.appendIfMissing(relevantBlocks, entity)
    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    for i = 1, tileWidth do
        for j = 1, tileHeight do
            local sprite = getTileSprite(entity, i, j, themeData.block, themeData.inner, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end
    
    local lightsSprite = drawableSprite.fromTexture(themeData.light, entity)
    lightsSprite:addPosition(math.floor(width / 2), 0)
    lightsSprite:setJustification(0.5, 0.0)
    table.insert(sprites, lightsSprite)

    return sprites
end

function connectedZipMover.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local mainRectangle = utils.rectangle(x, y, width, height)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeRectangles = {}
    for _, node in ipairs(nodes) do
        local centerNodeX, centerNodeY = node.x + halfWidth, node.y + halfHeight

        table.insert(nodeRectangles, utils.rectangle(centerNodeX - 5, centerNodeY - 5, 10, 10))
    end

    return mainRectangle, nodeRectangles
end

return connectedZipMover
