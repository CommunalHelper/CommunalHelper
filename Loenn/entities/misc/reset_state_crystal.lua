local utils = require("utils")

local resetStateCrystal = {}

resetStateCrystal.name = "CommunalHelper/ResetStateCrystal"
resetStateCrystal.depth = -100
resetStateCrystal.placements = {
    {
        name = "reset_state_crystal",
        data = {
            oneUse = false
        }
    }
}

resetStateCrystal.texture = "objects/CommunalHelper/resetStateCrystal/ghostIdle00"
resetStateCrystal.color = {0.35, 0.35, 0.35, 1.0}

function resetStateCrystal.rectangle(room, entity)
    return utils.rectangle(entity.x - 5, entity.y - 5, 10, 10)
end

return resetStateCrystal
