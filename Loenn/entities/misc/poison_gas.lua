local utils = require('utils')
local drawableSprite = require('structs.drawable_sprite')

local poisonGas = {
    name = "CommunalHelper/PoisonGas",
    depth = -101
}
poisonGas.placements = {
    name = "main",
    data = {
        spritePath = "objects/CommunalHelper/poisonGas/gas00",
        radius = 48
    }
}
poisonGas.fieldInformation = {
    spritePath = {fieldType=path, allowFolders = false, allowFiles = true},
}

function poisonGas.selection(room, entity)
    return utils.rectangle(entity.x - entity.radius, entity.y - entity.radius, entity.radius*2, entity.radius*2)
end

function poisonGas.sprite(room, entity) 
    local sprite = drawableSprite.fromTexture(entity.spritePath, entity)
    sprite:setScale(entity.radius / 24, entity.radius / 24)
    return sprite
end

return poisonGas
