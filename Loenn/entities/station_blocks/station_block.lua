local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local stationBlock = {}

local themes = {"Normal", "Moon"}
local behaviors = {"Pushing", "Pulling"}

stationBlock.name = "CommunalHelper/StationBlock"
stationBlock.depth = -9999
stationBlock.minimumSize = {16, 16}
stationBlock.fieldInformation = {
    wavedashButtonColor = {
        fieldType = "color"
    },
    wavedashButtonPressedColor = {
        fieldType = "color"
    },
    theme = {
        options = themes,
        editable = false
    },
    behavior = {
        options = behaviors,
        editable = false
    },
    speedFactor = {
        minimumValue = 0.0
    }
}

stationBlock.placements = {}
for i, theme in ipairs(themes) do
    for j, behavior in ipairs(behaviors) do
        local idx = 2 * i + j - 2
        stationBlock.placements[idx] = {
            name = string.lower(theme .. "_" .. behavior),
            placementType = "rectangle",
            data = {
                width = 16,
                height = 16,
                theme = theme,
                behavior = behavior,
                customBlockPath = "",
                customArrowPath = "",
                customTrackPath = "",
                speedFactor = 1.0,
                allowWavedash = false,
                allowWavedashBottom = false,
                wavedashButtonColor = "5BF75B",
                wavedashButtonPressedColor = "F25EFF",
                dashCornerCorrection = true
            }
        }
    end
end

local function getStationBlockThemeData(size, theme, behavior, wavedash, wavedashBottom)
    local moon = theme == "moon"
    local pushing = behavior == "pushing"

    local button = (wavedash and wavedashBottom and "_button_both") or (wavedashBottom and "_button_bottom") or (wavedash and "_button")
    local block = (pushing and "alt_" or "") .. (moon and "moon_" or "") .. "block" .. button

    local arrowDir = moon and (pushing and "altMoonArrow" or "moonArrow") or (pushing and "altArrow" or "arrow")
    local arrowFrame = (size <= 16 and "small00") or (size <= 24 and "med00") or "big00"

    return {
        block = "objects/CommunalHelper/stationBlock/blocks/" .. block,
        arrow = "objects/CommunalHelper/stationBlock/" .. arrowDir .. "/" .. arrowFrame
    }
end

local function addBlockSprites(sprites, themeData, button, bottomButton, x, y, w, h, wavedashButtonColor)
    local ninePatch = drawableNinePatch.fromTexture(themeData.block, {}, x, y, w, h)
    table.insert(sprites, ninePatch)

    if button or bottomButton then
        local tileWidth = math.floor(w / 8) - 1
        local color = communalHelper.hexToColor(wavedashButtonColor)

        for i = 0, tileWidth do
            local tx = (i == 0 and 0) or (i == tileWidth and 16) or 8
            local sx, sy = x + i * 8, y - 4

            if button then
                local buttonOutlineSprite = drawableSprite.fromTexture("objects/CommunalHelper/stationBlock/button_outline")
                buttonOutlineSprite:setPosition(sx, sy)
                buttonOutlineSprite:useRelativeQuad(tx, 0, 8, 8)

                local buttonSprite = drawableSprite.fromTexture("objects/CommunalHelper/stationBlock/button")
                buttonSprite:setColor(color)
                buttonSprite:setPosition(sx, sy)
                buttonSprite:useRelativeQuad(tx, 0, 8, 8)
                table.insert(sprites, buttonOutlineSprite)
                table.insert(sprites, buttonSprite)
            end

            if bottomButton then
                local buttonOutlineSprite = drawableSprite.fromTexture("objects/CommunalHelper/stationBlock/button_outline")
                buttonOutlineSprite:setPosition(sx, sy + h + 8)
                buttonOutlineSprite:useRelativeQuad(tx, 0, 8, 8)
                buttonOutlineSprite:setScale(1, -1)

                local buttonSprite = drawableSprite.fromTexture("objects/CommunalHelper/stationBlock/button")
                buttonSprite:setColor(color)
                buttonSprite:setPosition(sx, sy + h + 8)
                buttonSprite:useRelativeQuad(tx, 0, 8, 8)
                buttonSprite:setScale(1, -1)
                table.insert(sprites, buttonOutlineSprite)
                table.insert(sprites, buttonSprite)
            end
        end
    end
end

local function addArrowSprite(sprites, themeData, entity, w, h)
    local arrow = drawableSprite.fromTexture(themeData.arrow, entity)
    arrow:addPosition(math.floor(w / 2), math.floor(h / 2))
    table.insert(sprites, arrow)
end

function stationBlock.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local size = math.min(width, height)

    local theme = string.lower(entity.theme or "Normal")
    local behavior = string.lower(entity.behavior or "Pulling")
    local wavedash, wavedashBottom = entity.allowWavedash, entity.allowWavedashBottom
    local wavedashButtonColor = entity.wavedashButtonColor or "ffffff"

    local themeData = getStationBlockThemeData(size, theme, behavior, wavedash, wavedashBottom)

    addBlockSprites(sprites, themeData, wavedash, wavedashBottom, x, y, width, height, wavedashButtonColor)
    addArrowSprite(sprites, themeData, entity, width, height)

    return sprites
end

return stationBlock
