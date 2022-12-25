local drawableLine = require("structs.drawable_line")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local heldBooster = {}

heldBooster.name = "CommunalHelper/HeldBooster"
heldBooster.depth = -8500
heldBooster.nodeLimits = {0, 1}
heldBooster.nodeVisibility = "always"

heldBooster.placements = {
    {
        name = "purple",
        data = {
            hidePath = false
        }
    },
    {
        name = "green",
        placementType = "line",
        data = {
            hidePath = false,
            nodes = {{x = 0, y = 0}} -- this is hacky
        }
    }
}

local function isGreen(entity)
    local nodes = entity.nodes or {}
    return #nodes == 1
end

function heldBooster.ignoredFields(entity)
    if isGreen(entity) then
        return {"_name", "_id", "green"}
    else
        return {"_name", "_id", "green", "hidePath", "nodes"}
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
        drawableLine.fromPoints({x, y, nx, ny}, entity.hidePath and hiddenPathColor or {1, 1, 1}),
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
