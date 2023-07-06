local drawableSprite = require "structs.drawable_sprite"
local drawableLine = require "structs.drawable_line"
local utils = require "utils"
local mods = require "mods"
local communalHelper = mods.requireFromPlugin "libraries.communal_helper"

local path = {}

path.name = "CommunalHelper/ShapeshifterPath"
path.depth = -1000000
path.nodeLimits = {3, 3}
path.nodeVisibility = "never"

path.fieldInformation = {
    duration = {
        fieldType = "number",
        minimumValue = 0.0
    },
    easer = {
        editable = false,
        options = communalHelper.easers,
    },
    rotateYaw = {
        fieldType = "integer",
    },
    rotatePitch = {
        fieldType = "integer",
    },
    rotateRoll = {
        fieldType = "integer",
    },
    quakeTime = {
        fieldType = "number",
        minimumValue = 0.0,
    },
    fakeoutTime = {
        fieldType = "number",
        minimumValue = 0.0,
    },
    fakeoutDistance = {
        fieldType = "number",
        minimumValue = 0.0,
    }
}

path.placements = {
    name = "shapeshiter_path",
    data = {
        duration = 2.0,
        easer = "Linear",
        rotateYaw = 0,
        rotatePitch = 0,
        rotateRoll = 0,
        quakeTime = 0.5,
        fakeoutTime = 0.75,
        fakeoutDistance = 32.0
    }
}

local function getCubicCurvePoint(start, stop, controlA, controlB, t)
    local t2 = t * t
    local t3 = t2 * t
    local mt = 1 - t
    local mt2 = mt * mt
    local mt3 = mt2 * mt

    local aMul = 3 * mt2 * t
    local bMul = 3 * mt * t2

    local x = mt3 * start[1] + aMul * controlA[1] + bMul * controlB[1] + t3 * stop[1]
    local y = mt3 * start[2] + aMul * controlA[2] + bMul * controlB[2] + t3 * stop[2]

    return x, y
end

local function getCubicCurveDerivative(start, stop, controlA, controlB, t)
    local tm = t - 1
    local threetm1 = (3 * t) - 1

    local fa = -3 * tm * tm
    local fb = 3 * tm * threetm1
    local fc = 1 - (threetm1 * threetm1)
    local fd = 3 * t * t

    local x = (fa * start[1]) + (fb * controlA[1]) + (fc * controlB[1]) + (fd * stop[1])
    local y = (fa * start[2]) + (fb * controlA[2]) + (fc * controlB[2]) + (fd * stop[2])

    return x, y
end

local function getCubicCurve(start, stop, controlA, controlB, resolution)
    resolution = resolution or 16

    local res = {}

    for i = 0, resolution do
        local x, y = getCubicCurvePoint(start, stop, controlA, controlB, i / resolution)

        table.insert(res, x)
        table.insert(res, y)
    end

    return res
end

local controlLineColor = {1, 1, 1, 0.2}
local cubicControlLineColor = {0.5, 0.5, 0.5, 0.075}
local controlNodeTexture = "particles/CommunalHelper/ring"
local arrowTexture = "particles/CommunalHelper/l"

function path.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x + 16, y = y}, {x = x + 32, y = y}, {x = x + 48, y = y}}

    local count = #nodes

    local points = {{x = x, y = y}}
    for i = 1, count do
        table.insert(points, nodes[i])
    end

    local sprites = {}

    table.insert(sprites, drawableSprite.fromTexture(controlNodeTexture, entity))

    local a = points[1]
    local ca = points[2]
    local cb = points[3]
    local b = points[4]

    local function arrowAt(t)
        local mx, my = getCubicCurvePoint({a.x, a.y}, {b.x, b.y}, {ca.x, ca.y}, {cb.x, cb.y}, t)
        local dx, dy = getCubicCurveDerivative({a.x, a.y}, {b.x, b.y}, {ca.x, ca.y}, {cb.x, cb.y}, t)
        local arrow = drawableSprite.fromTexture(arrowTexture, {x = mx, y = my})
        arrow.rotation = math.atan(dy / dx) + (dx >= 0 and 0 or math.pi) + math.pi / 4
        table.insert(sprites, arrow)
    end

    table.insert(sprites, drawableSprite.fromTexture(controlNodeTexture, ca))
    table.insert(sprites, drawableSprite.fromTexture(controlNodeTexture, cb))
    table.insert(sprites, drawableSprite.fromTexture(controlNodeTexture, b))

    table.insert(sprites, drawableLine.fromPoints(getCubicCurve({a.x, a.y}, {b.x, b.y}, {ca.x, ca.y}, {cb.x, cb.y}, 32)))

    arrowAt(0.25)
    arrowAt(0.50)
    arrowAt(0.75)

    table.insert(sprites, drawableLine.fromPoints({a.x, a.y, ca.x + 0.5, ca.y + 0.5}, controlLineColor))
    table.insert(sprites, drawableLine.fromPoints({b.x, b.y, cb.x + 0.5, cb.y + 0.5}, controlLineColor))
    table.insert(sprites, drawableLine.fromPoints({ca.x + 0.5, ca.y + 0.5, cb.x + 0.5, cb.y + 0.5}, cubicControlLineColor))

    return sprites
end

function path.selection(room, entity)
    local x, y = entity.x, entity.y
    local nodes = entity.nodes or {{x = x + 16, y = y}, {x = x + 32, y = y}, {x = x + 48, y = y}}

    local nodeRectangles = {}
    for _, node in ipairs(nodes) do
        table.insert(nodeRectangles, utils.rectangle(node.x - 4, node.y - 4, 8, 8))
    end

    return utils.rectangle(x - 4, y - 4, 8, 8), nodeRectangles
end

return path
