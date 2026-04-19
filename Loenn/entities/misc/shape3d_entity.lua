local depths = require("consts.object_depths")

local shape3dEntity = {}

shape3dEntity.name = "CommunalHelper/Shape3DEntity"
shape3dEntity.depth = function(room, entity) return entity.depth or 0 end
shape3dEntity.texture = "objects/CommunalHelper/shape3dEntity/icon"
shape3dEntity.placements = {
    {
        name = "shape3d_entity",
        data = {
            depth = 0,
            modelParameters = "gear:6.0,2.0,3.0,4.0,4.0,1.0,ffffff",
            modelTexture = "",
            position = "0.0,0.0,0.0",
            scale = "1.0,1.0,1.0",
            yaw = 0.0,
            pitch = 0.0,
            roll = 0.0,
            speedYaw = 0.0,
            speedPitch = 0.0,
            speedRoll = 0.0,
            tint = "ffffff",
            highlightStrength = 0.0,
            rainbowMix = 0.0,
        }
    }
}

shape3dEntity.fieldOrder = {
    "x", "y",
    "depth",
    "modelParameters", "modelTexture",
    "position", "scale", "yaw", "pitch", "roll",
    "speedYaw", "speedPitch", "speedRoll",
    "tint", "highlightStrength", "rainbowMix"
}
shape3dEntity.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = depths,
        editable = true
    },
    tint = {
        fieldType = "color"
    },
    highlightStrength = {
        minimumValue = 0.0,
        maximumValue = 1.0
    },
    rainbowMix = {
        minimumValue = 0.0,
        maximumValue = 1.0
    },
}

return shape3dEntity