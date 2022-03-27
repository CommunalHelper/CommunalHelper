local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dreamFloatySpaceBlock = {}

dreamFloatySpaceBlock.name = "CommunalHelper/DreamFloatySpaceBlock"

function dreamFloatySpaceBlock.depth(room, entity)
    return entity.below and 5000 or -11000
end

dreamFloatySpaceBlock.placements = {
    {
        name = "dream_floaty_space_block",
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
    }
}

function dreamFloatySpaceBlock.sprite(room, entity)
    return communalHelper.getCustomDreamBlockSpritesByEntity(entity)
end

return dreamFloatySpaceBlock
