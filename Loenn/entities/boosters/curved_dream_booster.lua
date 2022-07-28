local drawing = require("utils.drawing")
local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local curvedDreamBooster = {}

curvedDreamBooster.name = "CommunalHelper/CurvedDreamBooster"
curvedDreamBooster.depth = -11000
curvedDreamBooster.nodeVisibility = "always"
curvedDreamBooster.ignoredFields = {
    "curve",
    "_name",
    "_id"
}
curvedDreamBooster.fieldInformation = {
    pathStyle = {
        options = communalHelper.dreamBoosterPathStyles,
        editable = false
    }
}

function curvedDreamBooster.nodeLimits(room, entity)
    local min = entity.curve == "Cubic" and 3 or 2
    return min, -1
end

curvedDreamBooster.placements = {
    {
        name = "quadratic",
        data = {
            curve = "Quadratic",
            hidePath = false,
            pathStyle = "Arrow"
        }
    },
    {
        name = "quadratic_hidden_path",
        data = {
            curve = "Quadratic",
            hidePath = true,
            pathStyle = "Arrow"
        }
    },
    {
        name = "cubic",
        data = {
            curve = "Cubic",
            hidePath = false,
            pathStyle = "Arrow"
        }
    },
    {
        name = "cubic_hidden_path",
        data = {
            curve = "Cubic",
            hidePath = true,
            pathStyle = "Arrow"
        }
    }
}

local bubbleTexture = "objects/CommunalHelper/boosters/dreamBooster/idlecurve00"
local insideTexture = "objects/CommunalHelper/boosters/dreamBooster/inside01"
local extraNodeTexture = "objects/CommunalHelper/boosters/dreamBooster/x"
local controlNodeTexture = "objects/CommunalHelper/boosters/dreamBooster/o"
local info01Texture = "objects/CommunalHelper/boosters/dreamBooster/info01"
local info02Texture = "objects/CommunalHelper/boosters/dreamBooster/info02"

local controlLineColor = {1, 1, 1, 0.2}
local cubicControlLineColor = {0.5, 0.5, 0.5, 0.075}

local function getCurveTemplate(mode, start, nodes)
    local cubic = mode == "Cubic"
    local m = cubic and 3 or 2
    local n = #nodes
    local extraCount = n % m
    local count = n - extraCount

    local points = {start}
    for i = 1, count do
        table.insert(points, nodes[i])
    end

    local info = nil
    if extraCount == 1 then
        info = cubic and info02Texture or info01Texture
    elseif extraCount == 2 then
        info = info01Texture
    end

    return {
        subcurveCount = count / m,
        points = points,
        info = info
    }
end

local function getCurveSprites(entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x + 16, y = y}, {x = x + 32, y = y}}
    local mode = entity.curve or "Quadratic"
    local template = getCurveTemplate(mode, {x = x, y = y}, nodes)

    local sprites = {}

    if template.subcurveCount > 0 then
        if mode == "Quadratic" then
            for i = 0, template.subcurveCount - 1 do
                local a = template.points[1 + i * 2]
                local b = template.points[3 + i * 2]
                local c = template.points[2 + i * 2]

                local points = drawing.getSimpleCurve({a.x, a.y}, {b.x, b.y}, {c.x, c.y}, 24)
                table.insert(sprites, drawableLine.fromPoints(points))

                table.insert(sprites, drawableLine.fromPoints({a.x, a.y, c.x + 0.5, c.y + 0.5}, controlLineColor))
                table.insert(sprites, drawableLine.fromPoints({b.x, b.y, c.x + 0.5, c.y + 0.5}, controlLineColor))
            end
        elseif mode == "Cubic" then
            for i = 0, template.subcurveCount - 1 do
                local a = template.points[1 + i * 3]
                local ca = template.points[2 + i * 3]
                local cb = template.points[3 + i * 3]
                local b = template.points[4 + i * 3]

                local points = communalHelper.getCubicCurve({a.x, a.y}, {b.x, b.y}, {ca.x, ca.y}, {cb.x, cb.y}, 24)
                table.insert(sprites, drawableLine.fromPoints(points))

                table.insert(sprites, drawableLine.fromPoints({a.x, a.y, ca.x + 0.5, ca.y + 0.5}, controlLineColor))
                table.insert(sprites, drawableLine.fromPoints({b.x, b.y, cb.x + 0.5, cb.y + 0.5}, controlLineColor))
                table.insert(sprites, drawableLine.fromPoints({ca.x + 0.5, ca.y + 0.5, cb.x + 0.5, cb.y + 0.5}, cubicControlLineColor))
            end
        end
    end

    if template.info then
        local infoSprite = drawableSprite.fromTexture(template.info, entity)
        infoSprite:addPosition(0, 16)
        table.insert(sprites, infoSprite)
    end

    return sprites
end

function curvedDreamBooster.sprite(room, entity)
    local sprites = getCurveSprites(entity)
    table.insert(sprites, drawableSprite.fromTexture(bubbleTexture, entity))
    return sprites
end

function curvedDreamBooster.nodeTexture(room, entity, node, nodeIndex, viewport)
    local nodes = entity.nodes or {}

    local mode = entity.curve or "Quadratic"
    local m = mode == "Cubic" and 3 or 2
    local n = #nodes

    if nodeIndex > n - (n % m) then
        return extraNodeTexture
    end

    if nodeIndex % m ~= 0 then
        return controlNodeTexture
    end

    return insideTexture
end

function curvedDreamBooster.selection(room, entity)
    local x, y = entity.x, entity.y
    local nodes = entity.nodes or {{x = x + 16, y = y}, {x = x + 32, y = y}}

    local mode = entity.curve or "Quadratic"
    local m = mode == "Cubic" and 3 or 2

    local nodeRectangles = {}
    for i, node in ipairs(nodes) do
        if i % m ~= 0 then
            table.insert(nodeRectangles, utils.rectangle(node.x - 4, node.y - 4, 9, 9))
        else
            table.insert(nodeRectangles, utils.rectangle(node.x - 9, node.y - 9, 18, 18))
        end
    end

    return utils.rectangle(x - 9, y - 9, 18, 18), nodeRectangles
end

return curvedDreamBooster
