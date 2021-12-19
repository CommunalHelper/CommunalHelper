local drawableSprite = require("structs.drawable_sprite")
local drawableNinePatch = require("structs.drawable_nine_patch")
local utils = require("utils")

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
                wavedashButtonColor = "5BF75B",
                wavedashButtonPressedColor = "F25EFF",
                dashCornerCorrection = true
            }
        }
    end
end

local function getStationBlockThemeData(size, theme, behavior, wavedash)
    local moon = theme == "moon"
    local pushing = behavior == "pushing"

    local block = (pushing and "alt_" or "") .. (moon and "moon_" or "") .. "block"  .. (wavedash and "_button" or "")
    
    local arrowDir = moon and (pushing and "altMoonArrow" or "moonArrow") or (pushing and "altArrow" or "arrow")
    local arrowFrame = (size <= 16 and "small00") or (size <= 24 and "med00") or "big00"

    return {
        block = "objects/CommunalHelper/stationBlock/blocks/" .. block,
        arrow = "objects/CommunalHelper/stationBlock/" .. arrowDir .. "/" .. arrowFrame,
    }
end

local function addBlockSprites(sprites, themeData, entity, button, x, y, w, h)
    local ninePatch = drawableNinePatch.fromTexture(themeData.block, {}, x, y, w, h)
    local blockSprites = ninePatch:getDrawableSprite()

    for _, sprite in ipairs(blockSprites) do
        table.insert(sprites, sprite)
    end

    if button then
        local tileWidth = math.floor(w / 8) - 1
        local success, r, g, b = utils.parseHexColor(entity.wavedashButtonColor or "ffffff")
        local color = {1.0, 1.0, 1.0}
        if success then
            color = {r, g, b}
        end

        for i = 0, tileWidth do
            local tx = (i == 0 and 0) or (i == tileWidth and 16) or 8
            local sx, sy = x + i * 8, y - 4

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
    local wavedash = entity.allowWavedash or false

    local themeData = getStationBlockThemeData(size, theme, behavior, wavedash)

    addBlockSprites(sprites, themeData, entity, wavedash, x, y, width, height)
    addArrowSprite(sprites, themeData, entity, width, height)

    return sprites
end

return stationBlock
