local cloudscape = {}

cloudscape.name = "CommunalHelper/Cloudscape"
cloudscape.canBackground = true
cloudscape.canForeground = false

cloudscape.fieldInformation = {
    bgColor = {
        fieldType = "color"
    },
    innerRadius = {
        minimumValue = 1.0
    },
    outerRadius = {
        minimumValue = 2.0
    },
    rings = {
        fieldType = "integer",
        minimumValue = 2
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
    },
    innerDensity = {
        minimumValue = 0.0,
        maximumValue = 2.0
    },
    outerDensity = {
        minimumValue = 0.0,
        maximumValue = 2.0
    },
    rotationExponent = {
        minimumValue = 0.0,
        maximumValue = 2.0
    }
}

cloudscape.defaultData = {
    seed = "",
    colors = "6d8ada,aea0c1,d9cbbc",
    bgColor = "4f9af7",
    innerRadius = 40.0,
    outerRadius = 400.0,
    rings = 24,
    lightning = false,
    lightningColors = "384bc8,7a50d0,c84ddd,3397e2",
    lightningFlashColor = "ffffff",
    lightningMinDelay = 5.0,
    lightningMaxDelay = 40.0,
    lightningMinDuration = 0.5,
    lightningMaxDuration = 1.0,
    lightningIntensity = 0.4,
    offsetX = 0.0,
    offsetY = 0.0,
    parallaxX = 0.05,
    parallaxY = 0.05,
    innerDensity = 1.0,
    outerDensity = 1.0,
    innerRotation = 0.002,
    outerRotation = 0.2,
    rotationExponent = 2.0,
    hasBackgroundColor = true,
    additive = false,
}

return cloudscape