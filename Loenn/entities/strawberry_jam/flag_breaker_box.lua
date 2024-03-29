local utils = require("utils")
local drawableSprite = require("structs.drawable_sprite")

local breakerBox = {}

breakerBox.name = "CommunalHelper/SJ/FlagBreakerBox"
breakerBox.depth = -10550
breakerBox.texture = "objects/breakerBox/Idle00"
breakerBox.fieldInformation = {
    music_progress = {
        fieldType = "integer",
    }
}
breakerBox.placements = {
    name = "breaker_box",
    data = {
        flipX = false,
        music_progress = -1,
        music_session = false,
        music = "",
        flag = "",
        aliveState = true
    }
}

function breakerBox.scale(room, entity)
    local scaleX = entity.flipX and -1 or 1

    return scaleX, 1
end

function breakerBox.justification(room, entity)
    local flipX = entity.flipX

    return flipX and 0.75 or 0.25, 0.25
end

return breakerBox