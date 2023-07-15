local drawableLine = require "structs.drawable_line"
local drawableSprite = require "structs.drawable_sprite"
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

local dotTexture = "objects/CommunalHelper/elytraRing/dot"
local ringColor = {0.2, 0.75, 0.2, 1.0}
local dotColor = {0.3, 0.8, 0.3, 0.5}

function elytraRefillRing.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    local line = drawableLine.fromPoints({x, y, nx, ny}, ringColor)

    local mx, my = (x + nx) / 2, (y + ny) / 2
    local dx, dy = ny - y, x - nx -- perpendicular (-y, x)
    local angle = math.atan(dy / dx) + (dx >= 0 and 0 or math.pi) + math.pi / 4

    local function addIcon(texture, atx, aty, scale, rot, color)
        local icon = drawableSprite.fromTexture(texture, {x = atx, y = aty})
        icon.rotation = rot + angle
        icon:setJustification(0.5, 0.5)
        icon:setScale(scale, scale)
        icon.color = color
        table.insert(sprites, icon)
    end

    addIcon(dotTexture, mx, my, 6, 0, dotColor)

    table.insert(sprites, line)

    addIcon(dotTexture, x, y, 2, 0, ringColor)
    addIcon(dotTexture, nx, ny, 2, 0, ringColor)

    return sprites
end

function elytraRefillRing.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    return utils.rectangle(x - 4, y - 4, 8, 8), {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

return elytraRefillRing
