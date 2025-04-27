local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local melvin = {}

melvin.name = "CommunalHelper/Melvin"
melvin.depth = 0
melvin.minimumSize = {24, 24}

melvin.placements = {
    {
        name = "strong",
        data = {
            width = 24,
            height = 24,
            weakTop = false,
            weakRight = false,
            weakBottom = false,
            weakLeft = false,
            spriteDir = "",
            fillColor = "62222b",
            activateParticleColor = "e45f7c",
            attackParticleColor = "ffeb6b",
            attackParticleFadeColor = "d39332"
        }
    },
    {
        name = "weak",
        data = {
            width = 24,
            height = 24,
            weakTop = true,
            weakRight = true,
            weakBottom = true,
            weakLeft = true,
            spriteDir = "",
            fillColor = "62222b",
            activateParticleColor = "e45f7c",
            attackParticleColor = "ffeb6b",
            attackParticleFadeColor = "d39332"
        }
    },
    {
        name = "horiz_weak",
        data = {
            width = 24,
            height = 24,
            weakTop = false,
            weakRight = true,
            weakBottom = false,
            weakLeft = true,
            spriteDir = "",
            fillColor = "62222b",
            activateParticleColor = "e45f7c",
            attackParticleColor = "ffeb6b",
            attackParticleFadeColor = "d39332"
        }
    },
    {
        name = "verti_weak",
        data = {
            width = 24,
            height = 24,
            weakTop = true,
            weakRight = false,
            weakBottom = true,
            weakLeft = false,
            spriteDir = "",
            fillColor = "62222b",
            activateParticleColor = "e45f7c",
            attackParticleColor = "ffeb6b",
            attackParticleFadeColor = "d39332"
        }
    }
}
melvin.fieldOrder = {
    "x", "y", "width", "height",
    "weakTop", "weakBottom", "weakLeft", "weakRight",
    "spriteDir", "fillColor",
    "activateParticleColor", "attackParticleColor", "attackParticleFadeColor"
}
melvin.fieldInformation = {
    fillColor = {
        fieldType = "color"
    },
    activateParticleColor = {
        fieldType = "color"
    },
    attackParticleColor = {
        fieldType = "color"
    },
    attackParticleFadeColor = {
        fieldType = "color"
    },
}

