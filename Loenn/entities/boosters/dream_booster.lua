local utils = require("utils")
local drawableLine = require("structs.drawable_line")
local drawableSprite = require("structs.drawable_sprite")

local dreamBooster = {}

dreamBooster.name = "CommunalHelper/DreamBooster"
dreamBooster.depth = -11000
dreamBooster.nodeVisibility = "never"
dreamBooster.nodeLimits = {1, 1}

dreamBooster.placements = {
    {
        name = "normal",
        data = {
            hidePath = false
        }
    },
    {
        name = "hidden_path",
        data = {
            hidePath = true
        }
    }
}

dreamBooster.nodeLineRenderType = "line"
local lineColor = {1.0, 1.0, 1.0}

function dreamBooster.sprite(room, entity)
    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nx, ny = nodes[1].x, nodes[1].y

    local line = drawableLine.fromPoints({entity.x, entity.y, nx, ny}, lineColor, 1)
    local from = drawableSprite.fromTexture("objects/CommunalHelper/boosters/dreamBooster/idle00", entity)
    local to = drawableSprite.fromTexture("objects/CommunalHelper/boosters/dreamBooster/idle00", {x = nx, y = ny, color = {1.0, 1.0, 1.0, 0.5}})

    return {line, from, to}
end

function dreamBooster.selection(room, entity)
    local rect = utils.rectangle(entity.x - 9, entity.y - 9, 18, 18)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nx, ny = nodes[1].x, nodes[1].y
    local nodeRect = utils.rectangle(nx - 9, ny - 9, 18, 18)

    return rect, {nodeRect}
end

return dreamBooster
