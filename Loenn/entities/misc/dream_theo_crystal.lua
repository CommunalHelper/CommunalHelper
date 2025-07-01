local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local dreamTheoCrystal = {}

dreamTheoCrystal.name = "CommunalHelper/DreamTheoCrystal"
dreamTheoCrystal.depth = 100
dreamTheoCrystal.placements = {
    name = "dream_theo_crystal",
}

-- Offset is from sprites.xml, not justifications
local offsetY = -10
local texture = "objects/CommunalHelper/dreamTheoCrystal/theo"

function dreamTheoCrystal.sprite(room, entity)
    local sprite = drawableSprite.fromTexture(texture, entity)
    sprite.y = sprite.y + offsetY

    return sprite
end

function dreamTheoCrystal.rectangle(room, entity)
    return utils.rectangle(entity.x - 11, entity.y - 21, 21, 22)
end

return dreamTheoCrystal
