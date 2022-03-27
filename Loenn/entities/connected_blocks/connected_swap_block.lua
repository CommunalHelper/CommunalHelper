local connectedEntities = require("helpers.connected_entities")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local enums = require("consts.celeste_enums")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local connectedSwapBlock = {}

connectedSwapBlock.name = "CommunalHelper/ConnectedSwapBlock"
connectedSwapBlock.depth = -9999
connectedSwapBlock.minimumSize = {16, 16}
connectedSwapBlock.nodeLimits = {1, 1}
connectedSwapBlock.fieldInformation = {
    theme = {
        editable = false,
        options = enums.swap_block_themes
    }
}

connectedSwapBlock.placements = {
    {
        name = "normal",
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            theme = "Normal",
            customGreenBlockTexture = "",
            customRedBlockTexture = "",
        }
    },
    {
        name = "reskinnable",
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            theme = "Normal",
            customGreenBlockTexture = "CommunalHelper/customConnectedBlock/customConnectedBlock",
            customRedBlockTexture = "CommunalHelper/customConnectedBlock/customConnectedBlock",
        }
    }
}

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

local function getConnectedSwapBlockThemeData(entity)
    local theme = string.lower(entity.theme or "normal")
    local themePath = (theme == "normal") and "" or (theme .. "/")

    local path = "objects/swapblock/" .. themePath .. "target"
    local mid = "objects/swapblock/" .. themePath .. "midBlockRed00"

    local customBlockTexture = entity.customRedBlockTexture or ""
    if customBlockTexture ~= "" then
        local full = "objects/" .. customBlockTexture
        return {
            block = full,
            inner = full,
            path = path,
            mid = mid,
            txOffset = 24,
        }
    end

    return {
        block = "objects/swapblock/" .. themePath .. "blockRed",
        inner = "objects/CommunalHelper/connectedSwapBlock/" .. themePath .. "innerCornersRed",
        path = path,
        mid = mid,
        txOffset = 0,
    }
end

local trailNinePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    useRealSize = true
}

local function addBlockSprites(sprites, room, entity, w, h, tw, th, themeData)
    local relevantBlocks = utils.filter(getSearchPredicate(), room.entities)
    connectedEntities.appendIfMissing(relevantBlocks, entity)
    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    for i = 1, tw do
        for j = 1, th do
            local sprite = getTileSprite(entity, i, j, themeData.block, themeData.inner, themeData.txOffset, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end

    local middleSprite = drawableSprite.fromTexture(themeData.mid, entity)
    middleSprite:addPosition(math.floor(w / 2), math.floor(h / 2))
    middleSprite.depth = -9999
    table.insert(sprites, middleSprite)
end

function connectedSwapBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local themeData = getConnectedSwapBlockThemeData(entity)

    local sprites = {}

    communalHelper.addTrailSprites(sprites, x, y, nodeX, nodeY, width, height, themeData.path)
    addBlockSprites(sprites, room, entity, width, height, tileWidth, tileHeight, themeData)

    return sprites
end

function connectedSwapBlock.nodeSprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local themeData = getConnectedSwapBlockThemeData(entity)

    local sprites = {}

    addBlockSprites(sprites, room, entity, width, height, tileWidth, tileHeight, themeData)

    for _, sprite in ipairs(sprites) do
        sprite:addPosition(nodeX - x, nodeY - y)
    end

    return sprites
end

function connectedSwapBlock.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 8, entity.height or 8

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return connectedSwapBlock
