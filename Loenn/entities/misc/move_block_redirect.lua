local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local enums = require("consts.celeste_enums")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local moveBlockRedirect = {}

local operations = {"Add", "Subtract", "Multiply"}
local directions = enums.move_block_directions

moveBlockRedirect.name = "CommunalHelper/MoveBlockRedirect"
moveBlockRedirect.depth = -8500
moveBlockRedirect.minimumSize = {16, 16}
moveBlockRedirect.fieldInformation = {
    direction = {
        options = directions,
        editable = false
    },
    operation = {
        options = operations,
        editable = false
    },
    modifier = {
        minimumValue = 0.0
    },
    overrideColor = {
        fieldType = "color"
    },
    overrideUsedColor = {
        fieldType = "color"
    }
}

moveBlockRedirect.placements = {
    {
        name = "normal",
        data = {
            width = 16,
            height = 16,
            direction = "Up",
            fastRedirect = false,
            deleteBlock = false,
            oneUse = false,
            modifier = 0.0,
            operation = "Add"
        }
    },
    {
        name = "one_use",
        data = {
            width = 16,
            height = 16,
            direction = "Up",
            fastRedirect = false,
            deleteBlock = false,
            oneUse = true,
            modifier = 0.0,
            operation = "Add"
        }
    },
    {
        name = "delete",
        data = {
            width = 16,
            height = 16,
            direction = "Up",
            fastRedirect = false,
            deleteBlock = true,
            oneUse = false,
            modifier = 0.0,
            operation = "Add"
        }
    },
    {
        name = "reskinnable",
        data = {
            width = 16,
            height = 16,
            direction = "Up",
            fastRedirect = false,
            deleteBlock = false,
            oneUse = false,
            modifier = 0.0,
            operation = "Add",
            reskinFolder = "",
            overrideColor = "ffffff",
            overrideUsedColor = "ffffff"
        }
    }
}

local defaultColor = {251 / 255, 206 / 255, 54 / 255, 255 / 255}
local deleteColor = {204 / 255, 37 / 255, 65 / 255, 255 / 255}
local fastColor = {41 / 255, 195 / 255, 47 / 255, 255 / 255}
local slowColor = {28 / 255, 91 / 255, 179 / 255, 255 / 255}

local defaultPath = "objects/CommunalHelper/moveBlockRedirect/"
local frameTexture = "objects/CommunalHelper/moveBlockRedirect/block"

local rotations = {
    ["Up"] = math.pi * 1.5,
    ["Right"] = 0,
    ["Down"] = math.pi / 2,
    ["Left"] = math.pi
}

local function getMoveBlockRedirectThemeData(entity)
    local path = defaultPath
    local reskinFolder = entity.reskinFolder or ""
    if reskinFolder ~= "" then
        if reskinFolder[string.len(reskinFolder)] ~= "/" then
            reskinFolder = reskinFolder * "/"
        end
        path = "objects/" * reskinFolder
    end

    local overrideColorHex = entity.overrideColor or ""

    local deleteBlock = entity.deleteBlock
    if deleteBlock then
        return {icon = path .. "x", color = communalHelper.hexToColor(overrideColorHex, deleteColor)}
    end

    local operation = entity.operation or "Add"
    local modifier = entity.modifier or 0

    if operation == "Add" then
        if modifier ~= 0.0 then
            return {icon = path .. "fast", color = communalHelper.hexToColor(overrideColorHex, fastColor)}
        end
    elseif operation == "Subtract" then
        if modifier ~= 0.0 then
            return {icon = path .. "slow", color = communalHelper.hexToColor(overrideColorHex, slowColor)}
        end
    elseif operation == "Multiply" then
        if modifier == 0.0 then
            return {icon = path .. "x", color = communalHelper.hexToColor(overrideColorHex, deleteColor)}
        elseif modifier > 1.0 then
            return {icon = path .. "fast", color = communalHelper.hexToColor(overrideColorHex, fastColor)}
        elseif modifier < 1.0 then
            return {icon = path .. "slow", color = communalHelper.hexToColor(overrideColorHex, slowColor)}
        end
    end

    return {icon = path .. "arrow", color = communalHelper.hexToColor(overrideColorHex, defaultColor)}
end

function moveBlockRedirect.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16

    local themeData = getMoveBlockRedirectThemeData(entity)

    -- drawable_nine_patch won't apply the given color as of right now (day of plugin implementation)
    -- this commit will fix this problem once it is included in a newer version of Lönn:
    -- https://github.com/CelestialCartographers/Loenn/commit/0389b59e2b0f2a50b50b691eb6141d4f53c0350f
    local ninePatchOptions = {
        mode = "border",
        color = themeData.color
    }
    local frameSprites = drawableNinePatch.fromTexture(frameTexture, ninePatchOptions, x - 8, y - 8, width + 16, height + 16):getDrawableSprite()

    local sprites = frameSprites

    local direction = entity.direction or "Right"
    local rot = rotations[direction]

    local iconSprite = drawableSprite.fromTexture(themeData.icon, {rotation = rot})
    iconSprite:addPosition(x + math.floor(width / 2), y + math.floor(height / 2))
    iconSprite:setColor(themeData.color)
    table.insert(sprites, iconSprite)

    return sprites
end

return moveBlockRedirect
