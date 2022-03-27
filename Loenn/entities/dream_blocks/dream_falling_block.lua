local drawableRectangle = require("structs.drawable_rectangle")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dreamFallingBlock = {}

dreamFallingBlock.name = "CommunalHelper/DreamFallingBlock"

function dreamFallingBlock.depth(room, entity)
    return entity.below and 5000 or -11000
end

-- let's not show the 'chained' field (which is needed in the c# side of the entity)
-- you get to chose whether this falling block is chained the the placements
dreamFallingBlock.ignoredFields = {
    "chained",
}

dreamFallingBlock.placements = {
    {
        name = "dream_falling_block_normal",
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            featherMode = false,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
            noCollide = false,
	        forceShake = false,
            chained = false,
        }
    },
    {
        name = "dream_falling_block_chained",
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            featherMode = false,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
            noCollide = false,
	        forceShake = false,
            fallDistance = 64,
            centeredChain = false,
            chainOutline = true,
            indicator = false,
            indicatorAtStart = false,
            chained = true,
        }
    }
}

function dreamFallingBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8

    local sprites = {}

    if entity.chained then
        local fallDistance = entity.fallDistance or 16
        local rect = drawableRectangle.fromRectangle("bordered", x, y, width, height + fallDistance, {0, 0, 0, 0}, {1, 1, 1})
        rect.depth = 0
        table.insert(sprites, rect)
    end

    table.insert(sprites, communalHelper.getCustomDreamBlockSprites(x, y, width, height, entity.featherMode))

    return sprites
end

return dreamFallingBlock
