local cassetteJumpFixController = {}

cassetteJumpFixController.name = "CommunalHelper/CassetteJumpFixController"
cassetteJumpFixController.depth = -1000000
cassetteJumpFixController.texture = "objects/CommunalHelper/cassetteJumpFixController/icon"

cassetteJumpFixController.placements = {
    {
        name = "controller",
        data = {
            persistent = false,
            off = false,
        }
    }
}

return cassetteJumpFixController
