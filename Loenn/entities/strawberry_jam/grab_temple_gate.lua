local drawableSprite = require("structs.drawable_sprite")

local grabTempleGate = {}

grabTempleGate.name = "CommunalHelper/SJ/GrabTempleGate"
grabTempleGate.depth = -9000
grabTempleGate.canResize = {false, false}

grabTempleGate.placements = {
    {
        name = "open",
        data = {
            closed = false,
        }
    },
    {
        name = "closed",
        data = {
            closed = true,
        }
    }
}

local texture = "objects/CommunalHelper/strawberryJam/grabTempleGate/TempleDoor05"

function grabTempleGate.sprite(room, entity)
    local sprite = drawableSprite.fromTexture(texture, entity)

    if not entity.closed then
        sprite.color = {1.0, 1.0, 1.0, 0.45}
    end

    sprite:setJustification(0.5, 0.0)
    sprite:addPosition(4, 0)

    return sprite
end

return grabTempleGate
