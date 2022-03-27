local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local connectedDreamBlock = {}

connectedDreamBlock.name = "CommunalHelper/ConnectedDreamBlock"

function connectedDreamBlock.depth(room, entity)
    return entity.below and 5000 or -11000
end

connectedDreamBlock.placements = {
    {
        name = "normal",
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            featherMode = false,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
        }
    },
    {
        name = "feather",
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            featherMode = true,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
        }
    },
    {
        name = "normal_oneuse",
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            featherMode = false,
            oneUse = true,
            refillCount = -1,
            below = false,
            quickDestroy = false,
        }
    },
    {
        name = "feather_oneuse",
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            featherMode = true,
            oneUse = true,
            refillCount = -1,
            below = false,
            quickDestroy = false,
        }
    }
}

function connectedDreamBlock.sprite(room, entity)
    return communalHelper.getCustomDreamBlockSpritesByEntity(entity)
end

return connectedDreamBlock
