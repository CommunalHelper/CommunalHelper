return {
    name = "CommunalHelper/ConfigureSeekerDashTrigger",
    placements = {
        {
            name = "trigger",
            data = {
                respectBoosters = false,
                revertOnLeave = false,
                revertOnDeath = false,
                onlyOnce = false,
                flag = "",
                flagInverted = false,
            }
        }
    },
    fieldOrder = {
        "x", "y", "width", "height",
        "respectBoosters",
        "revertOnLeave", "revertOnDeath", "onlyOnce",
        "flag", "flagInverted",
    },
}
