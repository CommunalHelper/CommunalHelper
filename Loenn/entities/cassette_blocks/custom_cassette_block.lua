local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local customCassetteBlock = {}

local colorNames = {
    ["0 - Blue"] = 0,
    ["1 - Rose"] = 1,
    ["2 - Bright Sun"] = 2,
    ["3 - Malachite"] = 3
}

local colors = {
    "49aaf0",
    "f049be",
    "fcdc3a",
    "38e04e",
}

customCassetteBlock.name = "CommunalHelper/CustomCassetteBlock"
customCassetteBlock.minimumSize = {16, 16}
customCassetteBlock.fieldInformation = {
    index = {
        options = colorNames,
        editable = false,
        fieldType = "integer",
    },
    customColor = {
        fieldType = "color",
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
            customColor = colors[i - 1]
        }
    }
end

function customCassetteBlock.sprite(room, entity)
    return communalHelper.getCustomCassetteBlockSprites(room, entity)
end

return customCassetteBlock