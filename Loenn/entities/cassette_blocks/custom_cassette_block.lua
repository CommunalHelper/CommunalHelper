local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local customCassetteBlock = {}

customCassetteBlock.name = "CommunalHelper/CustomCassetteBlock"
customCassetteBlock.minimumSize = {16, 16}
customCassetteBlock.fieldInformation = {
    index = {
        fieldType = "integer",
    }
}

customCassetteBlock.placements = {}
for i = 1, 4 do
    customCassetteBlock.placements[i] = {
        name = string.format("cassette_block_%s", i - 1),
        data = {
            index = i - 1,
            tempo = 1.0,
            width = 16,
            height = 16,
            customColor = ""
        }
    }
end

function customCassetteBlock.sprite(room, entity)
    return communalHelper.getCustomCassetteBlockSprites(room, entity)
end

return customCassetteBlock