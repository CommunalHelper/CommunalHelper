local modes = {"Alternate", "On", "Off"}

return {
    name = "CommunalHelper/TrackSwitchTrigger",

    fieldInformation = {
        mode = {
            editable = false,
            options = modes,
        },
    },

    placements = {
        name = "trigger",
        data = {
            oneUse = true,
            flash = false,
            globalSwitch = false,
            mode = "Alternate",
        }
    }
}