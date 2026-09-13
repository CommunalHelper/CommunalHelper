local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

return {
    name = "CommunalHelper/CloudscapeColorTransitionTrigger",
    fieldInformation = {
        mode = {
            editable = false,
            options = communalHelper.lerpDirections
        },
        colorsFrom = {
            fieldType = "list",
            elementSeparator = ",",
            elementDefault = "ffffffff",
            elementOptions = {
                fieldType = "color",
                useAlpha = true
            }
        },
        colorsTo = {
            fieldType = "list",
            elementSeparator = ",",
            elementDefault = "ffffffff",
            elementOptions = {
                fieldType = "color",
                useAlpha = true
            }
        },
        bgFrom = {
            fieldType = "color",
            useAlpha = true
        },
        bgTo = {
            fieldType = "color",
            useAlpha = true
        }
    },
    placements = {
        name = "trigger",
        data = {
            mode = "LeftToRight",
            colorsFrom = "6d8adaff,aea0c1ff,d9cbbcff",
            colorsTo = "ff0000ff,00ff00ff,0000ffff",
            bgFrom = "4f9af7ff",
            bgTo = "000000ff"
        }
    }
}
