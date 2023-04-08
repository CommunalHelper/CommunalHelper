local expiringDashRefill = {}

expiringDashRefill.name = "CommunalHelper/SJ/ExpiringDashRefill"

expiringDashRefill.fieldInformation = {
    dashExpirationTime = {
        fieldType = "number",
        minimumValue = 0.0
    },
    hairFlashThreshold = {
        fieldType = "number",
        minimumValue = 0.0,
        maximumValue = 1.0
    }
}

expiringDashRefill.placements = {
    {
        name = "expiring_dash_refill",
        data = {
            oneUse = false,
            dashExpirationTime = 5.0,
            hairFlashThreshold = 0.2
        }
    }
}

expiringDashRefill.texture = "objects/refill/idle00"

return expiringDashRefill
