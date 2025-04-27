local utils = require("utils")

local orientationNames = { "Up", "Right", "Down", "Left" }
local orientationIndices = { Up = 0, Right = 1, Down = 2, Left = 3 }
local sprite_path = "objects/CommunalHelper/strawberryJam/pelletEmitter"

local function createPlacement(orientationIndex, cassetteIndex)
    local orientationName = orientationNames[orientationIndex + 1]
    local suffix = cassetteIndex < 0 and "both" or cassetteIndex
    return {
        name = string.lower(orientationName).."_"..suffix,
        data = {
            x = 0,
            y = 0,
            orientation = orientationName,
            cassetteIndex = cassetteIndex,
            killPlayer = true,
            collideWithSolids = true,
            pelletSpeed = 100,
            pelletCount = 1,
            pelletDelay = 0.25,
            wiggleAmount = 2,
            wiggleFrequency = 2,
            wiggleHitbox = false,
        },
    }
end

local function createPlacements()
    local placements = { }
    for orientation = 0,3 do
        for cassetteIndex = -1,1 do
            table.insert(placements, createPlacement(orientation, cassetteIndex))
        end
    end
    return placements
end

local pelletEmitter = {
    name = "CommunalHelper/SJ/PelletEmitter",
    depth = -8500,
    placements = createPlacements(),
    justification = { 0, 0.5 },
    fieldInformation = {
        orientation = {
            editable = false,
            options = orientationNames,
        },
        cassetteIndex = {
            editable = false,
            options = {
                -- we only support blue and pink for now since that's the only sprites we have
                ["Blue"] = 0,
                ["Rose"] = 1,
                ["Both"] = -1,
            },
        },
        pelletSpeed = {
            fieldType = "number"
        },
        pelletCount = {
            fieldType = "integer",
            minimumValue = 1
        },
        pelletDelay = {
            fieldType = "number",
            minimumValue = 0
        },
        wiggleAmount = {
            fieldType = "number"
        },
        wiggleFrequency = {
            fieldType = "number",
            minimumValue = 0
        }
    }
}

function pelletEmitter.texture(room, entity)
    if entity.cassetteIndex == 0 then
        return sprite_path.."/blue/emitter00"
    end
    if entity.cassetteIndex == 1 then
        return sprite_path.."/pink/emitter00"
    end
    return sprite_path.."/both/emitter00"
end

function pelletEmitter.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local size = 16
    local orientationIndex = orientationIndices[entity.orientation]
    
    if orientationIndex == 0 then
        x = x - size / 2
        y = y - size
    elseif orientationIndex == 1 then
        y = y - size / 2
    elseif orientationIndex == 2 then
        x = x - size / 2
    else
        x = x - size
        y = y - size / 2
    end
    
    return utils.rectangle(x, y, size, size)
end

function pelletEmitter.rotation(room, entity)
    local orientationIndex = orientationIndices[entity.orientation]
    return (orientationIndex - 1) * math.pi / 2
end

function pelletEmitter.flip(room, entity, horizontal, vertical)
    if horizontal and entity.orientation == "Left" then
        entity.orientation = "Right"
    elseif horizontal and entity.orientation == "Right" then
        entity.orientation = "Left"
    elseif vertical and entity.orientation == "Up" then
        entity.orientation = "Down"
    elseif vertical and entity.orientation == "Down" then
        entity.orientation = "Up"
    else
        return false
    end
    return true
end

function pelletEmitter.rotate(room, entity, direction)
    local orientationIndex = orientationIndices[entity.orientation]
    entity.orientation = orientationNames[(orientationIndex + direction + 4) % 4 + 1]
    return true
end

return pelletEmitter
