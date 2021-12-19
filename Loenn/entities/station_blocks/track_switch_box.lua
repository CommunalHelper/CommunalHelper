local drawableSprite = require("structs.drawable_sprite")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local trackSwitchBox = {}

trackSwitchBox.name = "CommunalHelper/TrackSwitchBox"
trackSwitchBox.depth = 0

trackSwitchBox.placements = {
    {
        name = "normal",
        data = {
            globalSwitch = false,
            floaty = true,
            bouncy = true,
        }
    },
    {
        name = "session",
        data = {
            globalSwitch = true,
            floaty = true,
            bouncy = true,
        }
    }
}

local bgColor = {0.3, 0.3, 0.4, 1.0}

function trackSwitchBox.sprite(room, entity)
    local rect = drawableRectangle.fromRectangle("fill", entity.x + 4, entity.y + 4, 24, 24, bgColor)

    local box = drawableSprite.fromTexture("objects/CommunalHelper/trackSwitchBox/idle00", entity)
    box:setJustification(0.25, 0.25)

    return {rect, box}
end

function trackSwitchBox.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, 32, 32)
end

return trackSwitchBox
