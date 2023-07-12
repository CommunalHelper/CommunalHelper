local drawableSprite = require "structs.drawable_sprite"
local drawableLine = require "structs.drawable_line"
local utils = require "utils"

local spiralDreamBooster = {}

spiralDreamBooster.name = "CommunalHelper/SpiralDreamBooster"
spiralDreamBooster.depth = -11000

spiralDreamBooster.nodeLimits = {1, 1}
spiralDreamBooster.nodeLineRenderType = "line"
spiralDreamBooster.nodeVisibility = "always"

spiralDreamBooster.ignoredFields = {"_name", "_id", "_type", "direct"}

spiralDreamBooster.fieldInformation = {
    spiralSpeed = {
        minimumValue = 0.0
    },
    beginTime = {
        minimumValue = 0.0
    },
    angle = {
        minimumValue = 0.0
    },
    delay = {
        minimumValue = 0.0
    },
    pathColor = {
        fieldType = "color",
    }
}

spiralDreamBooster.placements = {
    {
        name = "clockwise",
        placementType = "line",
        data = {
            angle = 180.0,
            clockwise = true,
            spiralSpeed = 240.0,
            beginTime = 0.75,
            delay = 0.2,
            pathColor = "ffffff",
            direct = false
        }
    },
    {
        name = "counterclockwise",
        placementType = "line",
        data = {
            angle = 180.0,
            clockwise = false,
            spiralSpeed = 240.0,
            beginTime = 0.75,
            delay = 0.2,
            pathColor = "ffffff",
            direct = false
        }
    },
    {
        name = "direct_clockwise",
        placementType = "line",
        data = {
            angle = 180.0,
            clockwise = true,
            spiralSpeed = 240.0,
            pathColor = "ffffff",
            direct = true
        }
    },
    {
        name = "direct_counterclockwise",
        placementType = "line",
        data = {
            angle = 180.0,
            clockwise = false,
            spiralSpeed = 240.0,
            pathColor = "ffffff",
            direct = true
        }
    }
}

local textureClockwise = "objects/CommunalHelper/boosters/dreamBooster/idlespiral_cw00"
local textureCounterclockwise = "objects/CommunalHelper/boosters/dreamBooster/idlespiral_ccw00"
local textureNodeClockwise = "objects/CommunalHelper/boosters/dreamBooster/insidespiral_cw01"
local textureNodeCounterclockwise = "objects/CommunalHelper/boosters/dreamBooster/insidespiral_ccw01"

local precision = 64
local arrowTipLength = 5

local function addArc(sprites, x, y, r, offsetAngle, arcAngle, direct)
    local points = direct and {} or {x, y}

    for i = 0, precision do
        local th = offsetAngle + arcAngle * i / precision
        table.insert(points, x + math.cos(th) * r)
        table.insert(points, y + math.sin(th) * r)
    end

    table.insert(sprites, drawableLine.fromPoints(points))
    table.insert(sprites, drawableLine.fromPoints({x, y, x + math.cos(offsetAngle) * r, y + math.sin(offsetAngle) * r}, {1.0, 1.0, 1.0, 0.3}))
end

function spiralDreamBooster.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nx, ny = nodes[1].x, nodes[1].y

    local dx, dy = nx - x, ny - y
    local radius = math.sqrt(dx ^ 2 + dy ^ 2)

    local angleOffset = math.atan(dy / dx) + (dx >= 0 and 0 or math.pi)

    local clockwise = entity.clockwise
    local sign = (clockwise and 1 or -1)
    local angle = sign * (entity.angle or 180.0) * math.pi / 180.0
    -- multiplying by π/180 converts degrees to radians

    local direct = entity.direct

    local sprites = {}

    addArc(sprites, x, y, radius, angleOffset, angle, direct)

    local arrowAngle = angleOffset + angle + 0.01 * sign
    local cos, sin = math.cos(arrowAngle), math.sin(arrowAngle)
    local arrow_x, arrow_y = x + cos * radius, y + sin * radius

    local correctedTipLength = sign * arrowTipLength
    local arrowPoints = {
        arrow_x + sin * correctedTipLength - cos * arrowTipLength,
        arrow_y - cos * correctedTipLength - sin * arrowTipLength,
        arrow_x,
        arrow_y,
        arrow_x + sin * correctedTipLength + cos * arrowTipLength,
        arrow_y - cos * correctedTipLength + sin * arrowTipLength
    }
    table.insert(sprites, drawableLine.fromPoints(arrowPoints))

    if direct then
        local texture = clockwise and textureNodeClockwise or textureNodeCounterclockwise
        local sprite = drawableSprite.fromTexture(texture, entity)
        sprite.color = {1.0, 1.0, 1.0, 0.25}
        table.insert(sprites, sprite)
    else
        local texture = clockwise and textureClockwise or textureCounterclockwise
        table.insert(sprites, drawableSprite.fromTexture(texture, entity))
    end

    return sprites
end

function spiralDreamBooster.nodeTexture(room, entity, _, _, _)
    local direct = entity.direct
    return entity.clockwise
        and (direct and textureClockwise or textureNodeClockwise)
         or (direct and textureCounterclockwise or textureNodeCounterclockwise)
end

function spiralDreamBooster.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nx, ny = nodes[1].x, nodes[1].y

    return utils.rectangle(x - 9, y - 9, 18, 18), {utils.rectangle(nx - 9, ny - 9, 18, 18)}
end

return spiralDreamBooster
