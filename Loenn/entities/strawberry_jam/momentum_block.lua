local drawableRect = require("structs.drawable_rectangle")
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
        speed = 10,
        direction = 0.0,
        speedFlagged = 10,
        directionFlagged = 0.0,
        startColor = "9A0000",
        endColor = "00FFFF",
        flag = ""
    }
}

mb.sprite = function(entity, room) 
    local startColor = utils.getColor(entity.startColor)
    local endColor = utils.getColor(entity.endColor)
    local g = math.abs((1 - (entity.speed / 282)) % 2.0 - 1)
    local color = communalHelper.colorLerp(startColor, endColor, g)
    return drawableRect.fromRectangle("bordered", entity.x, entity.y, entity.width, entity.height, color, {0,0,0,0})
end