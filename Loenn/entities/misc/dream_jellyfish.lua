local drawing = require("utils.drawing")
local drawableLine = require("structs.drawable_line")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local dreamJellyfish = {}

dreamJellyfish.name = "CommunalHelper/DreamJellyfish"
dreamJellyfish.depth = -5

dreamJellyfish.placements = {
    {
        name = "normal",
        data = {
            bubble = false,
            tutorial = false,
            fixedInvertedColliderOffset = true,
            oneUse = false,
            quickDestroy = false,
            refillOnFloorSprings = true,
            refillOnWallSprings = true,
        }
    },
    {
        name = "floating",
        data = {
            bubble = true,
            tutorial = false,
            fixedInvertedColliderOffset = true,
            oneUse = false,
            quickDestroy = false,
            refillOnFloorSprings = true,
            refillOnWallSprings = true,
        }
    },
    {
        name = "oneUse",
        data = {
            bubble = false,
            tutorial = false,
            fixedInvertedColliderOffset = true,
            oneUse = true,
            quickDestroy = false,
            refillOnFloorSprings = true,
            refillOnWallSprings = true,
        }
    }
}

local texture = "objects/CommunalHelper/dreamJellyfish/jello"

function dreamJellyfish.sprite(room, entity)
    if entity.bubble then
        local x, y = entity.x or 0, entity.y or 0
        local points = drawing.getSimpleCurve({ x - 11, y - 1 }, { x + 11, y - 1 }, { x - 0, y - 6 })
        local lineSprites = drawableLine.fromPoints(points):getDrawableSprite()
        local jellySprite = drawableSprite.fromTexture(texture, entity)

        table.insert(lineSprites, 1, jellySprite)

        return lineSprites
    else
        return drawableSprite.fromTexture(texture, entity)
    end
end

function dreamJellyfish.rectangle(room, entity)
    return utils.rectangle(entity.x - 14, entity.y - 15, 30, 19)
end

return dreamJellyfish
