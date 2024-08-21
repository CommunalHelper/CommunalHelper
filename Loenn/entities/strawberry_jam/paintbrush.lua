local drawableSprite = require('structs.drawable_sprite')
local utils = require('utils')

local minimum_size = 16
local selection_thickness = 8
local sprite_path = "objects/StrawberryJam2021/paintbrush"

local orientationNames = { "Up", "Right", "Down", "Left" }
local orientationIndices = { Up = 0, Right = 1, Down = 2, Left = 3 }

local function getSpriteName(entity, large)
    local prefix = entity.cassetteIndex == 0 and "blue" or "pink"
    return sprite_path.."/"..prefix..(large and "/brush1" or "/backbrush1")
end

local function createPlacement(orientationIndex, cassetteIndex)
    local orientationName = orientationNames[orientationIndex + 1]
    return {
        name = string.lower(orientationName).."_"..cassetteIndex,
        data = {
            x = 0,
            y = 0,
            width = minimum_size,
            height = minimum_size,
            orientation = orientationName,
            cassetteIndex = cassetteIndex,
            killPlayer = true,
            collideWithSolids = true,
            halfLength = false,
        },
    }
end

local function createPlacements()
    local placements = { }
    for orientation = 0,3 do
        for cassetteIndex = 0,3 do
            table.insert(placements, createPlacement(orientation, cassetteIndex))
        end
    end
    return placements
end

local paintbrush = {
    name = "CommunalHelper/SJ/Paintbrush",
    depth = -8500,
    placements = createPlacements(),
    texture = "objects/CommunalHelper/strawberryJam/paintbrush/blue/brush1",
    fieldInformation = {
        orientation = {
            editable = false,
            options = orientationNames,
        },
        length = {
            fieldType = "integer",
        },
        cassetteIndex = {
            editable = false,
            options = {
                -- we only support blue and pink for now since that's the only sprites we have
                ["0 - Blue"] = 0,
                ["1 - Rose"] = 1,
            },
        }
    },
}

function paintbrush.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local orientationIndex = orientationIndices[entity.orientation] or 0
    local vertical = orientationIndex % 2 == 0
    local width = vertical and entity.width or selection_thickness
    local height = vertical and selection_thickness or entity.height
    if orientationIndex == 0 then y = y - height end
    if orientationIndex == 3 then x = x - width end
    return utils.rectangle(x, y, width, height)
end

function paintbrush.minimumSize(room, entity)
    local orientationIndex = orientationIndices[entity.orientation] or 0
    local vertical = orientationIndex % 2 == 0
    return vertical and { minimum_size, 0 } or { 0, minimum_size }
end

function paintbrush.canResize(room, entity)
    local orientationIndex = orientationIndices[entity.orientation] or 0
    local vertical = orientationIndex % 2 == 0
    return { vertical, not vertical }
end

function paintbrush.sprite(room, entity)
    -- get some data
    local orientationIndex = orientationIndices[entity.orientation] or 0
    local vertical = orientationIndex % 2 == 0
    local size = vertical and entity.width or entity.height
    local tiles = math.ceil(size / 8)
    
    -- add background brushes
    local largeTexture = getSpriteName(entity, true)
    local smallTexture = getSpriteName(entity, false)
    
    local function configureSprite(sprite, i)
        if orientationIndex == 0 or orientationIndex == 3 then
            sprite:setScale(-1, 1)
        end
        sprite.rotation = vertical and math.pi / 2 or 0
        sprite:setJustification(0, 0.5)
        sprite:addPosition(vertical and i * 8 or 0, (not vertical) and i * 8 or 0)
    end
    
    -- generate sprites
    local sprites = { }
    for i = 2,tiles-1,2 do
        local smallSprite = drawableSprite.fromTexture(smallTexture, entity)
        configureSprite(smallSprite, i)
        table.insert(sprites, smallSprite)
    end
    for i = 1,tiles-1,2 do
        local largeSprite = drawableSprite.fromTexture(largeTexture, entity)
        configureSprite(largeSprite, i)
        table.insert(sprites, largeSprite)
    end
    
    return sprites
end

return paintbrush
