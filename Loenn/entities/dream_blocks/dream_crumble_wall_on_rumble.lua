local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dreamCrumbleWallOnRumble = {}

dreamCrumbleWallOnRumble.name = "CommunalHelper/DreamCrumbleWallOnRumble"
dreamCrumbleWallOnRumble.fieldInformation = {
    refillCount = {
        fieldType = "integer"
    }
}

function dreamCrumbleWallOnRumble.depth(room, entity)
    return entity.below and 5000 or -11000
end

dreamCrumbleWallOnRumble.placements = {
    {
        name = "dream_crumble_wall_on_rumble",
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            featherMode = false,
            dashSpeed = 240.0,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
            persistent = false
        }
    }
}

function dreamCrumbleWallOnRumble.sprite(room, entity)
    return communalHelper.getCustomDreamBlockSpritesByEntity(entity)
end

return dreamCrumbleWallOnRumble
