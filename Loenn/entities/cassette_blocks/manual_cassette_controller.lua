local cassetteJumpFixController = {}

cassetteJumpFixController.name = "CommunalHelper/ManualCassetteController"
cassetteJumpFixController.depth = -1000000

local alt = math.random(100) == 42
cassetteJumpFixController.texture = string.format("objects/CommunalHelper/manualCassetteController/icon%s", (alt and "_wacked" or ""))

cassetteJumpFixController.placements = {
    {
        name = "normal",
        data = {
            startIndex = 0
        }
    }
}

return cassetteJumpFixController
