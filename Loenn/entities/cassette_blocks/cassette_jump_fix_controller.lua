local cassetteJumpFixController = {}

cassetteJumpFixController.name = "CommunalHelper/CassetteJumpFixController"
cassetteJumpFixController.depth = -1000000

cassetteJumpFixController.placements = {
    {
        name = "controller",
        data = {
            persistent = false,
            off = false
        }
    }
}

cassetteJumpFixController.texture = "objects/CommunalHelper/cassetteJumpFixController/icon"

return cassetteJumpFixController