local function addBorderTiles(sprites, x, y, width, height, weakTop, weakBottom, weakLeft, weakRight, spriteDir)
    -- set the randomness seed based off this entity
    math.randomseed(x, y)
    
    local strongTiles = spriteDir .. "/block_strong"
    local weakTiles = spriteDir .. "/block_weak"
    local weakCornersHTiles = spriteDir .. "/corners_weak_h"
    local weakCornersVTiles = spriteDir .. "/corners_weak_v"

    -- CORNERS
    local topleft = strongTiles
    local topright = strongTiles
    local bottomleft = strongTiles
    local bottomright = strongTiles

    -- top-left corner
    if weakLeft and weakTop then
        topleft = weakTiles
    elseif weakLeft and not weakTop then
        topleft = weakCornersHTiles
    elseif not weakLeft and weakTop then
        topleft = weakCornersVTiles
    end

    -- top-right corner
    if weakRight and weakTop then
        topright = weakTiles
    elseif weakRight and not weakTop then
        topright = weakCornersHTiles
    elseif not weakRight and weakTop then
        topright = weakCornersVTiles
    end

    -- bottom-left corner
    if weakLeft and weakBottom then
        bottomleft = weakTiles
    elseif weakLeft and not weakBottom then
        bottomleft = weakCornersHTiles
    elseif not weakLeft and weakBottom then
        bottomleft = weakCornersVTiles
    end

    -- bottom-right corner
    if weakRight and weakBottom then
        bottomright = weakTiles
    elseif weakRight and not weakBottom then
        bottomright = weakCornersHTiles
    elseif not weakRight and weakBottom then
        bottomright = weakCornersVTiles
    end

    -- creating and inserting the tiles
    local topleftSprite = drawableSprite.fromTexture(topleft)
    topleftSprite:useRelativeQuad(0, 0, 8, 8)
    topleftSprite:addPosition(x, y)
    table.insert(sprites, topleftSprite)

    local toprightSprite = drawableSprite.fromTexture(topright)
    toprightSprite:useRelativeQuad(24, 0, 8, 8)
    toprightSprite:addPosition(x + width - 8, y)
    table.insert(sprites, toprightSprite)

    local bottomleftSprite = drawableSprite.fromTexture(bottomleft)
    bottomleftSprite:useRelativeQuad(0, 24, 8, 8)
    bottomleftSprite:addPosition(x, y + height - 8)
    table.insert(sprites, bottomleftSprite)

    local bottomrightSprite = drawableSprite.fromTexture(bottomright)
    bottomrightSprite:useRelativeQuad(24, 24, 8, 8)
    bottomrightSprite:addPosition(x + width - 8, y + height - 8)
    table.insert(sprites, bottomrightSprite)

    -- EDGES
    local top = weakTop and weakTiles or strongTiles
    local bottom = weakBottom and weakTiles or strongTiles
    local left = weakLeft and weakTiles or strongTiles
    local right = weakRight and weakTiles or strongTiles

    local w = math.floor(width / 8) - 2
    local h = math.floor(height / 8) - 2

    -- creating and inserting the tiles
    for tx = 1, w do
        local topSprite = drawableSprite.fromTexture(top)
        topSprite:addPosition(x + tx * 8, y)
        topSprite:useRelativeQuad(8 + math.random(0, 1) * 8, 0, 8, 8)

        local bottomSprite = drawableSprite.fromTexture(bottom)
        bottomSprite:addPosition(x + tx * 8, y + height - 8)
        bottomSprite:useRelativeQuad(8 + math.random(0, 1) * 8, 24, 8, 8)

        table.insert(sprites, topSprite)
        table.insert(sprites, bottomSprite)
    end

    for ty = 1, h do
        local leftSprite = drawableSprite.fromTexture(left)
        leftSprite:addPosition(x, y + ty * 8)
        leftSprite:useRelativeQuad(0, 8 + math.random(0, 1) * 8, 8, 8)

        local rightSprite = drawableSprite.fromTexture(right)
        rightSprite:addPosition(x + width - 8, y + ty * 8)
        rightSprite:useRelativeQuad(24, 8 + math.random(0, 1) * 8, 8, 8)

        table.insert(sprites, leftSprite)
        table.insert(sprites, rightSprite)
    end
end

local function addInsideTiles(sprites, x, y, width, height, spriteDir)
    local insideTiles = spriteDir .. "/inside"

    -- if the block has minimum size, the eye will hide any inside tiles, so, in that case, let's not add any
    if width > 24 or height > 24 then
        for tx = 1, math.floor(width / 8) - 2 do
            for ty = 1, math.floor(height / 8) - 2 do
                local sprite = drawableSprite.fromTexture(insideTiles)
                sprite:addPosition(x + tx * 8 + math.random(-1, 1), y + ty * 8 + math.random(-1, 1))
                sprite:useRelativeQuad(0, 0, 8, 8)
                table.insert(sprites, sprite)
            end
        end
    end
end

function melvin.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    local weakTop = entity.weakTop
    local weakBottom = entity.weakBottom
    local weakLeft = entity.weakLeft
    local weakRight = entity.weakRight
    
    local spriteDir = (entity.spriteDir or "") ~= "" and entity.spriteDir or "objects/CommunalHelper/melvin"
    local fillColor = utils.getColor(entity.fillColor or { 98 / 255, 34 / 255, 43 / 255 })

    local sprites = {}

    local bgRect = drawableRectangle.fromRectangle("fill", x + 1, y + 1, width - 2, height - 2, fillColor)
    table.insert(sprites, bgRect)

    addBorderTiles(sprites, x, y, width, height, weakTop, weakBottom, weakLeft, weakRight, spriteDir)
    addInsideTiles(sprites, x, y, width, height, spriteDir)

    local eye = spriteDir .. "/eye/idle_small00"
    local eyeSprite = drawableSprite.fromTexture(eye, entity)
    eyeSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    table.insert(sprites, eyeSprite)

    return sprites
end

return melvin
