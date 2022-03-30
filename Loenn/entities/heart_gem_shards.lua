local drawableSprite = require("structs.drawable_sprite")

local heartGemShards = {}

heartGemShards.name = "CommunalHelper/CrystalHeart"
heartGemShards.depth = -2000000
heartGemShards.nodeLimits = {1, -1}
heartGemShards.nodeVisibility = "always"
heartGemShards.nodeLineRenderType = "fan"

heartGemShards.placements = {
    {
        name = "heart_gem_shards",
        data = {
            removeCameraTriggers = false
        }
    }
}

heartGemShards.texture = "collectables/heartGem/ghost00"

function heartGemShards.nodeSprite(room, entity, node, nodeIndex, viewport)
    return {
        drawableSprite.fromTexture("collectables/CommunalHelper/heartGemShard/shard_outline0" .. (nodeIndex % 3), node),
        drawableSprite.fromTexture("collectables/CommunalHelper/heartGemShard/shard0" .. (nodeIndex % 3), node)
    }
end

return heartGemShards
