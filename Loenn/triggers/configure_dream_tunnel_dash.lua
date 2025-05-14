return {
    name = "CommunalHelper/ConfigureDreamTunnelDashTrigger",
    placements = {
        {
            name = "trigger",
            data = {
                allowRedirect = false,
                allowSameDirectionRedirect = false,
                sameDirectionSpeedMultiplier = 1,
                useEntryDirection = false,
                speedConfiguration = 0,
                customSpeed = 0,
                allowDashCancels = false,
                redirectConsumesNormalDash = false,
                allowTransitions = false,
                bounceOnCollision = false,
                revertOnLeave = false,
                revertOnDeath = false,
                onlyOnce = false,
                flag = "",
                flagInverted = false,
            }
        }
    },
    fieldInformation = {
        speedConfiguration = {
            options = {
                ["Default"] = 0,
                ["Never Slow Down"] = 1,
                ["Use Custom Speed"] = 2,
            },
            editable = false,
        }
    },
    fieldOrder = {
        "x", "y", "width", "height",
        "allowRedirect", "allowSameDirectionRedirect", "sameDirectionSpeedMultiplier", "redirectConsumesNormalDash",
        "useEntryDirection", "speedConfiguration", "customSpeed",
        "allowDashCancels", "allowTransitions", "bounceOnCollision",
        "revertOnLeave", "revertOnDeath", "onlyOnce",
        "flag", "flagInverted",
    },
}
