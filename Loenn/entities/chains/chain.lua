local drawing = require("utils.drawing")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")

local chain = {}

chain.name = "CommunalHelper/Chain"
chain.nodeLimits = {1, 1}
chain.fieldInformation = {
    extraJoints = {
        minimumValue = 0,
        fieldType = "integer"
    }
}

chain.placements = {
    name = "chain",
    data = {
        extraJoints = 0,
        outline = true,
        texture = "objects/CommunalHelper/chains/chain",
    }
}

function chain.sprite(room, entity)
    local firstNode = entity.nodes[1]

    local start = {entity.x, entity.y}
    local stop = {firstNode.x, firstNode.y}
    local control = {
        (start[1] + stop[1]) / 2,
        (start[2] + stop[2]) / 2 + 24
    }

    local points = drawing.getSimpleCurve(start, stop, control)

    return drawableLine.fromPoints(points, {0.0, 0.5, 0.5, 1.0}, 1)
end

function chain.selection(room, entity)
    local main = utils.rectangle(entity.x - 2, entity.y - 2, 5, 5)
    local nodes = {}

    if entity.nodes then
        for i, node in ipairs(entity.nodes) do
            nodes[i] = utils.rectangle(node.x - 2, node.y - 2, 5, 5)
        end
    end

    return main, nodes
end

return chain
