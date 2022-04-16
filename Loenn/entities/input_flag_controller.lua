local inputFlagController = {}

inputFlagController.name = "CommunalHelper/InputFlagController"
inputFlagController.depth = -1000000
inputFlagController.texture = "objects/CommunalHelper/inputFlagController/icon"

inputFlagController.placements = {
    {
        name = "controller",
        data = {
            flags = "",
            toggle = true,
            resetFlags = false,
            delay = 0.0,
            grabOverride = false
        }
    }
}

return inputFlagController
