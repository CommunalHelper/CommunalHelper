local drawableLine = require "structs.drawable_line"
local utils = require "utils"

local elytraBoostRing = {}

elytraBoostRing.name = "CommunalHelper/ElytraBoostRing"

elytraBoostRing.nodeLimits = {1, 1}
elytraBoostRing.nodeVisibility = "always"
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
        name = "elytra_boost_ring",
        data = {
            speed = 240.0,
            duration = 0.5,
            refill = false,
        }
    }
}

function elytraBoostRing.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    local line = drawableLine.fromPoints({x, y, nx, ny}, {0.0, 0.5, 0.5, 1.0})

    local mx, my = (x + nx) / 2, (y + ny) / 2
    local dx, dy = x - nx, y - ny
    local l = math.sqrt(dx * dx + dy * dy)
    local px, py = -dy / l, dx / l

    local midline = drawableLine.fromPoints({mx, my, mx + px * 8, my + py * 8}, {1.0, 0.0, 0.0, 1.0})

    table.insert(sprites, line)
    table.insert(sprites, midline)

    return sprites
end

function elytraBoostRing.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    return utils.rectangle(x - 4, y - 4, 8, 8), {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

return elytraBoostRing
