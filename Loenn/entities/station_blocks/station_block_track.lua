local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local stationBlockTrack = {}

local switchStates = {"None", "On", "Off"}
local moveModes = {"None", "ForwardOneWay", "BackwardOneWay", "ForwardForce", "BackwardForce"}

stationBlockTrack.name = "CommunalHelper/StationBlockTrack"
stationBlockTrack.depth = -5000
stationBlockTrack.minimumSize = {24, 24}
stationBlockTrack.canResize = {true, true}
stationBlockTrack.fieldInformation = {
    indicatorColor = {
        fieldType = "color"
    },
    indicatorIncomingColor = {
        fieldType = "color"
    }
}

stationBlockTrack.placements = {}
for i, state in ipairs(switchStates) do
    stationBlockTrack.placements[2 * i - 1] = {
        name = "horiz_" .. string.lower(state),
        placementType = "rectangle",
        data = {
            width = 24,
            height = 8,
            horizontal = true,
            trackSwitchState = state,
            moveMode = "None",
            multiBlockTrack = false,
            indicator = true,
            indicatorColor = "008080",
            indicatorIncomingColor = "c92828"
        }
    }
    stationBlockTrack.placements[2 * i] = {
        name = "verti_" .. string.lower(state),
        placementType = "rectangle",
        data = {
            width = 8,
            height = 24,
            horizontal = false,
            trackSwitchState = state,
            moveMode = "None",
            multiBlockTrack = false,
            indicator = true,
            indicatorColor = "008080",
            indicatorIncomingColor = "c92828"
        }
    }
end

local noneColor = {1.0, 1.0, 1.0, 1.0}
local onColor = {66 / 255, 167 / 255, 1.0, 1.0}
local offColor = {1.0, 48 / 255, 131 / 255, 1.0}

local nodesTexture = "objects/CommunalHelper/stationBlock/tracks/outline/node"
local hTrackTexture = "objects/CommunalHelper/stationBlock/tracks/outline/h"
local vTrackTexture = "objects/CommunalHelper/stationBlock/tracks/outline/v"
local arrowsTexture = "objects/CommunalHelper/stationBlock/tracks/outline/arrows"

local function addMoveModeIndicatorSprites(sprites, entity, width, height, moveMode, ty, ox, oy, color)
    local force = moveMode == "backwardforce" or moveMode == "forwardforce"
    local backward = moveMode == "backwardoneway" or moveMode == "backwardforce"

    local stop = force and 1 or 0
    for i = 0, stop do
        local offset = 3 * i * (backward and -1 or 1)

        local arrowSprite = drawableSprite.fromTexture(arrowsTexture, entity)
        arrowSprite:setJustification(0.0, 0.0)
        arrowSprite:useRelativeQuad(backward and 0 or 8, ty, 8, 8)
        arrowSprite:addPosition(ox * ((backward and -6 or (width - 2)) + offset), oy * ((backward and -6 or (height - 2)) + offset))
        arrowSprite:setColor(color)

        table.insert(sprites, arrowSprite)
    end
end 

local function addTrackSprites(sprites, entity, horiz, color, width, height, ty, ox, oy)
    local nodeFromSprite = drawableSprite.fromTexture(nodesTexture, entity)
    nodeFromSprite:setColor(color)
    nodeFromSprite:setJustification(0.0, 0.0)
    nodeFromSprite:useRelativeQuad(0, ty, 8, 8)

    local nodeToSprite = drawableSprite.fromTexture(nodesTexture, entity)
    nodeToSprite:addPosition(ox * (width - 8), oy * (height - 8))
    nodeToSprite:setColor(color)
    nodeToSprite:setJustification(0.0, 0.0)
    nodeToSprite:useRelativeQuad(8, ty, 8, 8)

    table.insert(sprites, nodeFromSprite)
    table.insert(sprites, nodeToSprite)
    
    local size = math.floor((horiz and width or height) / 8)
    local trackTexture = horiz and hTrackTexture or vTrackTexture

    for i = 1, size - 2 do
        local trackSprite = drawableSprite.fromTexture(trackTexture, entity)
        trackSprite:setColor(color)
        trackSprite:setJustification(0.0, 0.0)
        trackSprite:addPosition(ox * i * 8, oy * i * 8)

        table.insert(sprites, trackSprite)
    end
end

function stationBlockTrack.sprite(room, entity)
    local sprites = {}

    local width, height = entity.width or 24, entity.height or 24
    local horiz = entity.horizontal
    local ox, oy = horiz and 1 or 0, horiz and 0 or 1
    local ty = horiz and 0 or 8

    local state = string.lower(entity.trackSwitchState or "None")
    local color = ((state == "on") and onColor) or ((state == "off") and offColor) or noneColor

    local moveMode = string.lower(entity.moveMode or "None")

    if moveMode ~= "none" then
        addMoveModeIndicatorSprites(sprites, entity, width, height, moveMode, ty, ox, oy, color)
    end

    addTrackSprites(sprites, entity, horiz, color, width, height, ty, ox, oy)

    return sprites
end

function stationBlockTrack.rectangle(room, entity)
    if entity.horizontal then
        return utils.rectangle(entity.x, entity.y, entity.width or 24, 8)
    end
    return utils.rectangle(entity.x, entity.y, 8, entity.height or 24)
end

return stationBlockTrack
