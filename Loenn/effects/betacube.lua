local betacube = {}

betacube.name = "CommunalHelper/BetaCube"
betacube.canBackground = true
betacube.canForeground = false

betacube.fieldInformation = {
    scale = {
        minimumValue = 0.0,
        fieldType = "number"
    },
    texture = {
        editable = true,
        options = {
            "backdrops/CommunalHelper/betacube"
        }
    }
}

betacube.defaultData =  {
    texture = "backdrops/CommunalHelper/betacube",
    colors = "ff172b,fda32c,298ca4,2f25fb",
    scale = 1.0,
}

return betacube