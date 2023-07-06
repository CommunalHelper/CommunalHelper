local drawableLine = require "structs.drawable_line"
local utils = require "utils"

local elytraRefillRing = {}

elytraRefillRing.name = "CommunalHelper/ElytraRefillRing"

elytraRefillRing.nodeLimits = {1, 1}
elytraRefillRing.nodeVisibility = "always"
elytraRefillRing.nodeLineRenderType = "line"

elytraRefillRing.placements = {
    {
        name = "elytra_refill_ring",
        data = {}
    }
}

function elytraRefillRing.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    local line = drawableLine.fromPoints({x, y, nx, ny}, {0.1, 0.9, 0.2, 1.0})
    table.insert(sprites, line)

    return sprites
end

function elytraRefillRing.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    return utils.rectangle(x - 4, y - 4, 8, 8), {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

return elytraRefillRing
