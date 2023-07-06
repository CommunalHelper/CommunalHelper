local drawableLine = require "structs.drawable_line"
local utils = require "utils"

local elytraStopRing = {}

elytraStopRing.name = "CommunalHelper/ElytraStopRing"

elytraStopRing.nodeLimits = {1, 1}
elytraStopRing.nodeVisibility = "always"
elytraStopRing.nodeLineRenderType = "line"

elytraStopRing.placements = {
    {
        name = "elytra_stop_ring",
        data = {
            refill = false,
        }
    }
}

function elytraStopRing.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    local line = drawableLine.fromPoints({x, y, nx, ny}, {1.0, 0.0, 0.0, 1.0})
    table.insert(sprites, line)

    return sprites
end

function elytraStopRing.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    return utils.rectangle(x - 4, y - 4, 8, 8), {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

return elytraStopRing
