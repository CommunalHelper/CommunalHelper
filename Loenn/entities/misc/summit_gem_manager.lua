local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")

local summitGemManager = {}

summitGemManager.name = "CommunalHelper/CustomSummitGemManager"
summitGemManager.depth = -1000000
summitGemManager.nodeLimits = {0, -1}
summitGemManager.nodeVisibility = "always"
summitGemManager.nodeLineRenderType = "fan"
summitGemManager.fieldInformation = {
    gemIds = {
        fieldType = "string",

        --[[ NOTE: the commented code below is incorrect, because this field is not always a list of integers.

        -- check if input is a comma-separated list of stricly positive integers
        validator = function(s)
            for sub in string.gmatch(s, "[^,]+") do
                if sub ~= string.match(sub, "%d+") then
                    return false
                end
                local n = tonumber(sub)
                if n == nil or n ~= math.floor(n) or n < 0 then
                    return false
                end
            end
            return true
        end

        ]]--
    },
    melody = {
        fieldType = "string",
        -- check if input is a comma-separated list of numbers
        validator = function(s)
            for sub in string.gmatch(s, "[^,]+") do
                if tonumber(sub) == nil then
                    return false
                end
            end
            return true
        end
    },
    heartOffset = {
        fieldType = "string",
        -- check if input is of form '$x,$y', where x and y are the coordinates of the heart offset vector
        validator = function(s)
            local i = 0
            for sub in string.gmatch(s, "[^,]+") do
                if tonumber(sub) == nil then
                    return false
                end
                i = i + 1
            end
            return i == 2
        end
    }
}

summitGemManager.placements = {
    {
        name = "summit_gem_manager",
        data = {
            gemIds = "",
            melody = "",
            heartOffset = "0.0,0.0"
        }
    }
}

local function getHeartOffset(x, y, s)
    local iter = string.gmatch(s, "[^,]+")
    return {x = x + tonumber(iter()), y = y + tonumber(iter())}
end

function summitGemManager.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local heartOffset = getHeartOffset(x, y, entity.heartOffset or "")
    return {
        drawableLine.fromPoints({x, y, heartOffset.x, heartOffset.y}),
        drawableSprite.fromTexture("objects/CommunalHelper/summitGemManager/circle", entity),
        drawableSprite.fromTexture("collectables/heartGem/ghost00", heartOffset)
    }
end

summitGemManager.nodeTexture = "collectables/summitgems/0/gem00"

function summitGemManager.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0

    local mainRectangle = utils.rectangle(x - 20, y - 20, 40, 40)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeRectangles = {}
    for _, node in ipairs(nodes) do
        table.insert(nodeRectangles, utils.rectangle(node.x - 10, node.y - 10, 20, 20))
    end

    -- (cursed) pseudo node for making it possible to 'move' heartOffset
    local heartOffset = getHeartOffset(x, y, entity.heartOffset or "")
    table.insert(nodeRectangles, utils.rectangle(heartOffset.x - 10, heartOffset.y - 10, 20, 20))

    return mainRectangle, nodeRectangles
end

function summitGemManager.onMove(room, entity, nodeIndex, offsetX, offsetY)
    local nodes = entity.nodes or {}
    local len = 0
    for _, _ in ipairs(nodes) do
        len = len + 1
    end

    if nodeIndex > len then
        -- we are moving the pseudo node
        local heartOffset = getHeartOffset(0, 0, entity.heartOffset or "")
        entity.heartOffset = string.format("%g,%g", heartOffset.x + offsetX, heartOffset.y + offsetY)
    end

    return true
end

-- prevents from adding node AFTER the pseudo node (which would cause a crash)
function summitGemManager.onNodeAdded(room, entity, nodeIndex)
    local nodes = entity.nodes or {}
    local len = 0
    for _, _ in ipairs(nodes) do
        len = len + 1
    end

    return nodeIndex <= len -- <=> is actual node
end

return summitGemManager
