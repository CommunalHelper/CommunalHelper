local dreamSpriteColorController = {}

dreamSpriteColorController.name = "CommunalHelper/DreamSpriteColorController"
dreamSpriteColorController.texture = "objects/CommunalHelper/dreamSpriteColorController/icon.png"

dreamSpriteColorController.fieldInformation = {
    lineColor = {
        fieldType = "color",
    },
    backColor = {
        fieldType = "color",
    },
}
dreamSpriteColorController.placements = {
    {
        name = "dreamSpriteColorController",
        data = {
            lineColor = "ffffff",
            backColor = "000000",
        },
    }
}

local defaultColors = { "ffff00", "00ffff", "ff00ff" }
for idx, placement in pairs(dreamSpriteColorController.placements) do
    for i = 0, 8 do
        placement.data["dreamColor" .. i] = defaultColors[i % #defaultColors + 1]
    end
end

for i = 0, 8 do
    dreamSpriteColorController.fieldInformation["dreamColor" .. i] = {
        fieldType = "color",
    }
end

return dreamSpriteColorController
