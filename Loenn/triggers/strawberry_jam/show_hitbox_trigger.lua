local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

return {
    name = "CommunalHelper/SJ/ShowHitboxTrigger",
    placements = {
        name = "trigger",
        data = {
            typeNames = "Celeste.CrystalStaticSpinner"
        }
    },
    fieldInformation = {
        typeNames = {
            fieldType = "list",
            elementSeparator = ",",
            elementDefault = "",
            elementOptions = {
                 options = function() return communalHelper.getMapSIDs() end,
                 searchable = true
            },
        }
    }
}
