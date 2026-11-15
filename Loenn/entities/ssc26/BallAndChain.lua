local drawableSprite = require("structs.drawable_sprite")

local ballandchain = {}

ballandchain.name = "CommunalHelper/WeightAndChain"
ballandchain.depth = 100
ballandchain.placements = {
    name = "weightandchain",
	data = {
		intro = false,
	}
}
ballandchain.offset = { 0, -8 }

function ballandchain.sprite(room, entity)
    local sprite = drawableSprite.fromTexture("objects/CommunalHelper/weightAndChain/weight", entity)
    sprite.y += -10
    return sprite
end

return ballandchain
