return {
    name = "CommunalHelper/SolarElevatorLevelTrigger",
    fieldInformation = {
        elevatorID = {
            fieldType = "integer",
        },
        position = {
            options = {"Closest", "Top", "Bottom"},
            editable = false
        }
    },
    placements = {
        {
            name = "trigger",
            data = {
                elevatorID = 0,
                position = "Closest",
            }
        }
    }
}
