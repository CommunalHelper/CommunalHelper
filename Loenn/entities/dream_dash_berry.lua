local dreamDashBerry = {}

dreamDashBerry.name = "CommunalHelper/DreamStrawberry"
dreamDashBerry.texture = "collectables/dreamberry/wings01"
dreamDashBerry.depth = -100
dreamDashBerry.fieldInformation = {
    order = {
        fieldType = "integer",
    },
    checkpointID = {
        fieldType = "integer"
    }
}

dreamDashBerry.placements = {
    name = "dream_dash_berry",
    data = {
        order = -1,
        checkpointID = -1
    }
}

return dreamDashBerry
