local drawableSprite = require("structs.drawable_sprite")
local enums = require("consts.celeste_enums")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local cassetteMoveBlock = {}

local colorNames = communalHelper.cassetteBlockColorNames
local colors = communalHelper.cassetteBlockHexColors

local moveSpeeds = {
    ["Slow"] = 60.0,
    ["Fast"] = 75.0
}

cassetteMoveBlock.name = "CommunalHelper/CassetteMoveBlock"
cassetteMoveBlock.minimumSize = { 16, 16 }
cassetteMoveBlock.fieldInformation = {
    index = {
        options = colorNames,
        editable = false,
        fieldType = "integer"
    },
    customColor = {
        fieldType = "color"
    },
    direction = {
        options = enums.move_block_directions,
        editable = false
    },
    moveSpeed = {
        options = moveSpeeds,
        minimumValue = 0.0
    },
    tempo = {
        minimumValue = 0.0
    },
    sideAlpha = {
        minimumValue = 0.0,
        maximumValue = 1.0
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
            customColor = colors[i],
            direction = "Right",
            moveSpeed = 60.0,
            oldConnectionBehavior = false,
            held = false,
            crashTime = 0.15,
            regenTime = 3.0,
            shakeOnCollision = true,
            noDebris = false,
            spritePath = "",
            sideAlpha = 1.0,
            ignore="",
        }
    }
end

local function arrowTextures(entity) 
    return {
        up = communalHelper.suffixFromPathOrDefault(entity.spritePath, "/arrow02", "objects/CommunalHelper/cassetteMoveBlock/arrow02"),
        left = communalHelper.suffixFromPathOrDefault(entity.spritePath, "/arrow04", "objects/CommunalHelper/cassetteMoveBlock/arrow04"),
        right = communalHelper.suffixFromPathOrDefault(entity.spritePath, "/arrow00", "objects/CommunalHelper/cassetteMoveBlock/arrow00"),
        down = communalHelper.suffixFromPathOrDefault(entity.spritePath, "/arrow06", "objects/CommunalHelper/cassetteMoveBlock/arrow06")
    }
end

function cassetteMoveBlock.sprite(room, entity)
    local sprites = communalHelper.getCustomCassetteBlockSprites(room, entity, true, entity.oldConnectionBehavior)

    local width, height = entity.width or 16, entity.height or 16
    local color = communalHelper.getCustomCassetteBlockColor(entity)

    local direction = string.lower(entity.direction)
    local arrowTexture = arrowTextures(entity)[direction] or arrowTextures(entity)["right"]

    local arrowSprite = drawableSprite.fromTexture(arrowTexture, entity)
    arrowSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    arrowSprite:setColor(color)
    arrowSprite.depth = -11

    table.insert(sprites, arrowSprite)

    return sprites
end

return cassetteMoveBlock
