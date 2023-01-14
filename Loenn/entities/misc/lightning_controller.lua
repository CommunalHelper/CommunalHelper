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
    flash = {
        maximumValue = 1.0,
        minimumValue = 0.0
    },
    flashDuration = {
        minimumValue = 0.0
    },
    depth = {
        fieldType = "integer",
        options = enums.depths
    },
    shakeAmount = {
        minimumValue = 0.0
    },
    probability = {
        maximumValue = 1.0,
        minimumValue = 0.0
    },
    color = {
        fieldType = "color"
    },
    flashColor = {
        fieldType = "color"
    }
}

lightningController.placements = {
    {
        name = "controller",
        data = {
            minDelay = 5,
            maxDelay = 10,
            startupDelay = 4,
            flash = 0.3,
            flashDuration = 0.5,
            depth = 0,
            shakeAmount = 0.3,
            sfx = "event:/new_content/game/10_farewell/lightning_strike",
            probability = 1.0,
            color = "ffffff",
            flashColor = "ffffff"
        }
    }
}

return lightningController
