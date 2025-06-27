local manualCassetteController = {}

manualCassetteController.name = "CommunalHelper/ManualCassetteController"
manualCassetteController.depth = -1000000

manualCassetteController.placements = {
    {
        name = "controller",
        data = {
            startIndex = 0,
        }
    }
}

local alt = math.random(100) == 42
manualCassetteController.texture = string.format("objects/CommunalHelper/manualCassetteController/icon%s", (alt and "_wacked" or ""))

return manualCassetteController
