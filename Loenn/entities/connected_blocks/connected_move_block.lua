local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local enums = require("consts.celeste_enums")
local connectedEntities = require("helpers.connected_entities")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local connectedMoveBlock = {}

local moveSpeeds = {
    ["Slow"] = 60.0,
    ["Fast"] = 75.0
}

local arrowIndices = {
    up = "02",
    left = "04",
    right = "00",
    down = "06"
}

connectedMoveBlock.name = "CommunalHelper/ConnectedMoveBlock"
connectedMoveBlock.depth = -1
connectedMoveBlock.minimumSize = { 16, 16 }
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
    },
    activatorFlags = {
        fieldType = "list",
        elementDefault = "",
        elementSeparator = "|",
        elementOptions = {
            fieldType = "list",
            elementDefault = "",
            elementSeparator = ",",
            elementOptions = {
                fieldType = "string"
            }
        }
    },
    breakerFlags = {
        fieldType = "list",
        elementDefault = "",
        elementSeparator = "|",
        elementOptions = {
            fieldType = "list",
            elementDefault = "",
            elementSeparator = ",",
            elementOptions = {
                fieldType = "string"
            }
        }
    },
    onActivateFlags = {
        fieldType = "list",
        elementDefault = "",
        elementOptions = {
            fieldType = "string"
        }
    },
    onBreakFlags = {
        fieldType = "list",
        elementDefault = "",
        elementOptions = {
            fieldType = "string"
        }
    },
    crashTime = {
        minimumValue = 0.0
    },
    regenTime = {
        minimumValue = 0.0
    },
    ignore = {
        fieldType = "list",
        elementDefault = "",
        elementOptions = {
            fieldType = "string",
            options = function() return communalHelper.getMapSIDs() end,
            editable = true,
            searchable = true
        }
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
            idleColor = "474070",
            pressedColor = "30b335",
            breakColor = "cc2541",
            customSkin = "",
            customSoundEffect = "",
            noArrowSprite = false,
            noBreakingSprite = false,
            noDebris = false,
            outline = true,
            activatorFlags = "_pressed",
            breakerFlags = "_obstructed",
            onActivateFlags = "",
            onBreakFlags = "",
            barrierBlocksFlags = false,
            waitForFlags = false,
            crashTime = 0.15,
            regenTime = 3.0,
            shakeOnCollision = true,
            redirectIsPersistent = false,
            ignore = ""
        }
    }
end

local function getSearchPredicate()
    return function(target)
        return target._name == "CommunalHelper/SolidExtension"
    end
end

local function getTileSprite(entity, x, y, tileset, rectangles)
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
            quadX, quadY = 32, 0
        elseif not hasAdjacent(entity, drawX - 8, drawY - 8, rectangles) then
            quadX, quadY = 24, 0
        elseif not hasAdjacent(entity, drawX + 8, drawY + 8, rectangles) then
            quadX, quadY = 32, 8
        elseif not hasAdjacent(entity, drawX - 8, drawY + 8, rectangles) then
            quadX, quadY = 24, 8
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

        return sprite
    end
end

local function getConnectedMoveBlockThemeData(entity)
    local default = {
        tileset = "objects/CommunalHelper/connectedMoveBlock/tileset",
        arrows = "objects/CommunalHelper/connectedMoveBlock/arrow"
    }

    local customSkin = entity.customSkin or ""
    if customSkin == "" then return default end

    local tilesetPath = customSkin .. "/tileset"
    local arrowPath = customSkin .. "/arrow"
    return {
        tileset = tilesetPath,
        arrows = arrowPath
    }
end

function connectedMoveBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local tileWidth, tileHeight = math.ceil(width / 8), math.ceil(height / 8)

    local sprites = {}

    local highlightColor = utils.getColor(entity.pressedColor or { 59 / 255, 50 / 255, 101 / 255 })
    local highlightRectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, highlightColor)
    table.insert(sprites, highlightRectangle:getDrawableSprite())

    local relevantBlocks = utils.filter(getSearchPredicate(), room.entities)
    connectedEntities.appendIfMissing(relevantBlocks, entity)
    local rectangles = connectedEntities.getEntityRectangles(relevantBlocks)

    local themeData = getConnectedMoveBlockThemeData(entity)

    for i = 1, tileWidth do
        for j = 1, tileHeight do
            local sprite = getTileSprite(entity, i, j, themeData.tileset, rectangles)

            if sprite then
                table.insert(sprites, sprite)
            end
        end
    end

    if not (entity.noArrowSprite or false) then
        local direction = string.lower(entity.direction or "right")

        local arrowTexture = themeData.arrows .. arrowIndices[direction]
        local arrowSprite = drawableSprite.fromTexture(arrowTexture, entity)
        arrowSprite:addPosition(math.floor(width / 2), math.floor(height / 2))

        local arrowSpriteWidth, arrowSpriteHeight = arrowSprite.meta.width, arrowSprite.meta.height
        local arrowX, arrowY = x + math.floor((width - arrowSpriteWidth) / 2), y + math.floor((height - arrowSpriteHeight) / 2)
        local arrowRectangle = drawableRectangle.fromRectangle("fill", arrowX, arrowY, arrowSpriteWidth, arrowSpriteHeight, highlightColor)

        table.insert(sprites, arrowRectangle:getDrawableSprite())
        table.insert(sprites, arrowSprite)
    end

    return sprites
end

return connectedMoveBlock
