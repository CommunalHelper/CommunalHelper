local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local glowController = {}

glowController.name = "CommunalHelper/GlowController"
glowController.depth = -1000000
glowController.texture = "objects/CommunalHelper/glowController/icon"

glowController.fieldOrder = {
    "x", "y",
    "lightBlacklist", "bloomBlacklist",
    "lightWhitelist", "bloomWhitelist",
    "lightColor", "bloomAlpha",
    "lightAlpha", "bloomRadius",
    "lightStartFade", "bloomOffsetX",
    "lightEndFade", "bloomOffsetY",
    "lightOffsetX", "deathAnimationIds",
    "lightOffsetY", "respawnAnimationIds",
    "flag", "flagFadeTime"
}

glowController.fieldInformation = {
    lightWhitelist = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return communalHelper.getMapSIDs() end,
             searchable = true,
        },
    },
    lightBlacklist = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return communalHelper.getMapSIDs() end,
             searchable = true,
        },
    },
    lightColor = {
        fieldType = "color",
    },
    lightAlpha = {
        fieldType = "number",
    },
    lightStartFade = {
        fieldType = "integer",
    },
    lightEndFade = {
        fieldType = "integer",
    },
    lightOffsetX = {
        fieldType = "integer",
    },
    lightOffsetY = {
        fieldType = "integer",
    },
    bloomWhitelist = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return communalHelper.getMapSIDs() end,
             searchable = true,
        },
    },
    bloomBlacklist = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return communalHelper.getMapSIDs() end,
             searchable = true,
        },
    },
    bloomAlpha = {
        fieldType = "number",
    },
    bloomRadius = {
        fieldType = "number",
    },
    bloomOffsetX = {
        fieldType = "integer",
    },
    bloomOffsetY = {
        fieldType = "integer",
    },
    deathAnimationIds = {
        fieldType = "list",
    },
    respawnAnimationIds = {
        fieldType = "list",
    },
    flagFadeTime = {
        minimumValue = 0.0,
    },
}

glowController.placements = {
    {
        name = "controller",
        data = {
            lightWhitelist = "",
            lightBlacklist = "",
            lightColor = "FFFFFF",
            lightAlpha = 1.0,
            lightStartFade = 24,
            lightEndFade = 48,
            lightOffsetX = 0,
            lightOffsetY = 0,
            bloomWhitelist = "",
            bloomBlacklist = "",
            bloomAlpha = 1.0,
            bloomRadius = 8.0,
            bloomOffsetX = 0,
            bloomOffsetY = 0,
            deathAnimationIds = "death",
            respawnAnimationIds = "respawn",
            flag = "",
            flagFadeTime = 1.0,
        },
    },
}

return glowController