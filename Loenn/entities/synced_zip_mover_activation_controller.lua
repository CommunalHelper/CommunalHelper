local syncedZipMoverActivationController = {}

syncedZipMoverActivationController.name = "CommunalHelper/SyncedZipMoverActivationController"
syncedZipMoverActivationController.depth = -1000000
syncedZipMoverActivationController.texture = "objects/CommunalHelper/syncedZipMoverActivationController/syncedZipMoverActivationController"
syncedZipMoverActivationController.fieldInformation = {
    colorCode = {
        fieldType = "color"
    },
    zipMoverSpeedMultiplier = {
        minimumValue = 0.0
    }
}

syncedZipMoverActivationController.placements = {
    {
        name = "controller",
        data = {
            colorCode = "ff00ff",
            zipMoverSpeedMultiplier = 1.0
        }
    }
}

return syncedZipMoverActivationController
