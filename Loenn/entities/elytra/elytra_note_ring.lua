local drawableLine = require "structs.drawable_line"
local drawableSprite = require "structs.drawable_sprite"
local utils = require "utils"

local elytraNoteRing = {}

elytraNoteRing.name = "CommunalHelper/ElytraNoteRing"

elytraNoteRing.nodeLimits = {1, 1}
elytraNoteRing.nodeVisibility = "always"
elytraNoteRing.nodeLineRenderType = "line"

elytraNoteRing.fieldInformation = {
    semitone = {
        fieldType = "integer",
        editable = true,
        options = {
            ["00 = C4"] = 0,
            ["01 = C#4"] = 1,
            ["02 = D4"] = 2,
            ["03 = Eb4"] = 3,
            ["04 = E4"] = 4,
            ["05 = F4"] = 5,
            ["06 = F#4"] = 6,
            ["07 = G4"] = 7,
            ["08 = Ab4"] = 8,
            ["09 = A4"] = 9,
            ["10 = Bb4"] = 10,
            ["11 = B4"] = 11,
            ["12 = C5"] = 12,
            ["13 = C#5"] = 13,
            ["14 = D5"] = 14,
            ["15 = Eb5"] = 15,
            ["16 = E5"] = 16,
            ["17 = F5"] = 17,
            ["18 = F#5"] = 18,
            ["19 = G5"] = 19,
            ["20 = Ab5"] = 20,
            ["21 = A5"] = 21,
            ["22 = Bb5"] = 22,
            ["23 = B5"] = 23,
            ["24 = C6"] = 24,
            ["25 = C#6"] = 25,
            ["26 = D6"] = 26,
            ["27 = Eb6"] = 27,
            ["28 = E6"] = 28,
            ["29 = F6"] = 29,
            ["30 = F#6"] = 30,
            ["31 = G6"] = 31,
            ["32 = Ab6"] = 32,
            ["33 = A6"] = 33,
            ["34 = Bb6"] = 34,
            ["35 = B6"] = 35,
            ["36 = C7"] = 36,
        }
    }
}

elytraNoteRing.placements = {
    {
        name = "elytra_note_ring",
        data = {
            semitone = 12,
        }
    }
}

local dotTexture = "objects/CommunalHelper/elytraRing/dot"
local ringColor = {0.8, 0.8, 0.8, 1.0}

function elytraNoteRing.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    local line = drawableLine.fromPoints({x, y, nx, ny}, ringColor)

    local dx, dy = ny - y, x - nx -- perpendicular (-y, x)
    local angle = math.atan(dy / dx) + (dx >= 0 and 0 or math.pi) + math.pi / 4

    local function addIcon(texture, atx, aty, scale, rot, color)
        local icon = drawableSprite.fromTexture(texture, {x = atx, y = aty})
        icon.rotation = rot + angle
        icon:setJustification(0.5, 0.5)
        icon:setScale(scale, scale)
        icon.color = color
        table.insert(sprites, icon)
    end

    table.insert(sprites, line)

    addIcon(dotTexture, x, y, 2, 0, ringColor)
    addIcon(dotTexture, nx, ny, 2, 0, ringColor)

    return sprites
end

function elytraNoteRing.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local nodes = entity.nodes or {{x = x, y = y + 64}}
    local nx, ny = nodes[1].x, nodes[1].y

    return utils.rectangle(x - 4, y - 4, 8, 8), {utils.rectangle(nx - 4, ny - 4, 8, 8)}
end

return elytraNoteRing
