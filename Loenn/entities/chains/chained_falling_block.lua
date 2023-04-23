local drawableRectangle = require("structs.drawable_rectangle")
local fakeTilesHelper = require("helpers.fake_tiles")

local chainedFallingBlock = {}

chainedFallingBlock.name = "CommunalHelper/ChainedFallingBlock"
chainedFallingBlock.fieldInformation = {
    tiletype = {
        options = fakeTilesHelper.getTilesOptions(),
        editable = false
    },
    fallDistance = {
        minimumValue = 0,
        fieldType = "integer"
    }
}

function chainedFallingBlock.depth(room, entity)
    return entity.behind and 5000 or 0
end

chainedFallingBlock.placements = {
    name = "chained_falling_block",
    data = {
        width = 8,
        height = 8,
        tiletype = "3",
        climbFall = true,
        behind = false,
        fallDistance = 64,
        centeredChain = false,
        chainOutline = true,
        indicator = false,
        indicatorAtStart = false,
        chainTexture = "objects/CommunalHelper/chains/chain"
    }
}

local fakeTilesSpriteFunction = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

function chainedFallingBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8

    local sprites = fakeTilesSpriteFunction(room, entity)

    local fallDistance = entity.fallDistance or 16
    local rect = drawableRectangle.fromRectangle("line", x, y, width, height + fallDistance, {1, 1, 1, 0.5})
    rect.depth = 0
    table.insert(sprites, rect)

    return sprites
end

return chainedFallingBlock
