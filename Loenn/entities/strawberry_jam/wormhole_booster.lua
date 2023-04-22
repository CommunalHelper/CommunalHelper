local drawableSprite = require("structs.drawable_sprite")

local wormholeBooster = {}

wormholeBooster.name = "CommunalHelper/SJ/WormholeBooster"
wormholeBooster.depth = -11000

wormholeBooster.placements = {
    name = "booster",
    data = {
        deathColor = "",
        instantCamera = false
    }
}

wormholeBooster.fieldInformation = {
    deathColor = {
        fieldType = "color"
    }
}

local boosterColor = {120 / 255, 0, 189 / 255}
local texture = "objects/CommunalHelper/strawberryJam/boosterWormhole/boosterWormhole00"

function wormholeBooster.sprite(room, entity)
    local sprite = drawableSprite.fromTexture(texture, entity)
    sprite:setColor(boosterColor)
    return sprite
end

return wormholeBooster
