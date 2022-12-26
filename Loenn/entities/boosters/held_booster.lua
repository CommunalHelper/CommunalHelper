local drawableLine = require("structs.drawable_line")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local heldBooster = {}

heldBooster.name = "CommunalHelper/HeldBooster"
heldBooster.depth = -8500
heldBooster.nodeLimits = {0, 1}
heldBooster.nodeVisibility = "always"

heldBooster.fieldInformation = {
    pathStyle = {
        options = communalHelper.dreamBoosterPathStyles,
        editable = false
    },
    speed = {
        minimumValue = 0.0
    }
}

heldBooster.placements = {
    {
        name = "purple",
        data = {
            pathStyle = "Arrow",
            speed = 240.0,
            deathTimer = -1.0,
            blinkSfx = true,
        }
    },
    {
        name = "purple_limited",
        data = {
            pathStyle = "Arrow",
            speed = 240.0,
            deathTimer = 3,
            blinkSfx = true,
        }
    },
    {
        name = "green",
        placementType = "line",
        data = {
            pathStyle = "Arrow",
            speed = 240.0,
            deathTimer = -1.0,
            blinkSfx = true,
            nodes = {{x = 0, y = 0}} -- this is hacky
        }
    },
    {
        name = "green_limited",
        placementType = "line",
        data = {
            pathStyle = "Arrow",
            speed = 240.0,
            deathTimer = 3,
            blinkSfx = true,
            nodes = {{x = 0, y = 0}} -- this is hacky
        }
    }
}

local function isGreen(entity)
    local nodes = entity.nodes or {}

    if #nodes == 0 then
        return false
    end

    local x, y = entity.x or 0, entity.y or 0
    local nx, ny = nodes[1].x, nodes[1].y

    return x ~= nx or y ~= ny
end

function heldBooster.ignoredFields(entity)
    if isGreen(entity) then
        return {"_name", "_id", "green", "nodes"}
    else
        return {"_name", "_id", "green", "pathStyle", "nodes"}
    end
end

local purpleTexture = "objects/CommunalHelper/boosters/heldBooster/purple/booster00"
local greenTexture = "objects/CommunalHelper/boosters/heldBooster/green/booster00"
local hiddenPathColor = {1.0, 1.0, 1.0, 0.25}

function heldBooster.sprite(room, entity)
    if not isGreen(entity) then
        return drawableSprite.fromTexture(purpleTexture, entity)
    end

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nx, ny = nodes[1].x, nodes[1].y

    return {
        drawableLine.fromPoints({x, y, nx, ny}, {1, 1, 1}),
        drawableSprite.fromTexture(greenTexture, entity)
    }
end

heldBooster.nodeTexture = greenTexture
heldBooster.nodeColor = {1, 1, 1, 0.25}

function heldBooster.selection(room, entity)
    local rect = utils.rectangle(entity.x - 9, entity.y - 9, 18, 18)

    if not isGreen(entity) then
        return rect
    end

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nx, ny = nodes[1].x, nodes[1].y
    local nodeRect = utils.rectangle(nx - 9, ny - 9, 18, 18)

    return rect, {nodeRect}
end

return heldBooster
