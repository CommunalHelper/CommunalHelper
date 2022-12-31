local enums = require("consts.celeste_enums")

local lightningController = {}

lightningController.name = "CommunalHelper/LightningController"
lightningController.depth = -1000000
lightningController.texture = "objects/CommunalHelper/lightningController/icon"

lightningController.fieldInformation = {
    minDelay = {
        minimumValue = 0.0
    },
    maxDelay = {
        minimumValue = 0.0
    },
    startupDelay = {
        minimumValue = 0.0
    },
    depth = {
        options = enums.depths
    }
}

lightningController.placements = {
    {
        name = "controller",
        data = {
            minDelay = 5,
            maxDelay = 10,
            startupDelay = 4,
            depth = 0
        }
    }
}

return lightningController
