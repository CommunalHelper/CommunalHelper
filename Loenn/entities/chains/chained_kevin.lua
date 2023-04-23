local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local enums = require("consts.celeste_enums")

local chainedKevin = {}

local directions = enums.move_block_directions

chainedKevin.name = "CommunalHelper/ChainedKevin"
chainedKevin.depth = 0
chainedKevin.minimumSize = {24, 24}
chainedKevin.fieldInformation = {
    direction = {
        options = directions,
        editable = false
    },
    chainLength = {
        minimumValue = 0,
        fieldType = "integer"
    }
}

chainedKevin.placements = {}
for _, direction in pairs(directions) do
    table.insert(
        chainedKevin.placements,
        {
            name = string.lower(direction),
            data = {
                width = 24,
                height = 24,
                chillout = false,
                chainLength = 64,
                direction = direction,
                chainOutline = true,
                centeredChain = false,
                chainTexture = "objects/CommunalHelper/chains/chain"
            }
        }
    )
end

local ninePatchOptions = {
    mode = "border",
    borderMode = "random"
}

local kevinColor = {98 / 255, 34 / 255, 43 / 255}
local smallFaceTexture = "objects/crushblock/idle_face"
local giantFaceTexture = "objects/crushblock/giant_block00"

function chainedKevin.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local direction = entity.direction or "Right"
    local chillout = entity.chillout

    local giant = height >= 48 and width >= 48 and chillout
    local faceTexture = giant and giantFaceTexture or smallFaceTexture

    -- set the randomness seed based off this entity
    math.randomseed(x, y)

    local frameTexture = "objects/CommunalHelper/chainedKevin/block" .. direction
    local ninePatch = drawableNinePatch.fromTexture(frameTexture, ninePatchOptions, x, y, width, height)

    local rectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, kevinColor)
    local faceSprite = drawableSprite.fromTexture(faceTexture, entity)

    faceSprite:addPosition(math.floor(width / 2), math.floor(height / 2))

    local sprites = ninePatch:getDrawableSprite()

    local length = entity.chainLength or 64
    if direction == "Up" then
        local rect = drawableRectangle.fromRectangle("line", x, y - length, width, height + length, {1, 1, 1, 0.5})
        table.insert(sprites, 1, rect)
    elseif direction == "Down" then
        local rect = drawableRectangle.fromRectangle("line", x, y, width, height + 64, {1, 1, 1, 0.5})
        table.insert(sprites, 1, rect)
    elseif direction == "Left" then
        local rect = drawableRectangle.fromRectangle("line", x - 64, y, width + length, height, {1, 1, 1, 0.5})
        table.insert(sprites, 1, rect)
    else
        local rect = drawableRectangle.fromRectangle("line", x, y, width + 64, height, {1, 1, 1, 0.5})
        table.insert(sprites, 1, rect)
    end

    table.insert(sprites, 1, rectangle:getDrawableSprite())
    table.insert(sprites, 2, faceSprite)

    return sprites
end

return chainedKevin
