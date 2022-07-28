local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local redlessBerry = {}

redlessBerry.name = "CommunalHelper/RedlessBerry"
redlessBerry.depth = -100

redlessBerry.placements = {
    {
        name = "redless_berry",
        data = {
            persistent = false,
            winged = false
        }
    }
}

local fruitTexture = "collectables/CommunalHelper/recolorableBerry/fruit00"
local leavesTexture = "collectables/CommunalHelper/recolorableBerry/leaves00"
local fruitWingedTexture = "collectables/CommunalHelper/recolorableBerry/fruit_wings00"
local leavesWingedTexture = "collectables/CommunalHelper/recolorableBerry/leaves_wings00"

-- will be changed in the future
local color = {253 / 255, 191 / 255, 71 / 255, 255 / 255}

function redlessBerry.sprite(room, entity)
    local winged = entity.winged
    local offset = winged and 1 or 0

    local sprites = {}

    local fruit = drawableSprite.fromTexture(winged and fruitWingedTexture or fruitTexture, entity)
    fruit.color = color
    fruit:addPosition(0, offset)
    table.insert(sprites, fruit)

    local leaves = drawableSprite.fromTexture(winged and leavesWingedTexture or leavesTexture, entity)
    leaves:addPosition(0, offset)
    table.insert(sprites, leaves)

    return sprites
end

function redlessBerry.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0

    return utils.rectangle(x - 5, y - 7, 10, 13)
end

return redlessBerry
