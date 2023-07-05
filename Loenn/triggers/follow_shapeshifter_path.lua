return {
    name = "CommunalHelper/FollowShapeshifterPathTrigger",
    fieldInformation = {
        pathID = {
            fieldType = "integer",
            minimumValue = -1,
        }
    },
    placements = {
        {
            name = "follow_path",
            data = {
                pathID = -1,
                shapeshifterID = "",
                once = true,
            }
        }
    }
}
