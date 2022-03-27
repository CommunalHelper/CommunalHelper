local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local enums = require("consts.celeste_enums")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dreamMoveBlock = {}

local moveSpeeds = {
    ["Slow"] = 60.0,
    ["Fast"] = 75.0,
}

dreamMoveBlock.name = "CommunalHelper/DreamMoveBlock"
dreamMoveBlock.minimumSize = {16, 16}
dreamMoveBlock.fieldInformation = {
    direction = {
        options = enums.move_block_directions,
        editable = false,
    },
    moveSpeed = {
        options = moveSpeeds,
    }
}

function dreamMoveBlock.depth(room, entity)
    return entity.below and 5000 or -11000
end

dreamMoveBlock.placements = {
    {
        name = "dream_move_block",
        data = {
            width = 16,
            height = 16,
            featherMode = false,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
            direction = "Right",
            moveSpeed = 60.0,
            noCollide = false,
        }
    }
}

local arrowTextures = {
    up = "objects/CommunalHelper/dreamMoveBlock/arrow02",
    left = "objects/CommunalHelper/dreamMoveBlock/arrow04",
    right = "objects/CommunalHelper/dreamMoveBlock/arrow00",
    down = "objects/CommunalHelper/dreamMoveBlock/arrow06"
}

function dreamMoveBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local feather = entity.featherMode

    local sprites = {}

    table.insert(sprites, communalHelper.getCustomDreamBlockSprites(x, y, width, height, feather))
    
    local direction = string.lower(entity.direction)
    local arrowTexture = arrowTextures[direction] or arrowTextures["right"]

    local arrowSprite = drawableSprite.fromTexture(arrowTexture, entity)
    arrowSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    arrowSprite.depth = -11
    table.insert(sprites, arrowSprite)

    return sprites
end

function dreamMoveBlock.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16

    return utils.rectangle(x, y, width, height)
end

return dreamMoveBlock
