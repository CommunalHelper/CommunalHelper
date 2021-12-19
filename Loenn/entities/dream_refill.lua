local utils = require("utils")

local dreamRefill = {}

dreamRefill.name = "CommunalHelper/DreamRefill"
dreamRefill.depth = -100

dreamRefill.placements = {
    {
        name = "normal",
        data = {
            oneUse = false,
            respawnTime = 2.5
        }
    }
}

dreamRefill.texture = "objects/CommunalHelper/dreamRefill/idle02"

function dreamRefill.rectangle(room, entity)
    return utils.rectangle(entity.x - 5, entity.y - 5, 10, 10)
end

return dreamRefill
