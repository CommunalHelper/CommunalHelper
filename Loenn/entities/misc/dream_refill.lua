local utils = require("utils")

local dreamRefill = {}

dreamRefill.name = "CommunalHelper/DreamRefill"
dreamRefill.depth = -100

dreamRefill.placements = {
    {
        name = "dream_refill",
        data = {
            oneUse = false,
            respawnTime = 2.5,
            twoDash = false,
        }
    },
    {
        name = "double_dream_refill",
        data = {
            oneUse = false,
            respawnTime = 2.5,
            twoDash = true,
        }
    }
}

function dreamRefill.texture(room, entity)
    return entity.twoDash and "objects/CommunalHelper/dreamRefillTwo/idle00" or "objects/CommunalHelper/dreamRefill/idle02"
end

function dreamRefill.rectangle(room, entity)
    return entity.twoDash and utils.rectangle(entity.x - 4, entity.y - 6, 8, 12) or utils.rectangle(entity.x - 5, entity.y - 5, 10, 10)
end

return dreamRefill
