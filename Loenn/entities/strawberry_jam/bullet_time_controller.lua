local bulletTimeController = {}

bulletTimeController.name = "CommunalHelper/SJ/BulletTimeController"
bulletTimeController.depth = -1000000
bulletTimeController.texture = "objects/CommunalHelper/bulletTimeController/icon"
bulletTimeController.fieldInformation = {
    timerate = {
        minimumValue = 0.01
    },
    minDashes = {
        fieldType = "integer",
        minimumValue = 0
    }
}

bulletTimeController.placements = {
    {
        name = "controller",
        data = {
            timerate = 1.0,
            flag = "",
            minDashes = 1
        }
    }
}

return bulletTimeController
