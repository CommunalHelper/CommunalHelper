local fakeTilesHelper = require "helpers.fake_tiles"

local elytraDashBlock = {}

elytraDashBlock.name = "CommunalHelper/ElytraDashBlock"
elytraDashBlock.depth = 0

local getFakeTilesFieldInformation = fakeTilesHelper.getFieldInformation("tiletype")
function elytraDashBlock.fieldInformation()
    local fields = getFakeTilesFieldInformation()

    fields.requiredSpeed = {
        fieldType = "number",
        minimumValue = 0.0,
    }

    return fields
end

elytraDashBlock.placements = {
    name = "elytra_dash_block",
    data = {
        width = 8,
        height = 8,
        tiletype = "3",
        blendin = true,
        permanent = false,
        requiredSpeed = 240.0,
    }
}

elytraDashBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", "blendin")

return elytraDashBlock
