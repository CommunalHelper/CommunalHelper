local drawableLine = require "structs.drawable_line"
local drawableSprite = require "structs.drawable_sprite"
local utils = require "utils"

local elytraBoostRing = {}

elytraBoostRing.name = "CommunalHelper/ElytraBoostRing"

elytraBoostRing.nodeLimits = {1, 1}
elytraBoostRing.nodeVisibility = "never"
elytraBoostRing.nodeLineRenderType = "line"

elytraBoostRing.fieldInformation = {
    speed = {
        fieldType = "number",
        minimumValue = 0.0,
    },
    duration = {
        fieldType = "number",
        minimumValue = 0.0,
    }
}

elytraBoostRing.placements = {
    {
        name = "unidirectional",
        data = {
            speed = 240.0,
            duration = 0.5,
            refill = false,
            bidirectional = false,
        }
    },
    {
        name = "bidirectional",
        data = {
            speed = 240.0,
            duration = 0.5,
            refill = false,
            bidirectional = true,
        }
    }
}

local arrowTexture = "objects/CommunalHelper/elytraRing/arrow"
local dotTexture = "objects/CommunalHelper/elytraRing/dot"

local arrowColor = {1.0, 0.1, 0.1, 0.5}
local ringColor = {0.1, 0.5, 0.5, 1.0}

function elytraBoostRing.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    local line = drawableLine.fromPoints({x, y, nx, ny}, ringColor)

    local mx, my = (x + nx) / 2, (y + ny) / 2
    local dx, dy = ny - y, x - nx -- perpendicular (-y, x)
    local angle = math.atan(dy / dx) + (dx >= 0 and 0 or math.pi) + math.pi / 4

    local length = math.sqrt(dx * dx + dy * dy)
    local norx, nory = dx / length, dy / length

    local function addIcon(texture, atx, aty, scale, rot, color)
        local icon = drawableSprite.fromTexture(texture, {x = atx, y = aty})
        icon.rotation = rot + angle
        icon:setJustification(0.5, 0.5)
        icon:setScale(scale, scale)
        icon.color = color
        table.insert(sprites, icon)
    end

    addIcon(arrowTexture, mx + norx * 4, my + nory * 4, 4, 0, arrowColor)
    if entity.bidirectional then
        addIcon(arrowTexture, mx - norx * 4, my - nory * 4, 4, math.pi, arrowColor)
    end

    table.insert(sprites, line)

    addIcon(dotTexture, x, y, 2, 0, ringColor)
    addIcon(dotTexture, nx, ny, 2, 0, ringColor)

    return sprites
end

function elytraBoostRing.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    return utils.rectangle(x - 4, y - 4, 8, 8), {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

return elytraBoostRing
