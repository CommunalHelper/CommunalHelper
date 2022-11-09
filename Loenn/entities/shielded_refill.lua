local drawableSprite = require("structs.drawable_sprite")

local shieldedRefill = {}

shieldedRefill.name = "CommunalHelper/ShieldedRefill"
shieldedRefill.depth = -100
shieldedRefill.placements = {
    {
        name = "one_dash",
        data = {
            twoDashes = false,
            oneUse = false,
            bubbleRepel = true
        }
    },
    {
        name = "two_dashes",
        data = {
            twoDashes = true,
            oneUse = false,
            bubbleRepel = true
        }
    }
}

local function getSprite(entity)
    return drawableSprite.fromTexture(
        entity.twoDashes and "objects/refillTwo/idle00" or "objects/refill/idle00",
        entity
    )
end

function shieldedRefill.selection(room, entity)
    local sprite = getSprite(entity)

    return sprite:getRectangle()
end

function shieldedRefill.draw(room, entity, viewport)
    local x, y = entity.x or 0, entity.y or 0

    love.graphics.circle("line", x, y, 8)
    getSprite(entity):draw()
end

return shieldedRefill
