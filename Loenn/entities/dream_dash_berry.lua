local utils = require("utils")

local dreamDashBerry = {}

dreamDashBerry.name = "CommunalHelper/DreamStrawberry"
dreamDashBerry.depth = -100
dreamDashBerry.nodeLineRenderType = "fan"
dreamDashBerry.nodeLimits = {0, -1}

dreamDashBerry.fieldInformation = {
    order = {
        fieldType = "integer"
    },
    checkpointID = {
        fieldType = "integer"
    }
}

dreamDashBerry.placements = {
    name = "dream_dash_berry",
    data = {
        order = -1,
        checkpointID = -1
    }
}

dreamDashBerry.texture = "collectables/CommunalHelper/dreamberry/wings01"
dreamDashBerry.nodeTexture = "collectables/CommunalHelper/dreamberry/seed02"

function dreamDashBerry.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}}

    local rects = {}
    for _, node in ipairs(nodes) do
        local nx, ny = node.x or 0, node.y or 0
        table.insert(rects, utils.rectangle(nx - 4, ny - 4, 7, 9))
    end

    return utils.rectangle(x - 6, y - 7, 12, 13), rects
end

return dreamDashBerry
