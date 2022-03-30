local utils = require("utils")

local dreamBooster = {}

dreamBooster.name = "CommunalHelper/DreamBooster"
dreamBooster.depth = -11000
dreamBooster.nodeLimits = {1, 1}
dreamBooster.nodeLineRenderType = "line"

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

local texture = "objects/CommunalHelper/boosters/dreamBooster/idle00"

dreamBooster.texture = texture
dreamBooster.nodeTexture = texture

function dreamBooster.selection(room, entity)
    local rect = utils.rectangle(entity.x - 9, entity.y - 9, 18, 18)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nx, ny = nodes[1].x, nodes[1].y
    local nodeRect = utils.rectangle(nx - 9, ny - 9, 18, 18)

    return rect, {nodeRect}
end

return dreamBooster
