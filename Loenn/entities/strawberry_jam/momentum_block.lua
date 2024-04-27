local drawableRect = require("structs.drawable_rectangle")
local drawableSprite = require('structs.drawable_sprite')
local utils = require('utils')
local communalHelper = require('mods').requireFromPlugin('libraries.communal_helper')

local mb = {name = "CommunalHelper/SJ/MomentumBlock"}
mb.depth = -11000
mb.minimumSize = {8, 8}
mb.fieldInformation = {
    startColor = {fieldType = "color", defaultValue = "9A0000"},
    endColor = {fieldType = "color", defaultValue = "00FFFF"},
    speed = {fieldType = "number", minimumValue = 0},
    speedFlagged = {fieldType = "number", minimumValue = 0},
}
mb.placements = {
    name = "momentum",
    data = {
        width = 8,
        height = 8,
        speed = 10,
        direction = 0.0,
        speedFlagged = 10,
        directionFlagged = 0.0,
        startColor = "9A0000",
        endColor = "00FFFF",
        flag = ""
    }
}
-- returns number between 0 and 8 for arrow texture
local function angle(direction)
    return math.floor(((math.pi * 2) - direction) % (math.pi * 2) / (math.pi * 2) * 8 + 0.5)
end

mb.sprite = function(room, entity) 
    local startColor = utils.getColor(entity.startColor or "9A0000")
    local endColor = utils.getColor(entity.endColor or "00FFFF")
    local speed = entity.speed or 10
    local g = math.abs((1 - (speed / 282)) % 2.0 - 1)
    local color = communalHelper.colorLerp(startColor, endColor, g)
    local str = "objects/moveBlock/arrow0"
    if (entity.width <= 8 or entity.height <= 8) then str = "objects/CommunalHelper/strawberryJam/momentumBlock/trianglearrow0" end
    str = str .. tostring(angle(entity.direction or 0))
    local sprite = drawableSprite.fromTexture(str, entity)
    sprite:addPosition(math.floor(entity.width/2),math.floor(entity.height/2))
    sprite.justificationX = 0.5
    sprite.justificationY = 0.5
    return {
        drawableRect.fromRectangle("fill", entity.x, entity.y, entity.width, entity.height, color), sprite
    }
end

return mb