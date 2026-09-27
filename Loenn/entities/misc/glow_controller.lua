local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local glowController = {}

glowController.name = "CommunalHelper/GlowController"
glowController.depth = -1000000
glowController.texture = "objects/CommunalHelper/glowController/icon"

glowController.fieldOrder = {
    "x", "y",
    "lightWhitelist", "lightBlacklist",
    "lightOffsetX", "lightOffsetY",
    "lightStartFade", "lightEndFade",
    "lightColor", "lightAlpha",
    "bloomWhitelist", "bloomBlacklist",
    "bloomOffsetX", "bloomOffsetY",
    "bloomRadius", "bloomAlpha",
    "lightOccluderWhitelist", "lightOccluderBlacklist",
    "lightOccluderOffsetX", "lightOccluderOffsetY",
    "lightOccluderWidth", "lightOccluderHeight",
    "useEntityBoundsForLightOccluders", "lightOccluderAlpha",
    "effectCutoutWhitelist", "effectCutoutBlacklist",
    "effectCutoutAlpha",
    "deathAnimationIds", "respawnAnimationIds",
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
        minimumValue = 0.0,
        maximumValue = 1.0,
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
        minimumValue = 0.0,
        maximumValue = 1.0,
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
    lightOccluderWhitelist = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return communalHelper.getMapSIDs() end,
             searchable = true,
        },
    },
    lightOccluderBlacklist = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return communalHelper.getMapSIDs() end,
             searchable = true,
        },
    },
    lightOccluderOffsetX = {
        fieldType = "integer",
    },
    lightOccluderOffsetY = {
        fieldType = "integer",
    },
    lightOccluderWidth = {
        fieldType = "integer",
    },
    lightOccluderHeight = {
        fieldType = "integer",
    },
    useEntityBoundsForLightOccluders = {
        fieldType = "boolean",
    },
    lightOccluderAlpha = {
        fieldType = "number",
        minimumValue = 0.0,
        maximumValue = 1.0,
    },
    effectCutoutWhitelist = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return communalHelper.getMapSIDs() end,
             searchable = true,
        },
    },
    effectCutoutBlacklist = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "",
        elementOptions = {
             options = function() return communalHelper.getMapSIDs() end,
             searchable = true,
        },
    },
    effectCutoutAlpha = {
        fieldType = "number",
        minimumValue = 0.0,
        maximumValue = 1.0,
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
            lightOccluderWhitelist = "",
            lightOccluderBlacklist = "",
            lightOccluderOffsetX = 0,
            lightOccluderOffsetY = 0,
            lightOccluderWidth = 16,
            lightOccluderHeight = 16,
            useEntityBoundsForLightOccluders = true,
            lightOccluderAlpha = 1.0,
            effectCutoutWhitelist = "",
            effectCutoutBlacklist = "",
            effectCutoutAlpha = 1.0,
            deathAnimationIds = "death",
            respawnAnimationIds = "respawn",
            flag = "",
            flagFadeTime = 1.0,
        },
    },
}

return glowController