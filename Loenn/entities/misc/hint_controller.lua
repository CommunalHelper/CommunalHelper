local hintController = {}

hintController.name = "CommunalHelper/HintController"
hintController.depth = -1000000
hintController.texture = "objects/CommunalHelper/hintController/icon"

hintController.placements = {
    {
        name = "controller",
        data = {
            titleDialog = "communalhelper_entities_hint_controller_menu"
            dialogIds = "",
            singleUses = "",
            selectorCounter = "",
            selectNextHint = false,
        },
    },
}

return hintController