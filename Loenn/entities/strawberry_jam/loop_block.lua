local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local matrix = require("utils.matrix")

local loopBlock = {}

local defaultTexture = "objects/CommunalHelper/strawberryJam/loopBlock/tiles"

loopBlock.name = "CommunalHelper/SJ/LoopBlock"
loopBlock.minimumSize = {24, 24}

loopBlock.fieldInformation = {
    edgeThickness = {
        fieldType = "integer",
        minimumValue = 1
    },
    color = {
        fieldType = "color"
    },
    texture = {
        options = { defaultTexture },
        editable = true,
    }
}

loopBlock.placements = {
    {
        name = "loop_block",
        data = {
            width = 24,
            height = 24,
            edgeThickness = 1,
            color = "FFFFFF",
            texture = defaultTexture,
        }
    }
}

-- generates a 2d array of boolean that represent whether the block has a tile, at each index
local function generateTorusTiles(w, h, thickness)
    return matrix.fromFunction(
        function(i, _, _)
            local x, y = 1 + (i - 1) % w, 1 + math.floor((i - 1) / w)
            return x <= thickness or x > w - thickness
                or y <= thickness or y > h - thickness
        end,
        w, h
    )
end

function loopBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    local w, h = math.floor(width / 8), math.floor(height / 8)

    math.randomseed(x, y)

    local sprites = {}

    local tileset = entity.texture or defaultTexture
    local tiles = generateTorusTiles(w, h, entity.edgeThickness or 1)

    for i = 0, w - 1 do
        for j = 0, h - 1 do
            if tiles:get0(i, j, false) then
                local up, down = tiles:get0(i, j - 1, false), tiles:get0(i, j + 1, false)
                local left, right = tiles:get0(i - 1, j, false), tiles:get0(i + 1, j, false)
                local upleft, upright = tiles:get0(i - 1, j - 1, false), tiles:get0(i + 1, j - 1, false)
                local downleft, downright = tiles:get0(i - 1, j + 1, false), tiles:get0(i + 1, j + 1, false)

                local innerCorner = up and down and left and right
                local full = innerCorner and upleft and upright and downleft and downright

                local tx, ty = 0, 0

                if full then
                    tx, ty = 1 + math.random(0, 1), 7 + math.random(0, 1)
                elseif innerCorner then
                    if not downright then
                        tx, ty = 0, 7
                    elseif not downleft then
                        tx, ty = 3, 7
                    elseif not upright then
                        tx, ty = 0, 8
                    elseif not upleft then
                        tx, ty = 3, 8
                    end
                else
                    if (not up) and down and left and right then
                        tx, ty = 0, 2
                    elseif up and (not down) and left and right then
                        tx, ty = 0, 3
                    elseif up and down and (not left) and right then
                        tx, ty = 3, 2
                    elseif up and down and left and (not right) then
                        tx, ty = 3, 3
                    elseif (not up) and down and (not left) and right then
                        tx, ty = 0, downright and 0 or 4
                    elseif (not up) and down and left and (not right) then
                        tx, ty = 3, downleft and 0 or 4
                    elseif up and (not down) and (not left) and right then
                        tx, ty = 0, upright and 1 or 5
                    elseif up and (not down) and left and (not right) then
                        tx, ty = 3, upleft and 1 or 5
                    elseif (not up) and (not down) and left and right then
                        tx, ty = 0, 6
                    elseif up and down and (not left) and (not right) then
                        tx, ty = 3, 6
                    end

                    tx = tx + math.random(0, 2)
                end

                local tile = drawableSprite.fromTexture(tileset, entity)
                tile:useRelativeQuad(tx * 8, ty * 8, 8, 8)
                tile:addPosition(i * 8, j * 8)
                -- entity color is passed through the entity table,
                -- which we use as option table when calling drawableSprite.fromTexture

                table.insert(sprites, tile)
            end
        end
    end

    return sprites
end

function loopBlock.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    return utils.rectangle(x, y, width, height)
end

return loopBlock
