local drawableSprite = require("structs.drawable_sprite")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local cassetteMoveBlock = {}

local directions = {"Up", "Down", "Left", "Right"}

cassetteMoveBlock.name = "CommunalHelper/CassetteMoveBlock"
cassetteMoveBlock.minimumSize = {16, 16}
cassetteMoveBlock.fieldInformation = {
    index = {
        fieldType = "integer",
    }
}

cassetteMoveBlock.placements = {}
for i = 1, 4 do
    cassetteMoveBlock.placements[i] = {
        name = string.format("cassette_block_%s", i - 1),
        data = {
            index = i - 1,
            tempo = 1.0,
            width = 16,
            height = 16,
            customColor = "",
            direction = "Right",
            moveSpeed = 60.0
        }
    }
end

local arrowTextures = {
    up = "objects/CommunalHelper/cassetteMoveBlock/arrow02",
    left = "objects/CommunalHelper/cassetteMoveBlock/arrow04",
    right = "objects/CommunalHelper/cassetteMoveBlock/arrow00",
    down = "objects/CommunalHelper/cassetteMoveBlock/arrow06"
}

function cassetteMoveBlock.sprite(room, entity)
    local sprites = communalHelper.getCustomCassetteBlockSprites(room, entity)

    local width, height = entity.width or 16, entity.height or 16
    local color = communalHelper.getCustomCassetteBlockColor(entity)

    local direction = string.lower(entity.direction)
    local arrowTexture = arrowTextures[direction] or arrowTextures["right"]
    
    local arrowSprite = drawableSprite.fromTexture(arrowTexture, entity)
    arrowSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    arrowSprite:setColor(color)
    arrowSprite.depth = -11

    table.insert(sprites, arrowSprite)

    return sprites
end

return cassetteMoveBlock