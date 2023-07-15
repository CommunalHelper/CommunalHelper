local drawableLine = require "structs.drawable_line"
local drawableSprite = require "structs.drawable_sprite"
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

local dotTexture = "objects/CommunalHelper/elytraRing/dot"
local xTexture = "objects/CommunalHelper/elytraRing/x"

local ringColor = {1.0, 0.2, 0.2, 1.0}
local xColor = {1.0, 0.4, 0.4, 0.5}

function elytraStopRing.sprite(room, entity)
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

    addIcon(xTexture, mx, my, 2, 0, xColor)

    table.insert(sprites, line)

    addIcon(dotTexture, x, y, 2, 0, ringColor)
    addIcon(dotTexture, nx, ny, 2, 0, ringColor)

    return sprites
end
function elytraStopRing.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    return utils.rectangle(x - 4, y - 4, 8, 8), {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

return elytraStopRing
