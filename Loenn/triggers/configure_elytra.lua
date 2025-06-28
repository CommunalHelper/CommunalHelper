return {
    name = "CommunalHelper/ConfigureElytraTrigger",
    ignoredFields = {
        "_name", "_id", "_type", "infinite",
    },
    placements = {
        {
            name = "yes",
            data = {
                allow = true,
                infinite = false,
                disableReverseVerticalMomentum = false,
                revertOnLeave = false,
                revertOnDeath = false,
                onlyOnce = false,
                flag = "",
                flagInverted = false,
            }
        },
        {
            name = "yes_infinite",
            data = {
                allow = true,
                infinite = true,
                disableReverseVerticalMomentum = false,
                revertOnLeave = false,
                revertOnDeath = false,
                onlyOnce = false,
                flag = "",
                flagInverted = false,
            }
        },
        {
            name = "no",
            data = {
                allow = false,
                infinite = false,
                disableReverseVerticalMomentum = false,
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
        "allow", "infinite", "disableReverseVerticalMomentum",
        "revertOnLeave", "revertOnDeath", "onlyOnce",
        "flag", "flagInverted",
    },
}
