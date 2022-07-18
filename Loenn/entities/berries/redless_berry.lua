local drawableSprite = require("structs.drawable_sprite")
local utils = require "utils"

local redlessBerry = {}

redlessBerry.name = "CommunalHelper/RedlessBerry"
redlessBerry.depth = -100

redlessBerry.placements = {
    {
        name = "redless_berry"
    }
}

local fruitTexture = "collectables/CommunalHelper/recolorableBerry/fruit00"
local leavesTexture = "collectables/CommunalHelper/recolorableBerry/leaves00"

-- will be changed in the future
local color = {1.0, 1.0, 1.0, 1.0}

function redlessBerry.sprite(room, entity)
    local sprites = {}

    local fruit = drawableSprite.fromTexture(fruitTexture, entity)
    fruit.color = color
    table.insert(sprites, fruit)

    local leaves = drawableSprite.fromTexture(leavesTexture, entity)
    table.insert(sprites, leaves)

    return sprites
end

function redlessBerry.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0

    return utils.rectangle(x - 5, y - 7, 10, 13)
end

return redlessBerry
