local utils = require("utils")

local dreamJellyfish = {}

dreamJellyfish.name = "CommunalHelper/DreamJellyfish"
dreamJellyfish.depth = -5

dreamJellyfish.placements = {
    {
        name = "normal",
        data = {
            bubble = false,
            tutorial = false
        }
    },
    {
        name = "floating",
        data = {
            bubble = true,
            tutorial = false
        }
    }
}

dreamJellyfish.texture = "objects/CommunalHelper/dreamJellyfish/jello"

function dreamJellyfish.rectangle(room, entity)
    return utils.rectangle(entity.x - 14, entity.y - 15, 30, 19)
end

return dreamJellyfish
