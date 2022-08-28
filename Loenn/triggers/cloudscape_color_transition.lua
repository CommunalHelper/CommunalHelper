local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

return {
    name = "CommunalHelper/CloudscapeColorTransitionTrigger",
    fieldInformation = {
        mode = {
            editable = false,
            options = communalHelper.lerpDirections
        },
        bgFrom = {
            fieldType = "color"
        },
        bgTo = {
            fieldType = "color"
        }
    },
    placements = {
        name = "trigger",
        data = {
            mode = "LeftToRight",
            colorsFrom = "6d8ada,aea0c1,d9cbbc",
            colorsTo = "ff0000,00ff00,0000ff",
            bgFrom = "4f9af7",
            bgTo = "000000"
        }
    }
}
