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
            width = 16,
            height = 16,
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
            width = 16,
            height = 16,
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
            width = 16,
            height = 16,
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
            width = 16,
            height = 16,
            featherMode = true,
            oneUse = true,
            refillCount = -1,
            below = false,
            quickDestroy = false,
        }
    }
}

function connectedDreamBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8

    return communalHelper.getCustomDreamBlockSprites(x, y, width, height, entity.featherMode)
end

return connectedDreamBlock
