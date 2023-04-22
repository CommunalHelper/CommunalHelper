local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local enums = require("consts.celeste_enums")

local startPositions = {"Closest", "Top", "Bottom"}

local solarElevator = {}

solarElevator.name = "CommunalHelper/SJ/SolarElevator"

solarElevator.fieldInformation = {
    distance = {
        fieldType = "integer",
        minimumValue = 8
    },
    bgDepth = {
        fieldType = "integer",
        options = enums.depths
    },
    time = {
        fieldType = "number",
        minimumValue = 0.25
    },
    delay = {
        fieldType = "number",
        minimumValue = 0.0
    },
    startPosition = {
        options = startPositions,
        editable = false
    }
}

solarElevator.placements = {
    {
        name = "solar_elevator",
        data = {
            distance = 128,
            bgDepth = 9000,
            time = 3.0,
            delay = 1.0,
            oneWay = false,
            startPosition = "Closest",
            moveSfx = "event:/CommunalHelperEvents/game/strawberryJam/game/solar_elevator/elevate",
            haltSfx = "event:/CommunalHelperEvents/game/strawberryJam/game/solar_elevator/halt",
            requiresHoldable = false,
            holdableHintDialog = "communalhelper_entities_strawberry_jam_solar_elevator_hint",
            reskinDirectory = ""
        }
    }
}

local frontTexture = "objects/CommunalHelper/strawberryJam/solarElevator/front"
local backTexture = "objects/CommunalHelper/strawberryJam/solarElevator/back"
local railsTexture = "objects/CommunalHelper/strawberryJam/solarElevator/rails"

local ghostColor = {1.0, 1.0, 1.0, 0.65}

function solarElevator.sprite(room, entity)
    local sprites = {}

    local distance = entity.distance or 128

    local y = 0
    while y < distance + 60 do
        local rails = drawableSprite.fromTexture(railsTexture, entity)
        local height = rails.meta.meta.height

        rails:addPosition(0, -y - height)
        table.insert(sprites, rails)

        y = y + height
    end

    local backBottom = drawableSprite.fromTexture(backTexture, entity)
    backBottom:setJustification(0.5, 1.0)
    backBottom:addPosition(0, 10)

    local frontBottom = drawableSprite.fromTexture(frontTexture, entity)
    frontBottom:setJustification(0.5, 1.0)
    frontBottom:addPosition(0, 10)

    local backTop = drawableSprite.fromTexture(backTexture, entity)
    backTop:setJustification(0.5, 1.0)
    backTop:addPosition(0, 10 - distance)
    backTop.color = ghostColor

    local frontTop = drawableSprite.fromTexture(frontTexture, entity)
    frontTop:setJustification(0.5, 1.0)
    frontTop:addPosition(0, 10 - distance)
    frontTop.color = ghostColor

    table.insert(sprites, backBottom)
    table.insert(sprites, frontBottom)
    table.insert(sprites, backTop)
    table.insert(sprites, frontTop)

    return sprites
end

function solarElevator.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    return utils.rectangle(x - 24, y - 70, 48, 80)
end

return solarElevator
