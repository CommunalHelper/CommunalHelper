local utils = require("utils")

local function createHandler(name)
    handler = {}
    
    handler.name = name
    handler.depth = -100
    handler.nodeLineRenderType = "fan"
    handler.nodeLimits = {0, -1}

    handler.fieldInformation = {
        order = {
            fieldType = "integer"
        },
        checkpointID = {
            fieldType = "integer"
        }
    }

    handler.placements = {
        name = "dream_dash_berry",
        data = {
            order = -1,
            checkpointID = -1
        }
    }

    handler.texture = "collectables/CommunalHelper/dreamberry/wings01"
    handler.nodeTexture = "collectables/CommunalHelper/dreamberry/seed02"

    function handler.selection(room, entity)
        local x, y = entity.x or 0, entity.y or 0
        local nodes = entity.nodes or {{x = 0, y = 0}}

        local rects = {}
        for _, node in ipairs(nodes) do
            local nx, ny = node.x or 0, node.y or 0
            table.insert(rects, utils.rectangle(nx - 4, ny - 4, 7, 9))
        end

        return utils.rectangle(x - 6, y - 7, 12, 13), rects
    end

    return handler
end

return {
    createHandler("CommunalHelper/DreamStrawberry"),
    createHandler("CommunalHelper/DreamStrawberryTracked")
}
