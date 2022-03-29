local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")

local playerBubbleRegion = {}

playerBubbleRegion.name = "CommunalHelper/PlayerBubbleRegion"
playerBubbleRegion.depth = -1000000
playerBubbleRegion.nodeLimits = {2, 2}
playerBubbleRegion.nodeVisibility = "always"

playerBubbleRegion.placements = {
    {
        name = "player_bubble_region",
        data = {
            width = 24,
            height = 24,
        }
    }
}

local icon = "characters/player/bubble"
local color = {45 / 255, 103 / 255, 111 / 255, 200 / 255}

function playerBubbleRegion.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)

    local rect = drawableRectangle.fromRectangle("fill", x, y, width, height, color)
    local iconSprite = drawableSprite.fromTexture(icon, entity)
    iconSprite:addPosition(halfWidth, halfHeight)

    local nodes = entity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}
    local cx, cy = x + halfWidth, y + halfHeight
    local mx, my = nodes[1].x, nodes[1].y
    local nx, ny = nodes[2].x, nodes[2].y

    local lines = drawableLine.fromPoints({cx, cy, mx, my, nx, ny}, color)

    return {rect, lines, iconSprite}
end

playerBubbleRegion.nodeTexture = icon

return playerBubbleRegion
