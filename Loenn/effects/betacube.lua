local betacube = {}

betacube.name = "CommunalHelper/BetaCube"
betacube.canBackground = true
betacube.canForeground = false

betacube.fieldInformation = {
    texture = {
        editable = true,
        options = {
            "backdrops/CommunalHelper/betacube"
        }
    },
    colors = {
        fieldType = "list",
        elementSeparator = ",",
        elementDefault = "ffffff",
        elementOptions = {
            fieldType = "color"
        }
    },
    scale = {
        minimumValue = 0.0,
        fieldType = "number"
    }
}

betacube.defaultData =  {
    texture = "backdrops/CommunalHelper/betacube",
    colors = "ff172b,fda32c,298ca4,2f25fb",
    scale = 1.0
}

return betacube