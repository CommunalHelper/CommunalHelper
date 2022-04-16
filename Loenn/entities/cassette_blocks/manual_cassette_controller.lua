local cassetteJumpFixController = {}

cassetteJumpFixController.name = "CommunalHelper/ManualCassetteController"
cassetteJumpFixController.depth = -1000000

cassetteJumpFixController.placements = {
    {
        name = "controller",
        data = {
            startIndex = 0
        }
    }
}

local alt = math.random(100) == 42
cassetteJumpFixController.texture = string.format("objects/CommunalHelper/manualCassetteController/icon%s", (alt and "_wacked" or ""))

return cassetteJumpFixController
