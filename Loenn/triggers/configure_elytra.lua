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
                updateCooldownInEveryState = true,
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
                updateCooldownInEveryState = true,
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
                updateCooldownInEveryState = true,
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
        "updateCooldownInEveryState",
        "revertOnLeave", "revertOnDeath", "onlyOnce",
        "flag", "flagInverted",
    },
}
