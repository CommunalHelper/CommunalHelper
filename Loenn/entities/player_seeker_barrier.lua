local playerSeekerBarrier = {}

playerSeekerBarrier.name = "CommunalHelper/PlayerSeekerBarrier"
playerSeekerBarrier.depth = 0
playerSeekerBarrier.minimumSize = {8, 8}

playerSeekerBarrier.placements = {
    {
        name = "normal",
        data = {
            width = 8,
            height = 8,
            spikeUp = false,
            spikeDown = false,
            spikeLeft = false,
            spikeRight = false
        }
    },
    {
        name = "spike_up",
        data = {
            width = 8,
            height = 8,
            spikeUp = true,
            spikeDown = false,
            spikeLeft = false,
            spikeRight = false
        }
    },
    {
        name = "spike_down",
        data = {
            width = 8,
            height = 8,
            spikeUp = false,
            spikeDown = true,
            spikeLeft = false,
            spikeRight = false
        }
    },
    {
        name = "spike_left",
        data = {
            width = 8,
            height = 8,
            spikeUp = false,
            spikeDown = false,
            spikeLeft = true,
            spikeRight = false
        }
    },
    {
        name = "spike_right",
        data = {
            width = 8,
            height = 8,
            spikeUp = false,
            spikeDown = false,
            spikeLeft = false,
            spikeRight = true
        }
    },
    {
        name = "spike_all",
        data = {
            width = 8,
            height = 8,
            spikeUp = true,
            spikeDown = true,
            spikeLeft = true,
            spikeRight = true
        }
    }
}

local normalColor = {0.36, 0.36, 0.36, 0.8}
local spikyColor = {0.36, 0.13, 0.13, 0.8}

function playerSeekerBarrier.color(room, entity)
    return entity.spiky and spikyColor or normalColor
end

return playerSeekerBarrier
