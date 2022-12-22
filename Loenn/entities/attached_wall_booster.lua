local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local attachedWallBooster = {}

attachedWallBooster.name = "CommunalHelper/AttachedWallBooster"
attachedWallBooster.depth = 1999
attachedWallBooster.canResize = {false, true}

attachedWallBooster.placements = {
    {
        name = "right",
        placementType = "rectangle",
        data = {
            height = 8,
            left = true,
            legacyBoost = true,
            coreModeBehavior = "ToggleDefaultHot"
        }
    },
    {
        name = "left",
        placementType = "rectangle",
        data = {
            height = 8,
            left = false,
            legacyBoost = true,
            coreModeBehavior = "ToggleDefaultHot"
        }
    }
}

attachedWallBooster.fieldInformation = {
    coreModeBehavior = {
        options = {
            {"Toggle (Default to Hot)", "ToggleDefaultHot"},
            {"Toggle (Default to Cold)", "ToggleDefaultCold"},
            {"Always Hot", "AlwaysHot"},
            {"Always Cold", "AlwaysCold"}
        },
        editable = false
    }
}

local fireTopTexture = "objects/wallBooster/fireTop00"
local fireMiddleTexture = "objects/wallBooster/fireMid00"
local fireBottomTexture = "objects/wallBooster/fireBottom00"

local iceTopTexture = "objects/wallBooster/iceTop00"
local iceMiddleTexture = "objects/wallBooster/iceMid00"
local iceBottomTexture = "objects/wallBooster/iceBottom00"

local function getWallTextures(renderCold)
    if renderCold then
        return iceTopTexture, iceMiddleTexture, iceBottomTexture

    else
        return fireTopTexture, fireMiddleTexture, fireBottomTexture
    end
end

local topTextureAlt = "objects/CommunalHelper/attachedWallBooster/fireTop00"
local bottomTextureAlt = "objects/CommunalHelper/attachedWallBooster/fireBottom00"

function attachedWallBooster.sprite(room, entity)
    local sprites = {}

    local left = entity.left
    local height = entity.height or 8
    local tileHeight = math.floor(height / 8)
    local offsetX = left and 0 or 8
    local scaleX = left and 1 or -1

    local renderCold = entity.notCoreMode or entity.coreModeBehavior == "ToggleDefaultCold" or entity.coreModeBehavior == "AlwaysCold"
    local topTexture, middleTexture, bottomTexture = getWallTextures(renderCold)

    for i = 2, tileHeight - 1 do
        local middleSprite = drawableSprite.fromTexture(middleTexture, entity)

        middleSprite:addPosition(offsetX, (i - 1) * 8)
        middleSprite:setScale(scaleX, 1)
        middleSprite:setJustification(0.0, 0.0)

        table.insert(sprites, middleSprite)
    end

    local legacyBoost = entity.legacyBoost
    local topSprite = drawableSprite.fromTexture((legacyBoost or renderCold) and topTexture or topTextureAlt, entity)
    local bottomSprite = drawableSprite.fromTexture((legacyBoost or renderCold) and bottomTexture or bottomTextureAlt, entity)

    topSprite:addPosition(offsetX, 0)
    topSprite:setScale(scaleX, 1)
    topSprite:setJustification(0.0, 0.0)

    bottomSprite:addPosition(offsetX, (tileHeight - 1) * 8)
    bottomSprite:setScale(scaleX, 1)
    bottomSprite:setJustification(0.0, 0.0)

    table.insert(sprites, topSprite)
    table.insert(sprites, bottomSprite)

    return sprites
end

function attachedWallBooster.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, 8, entity.height or 8)
end

return attachedWallBooster
