local playerSeekerBarrier = {}

playerSeekerBarrier.name = "CommunalHelper/PlayerSeekerBarrier"
playerSeekerBarrier.depth = 0

playerSeekerBarrier.placements = {
    {
        name = "normal",
        data = {
            width = 8,
            height = 8,
            spiky = false,
        }
    },
    {
        name = "spiky",
        data = {
            width = 8,
            height = 8,
            spiky = true,
        }
    }
}

local normalColor = {0.36, 0.36, 0.36, 0.8}
local spikyColor = {0.36, 0.13, 0.13, 0.8}

function playerSeekerBarrier.color(room, entity)
    return entity.spiky and spikyColor or normalColor
end

return playerSeekerBarrier
