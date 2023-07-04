return {
    name = "CommunalHelper/FollowShapeshifterPathTrigger",
    fieldInformation = {
        pathID = {
            fieldType = "integer",
            minimumValue = -1,
        },
        shapeshifterID = {
            fieldType = "integer",
            minimumValue = 0,
        }
    },
    placements = {
        {
            name = "follow_path",
            data = {
                pathID = -1,
                shapeshifterID = 0,
                once = true,
            }
        }
    }
}
