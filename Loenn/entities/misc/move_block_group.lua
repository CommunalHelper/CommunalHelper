local drawableLine = require("structs.drawable_line")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local moveBlockGroup = {}

moveBlockGroup.name = "CommunalHelper/MoveBlockGroup"
moveBlockGroup.depth = -100000

moveBlockGroup.nodeLimits = {2, -1}
moveBlockGroup.nodeLineRenderType = "fan"
moveBlockGroup.nodeVisibility = "always"

local defaultColorValue = "ffae11"

local respawnBehaviors = {
    "Immediate",
    "Simultaneous"
}

moveBlockGroup.fieldInformation = {
    color = {
        fieldType = "color"
    },
    respawnBehavior = {
        options = respawnBehaviors,
        editable = false,
    }
}

moveBlockGroup.placements = {
    {
        name = "move_block_group",
        data = {
            color = defaultColorValue,
            syncActivation = true,
            respawnBehavior = "Simultaneous",
        }
    }
}

local iconTexture = "objects/CommunalHelper/moveBlockGroup/icon"
local ringTexture = "objects/CommunalHelper/moveBlockGroup/ring"
local ringOutlineTexture = "objects/CommunalHelper/moveBlockGroup/ring_outline"

local lineColor = {0.5, 0.5, 0.5, 0.5}

function moveBlockGroup.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x + 16, y = y}, {x = x + 32, y = y}}

    local sprites = {}

    -- lines
    for _, node in ipairs(nodes) do
        local nx, ny = node.x or 0, node.y or 0

        local line = drawableLine.fromPoints({x, y, nx, ny}, lineColor)
        table.insert(sprites, line)
    end

    -- icon
    local icon = drawableSprite.fromTexture(iconTexture, entity)
    icon.color = {1.0, 1.0, 1.0, 1.0}

    table.insert(sprites, icon)

    return sprites
end

function moveBlockGroup.nodeSprite(room, entity, node, _, _)
    local sprites = {}

    local ring = drawableSprite.fromTexture(ringTexture, node)
    local outline = drawableSprite.fromTexture(ringOutlineTexture, node)

    local color = entity.color or defaultColorValue
    ring:setColor(color)

    table.insert(sprites, outline)
    table.insert(sprites, ring)

    return sprites
end

function moveBlockGroup.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x + 16, y = y}, {x = x + 32, y = y}}

    local rects = {}
    for _, node in ipairs(nodes) do
        local nx, ny = node.x or 0, node.y or 0
        table.insert(rects, utils.rectangle(nx - 4, ny - 4, 8, 8))
    end

    return utils.rectangle(x - 8, y - 8, 17, 17), rects
end

return moveBlockGroup
