local drawableSprite = require("structs.drawable_sprite")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local heartGemShards = {}

heartGemShards.name = "CommunalHelper/AdventureHelper/CustomCrystalHeart"
heartGemShards.depth = -2000000
heartGemShards.nodeLimits = {1, -1}
heartGemShards.nodeVisibility = "always"
heartGemShards.nodeLineRenderType = "fan"
heartGemShards.fieldInformation = {
    color = {
        fieldType = "color"
    }
}

heartGemShards.placements = {
    {
        name = "heart_gem_shards",
        data = {
            color = "00a81f",
            path = "",
        }
    }
}

function heartGemShards.sprite(room, entity)
    local path = entity.path or ""

    if path == "" then
        local sprite = drawableSprite.fromTexture("collectables/AdventureHelper/RecolorHeart/00", entity)
        sprite:setColor(communalHelper.hexToColor(entity.color))
        return {
            drawableSprite.fromTexture("collectables/AdventureHelper/RecolorHeart_Outline/00", entity),
            sprite
        }
    end

    local texture = "collectables/heartGem/3/00"
    if path == "heartgem0" then
        texture = "collectables/heartGem/0/00"
    elseif path == "heartgem1" then
        texture = "collectables/heartGem/1/00"
    elseif path == "heartgem2" then
        texture = "collectables/heartGem/2/00"
    elseif path == "heartgem3" then
        texture = "collectables/heartGem/3/00"
    end

    return drawableSprite.fromTexture(texture, entity)
end

function heartGemShards.nodeSprite(room, entity, node, nodeIndex, viewport)
    local outline = drawableSprite.fromTexture("collectables/CommunalHelper/heartGemShard/shard_outline0" .. (nodeIndex % 3), node)
    local sprite = drawableSprite.fromTexture("collectables/CommunalHelper/heartGemShard/shard0" .. (nodeIndex % 3), node)
    sprite:setColor(communalHelper.hexToColor(entity.color))

    return {outline, sprite}
end

return heartGemShards
