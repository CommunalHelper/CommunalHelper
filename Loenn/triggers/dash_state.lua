local modes = {
    "OneUse",
    "Trigger",
    "Field",
}

local dashStates = {
    "DreamTunnel",
    "SeekerDash",
}

return {
    name = "CommunalHelper/DashStateTrigger",

    fieldInformation = {
        mode = {
            editable = false,
            options = modes
        },
        dashState = {
            editable = false,
            options = dashStates
        }
    },

    placements = {
        name = "trigger",
        data = {
            mode = "Trigger",
            dashState = "DreamTunnel",
        }
    }
}