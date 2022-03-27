local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local cassetteFallingBlock = {}

local colorNames = communalHelper.cassetteBlockColorNames
local colors = communalHelper.cassetteBlockHexColors

cassetteFallingBlock.name = "CommunalHelper/CassetteFallingBlock"
cassetteFallingBlock.minimumSize = {16, 16}
cassetteFallingBlock.fieldInformation = {
    index = {
        options = colorNames,
        editable = false,
        fieldType = "integer",
    },
    customColor = {
        fieldType = "color",
    }
}

cassetteFallingBlock.placements = {}
for i = 1, 4 do
    cassetteFallingBlock.placements[i] = {
        name = string.format("cassette_block_%s", i - 1),
        data = {
            index = i - 1,
            tempo = 1.0,
            width = 16,
            height = 16,
            customColor = colors[i]
        }
    }
end

function cassetteFallingBlock.sprite(room, entity)
    return communalHelper.getCustomCassetteBlockSprites(room, entity)
end

return cassetteFallingBlock