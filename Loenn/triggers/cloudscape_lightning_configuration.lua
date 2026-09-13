return {
    name = "CommunalHelper/CloudscapeLightningConfigurationTrigger",
    fieldInformation = {
        colorsFrom = {
            fieldType = "list",
            elementSeparator = ",",
            elementDefault = "ffffff",
            elementOptions = {
                fieldType = "color"
            }
        },
        lightningFlashColor = {
            fieldType = "color"
        },
        lightningMinDelay = {
            minimumValue = 0.0
        },
        lightningMaxDelay = {
            minimumValue = 0.0
        },
        lightningMinDuration = {
            minimumValue = 0.0
        },
        lightningMaxDuration = {
            minimumValue = 0.0
        },
        lightningIntensity = {
            minimumValue = 0.0,
            maximumValue = 1.0
        }
    },
    ignoredFields = {
        "_name",
        "_id",
        "enable"
    },
    placements = {
        {
            name = "enable",
            data = {
                tag = "",
                enable = true,
                lightningColors = "384bc8,7a50d0,c84ddd,3397e2",
                lightningFlashColor = "ffffff",
                lightningMinDelay = 5.0,
                lightningMaxDelay = 40.0,
                lightningMinDuration = 0.5,
                lightningMaxDuration = 1.0,
                lightningIntensity = 0.4
            }
        },
        {
            name = "disable",
            data = {
                tag = "",
                enable = false
            }
        }
    }
}
