local drawableSprite = require("structs.drawable_sprite")

local laserEmitter = {}

laserEmitter.name = "CommunalHelper/SJ/LaserEmitter"
laserEmitter.depth = -8500

laserEmitter.fieldInformation = {
    color = {
        fieldType = "color"
    },
    thickness = {
        fieldType = "integer"
    },
    flagAction = {
        options = {
            "None",
            "Set",
            "Clear"
        }
    },
    emitterColliderWidth = {
        fieldType = "integer"
    },
    emitterColliderHeight = {
        fieldType = "integer"
    },
    orientation = {
        options = {
            "Up",
            "Down",
            "Left",
            "Right"
        }
    }
}

laserEmitter.placements = {}

local function addPlacement(orientation)
    table.insert(laserEmitter.placements, {
        name = string.lower(orientation),
        data = {
            orientation = orientation,
            alpha = 0.4,
            collideWithSolids = true,
            color = "ff0000",
            colorChannel = "", -- needs to be nullable
            disableLasers = false,
            flicker = true,
            killPlayer = true,
            thickness = 6,
            triggerZipMovers = false,
            flagName = "",
            flagAction = "None",
            spriteName = "",
            useTintOverlay = true,
            emitterColliderWidth = 14,
            emitterColliderHeight = 6,
            emitSparks = true,
        }
    })
end

addPlacement("Up")
addPlacement("Down")
addPlacement("Left")
addPlacement("Right")

local baseTexture = "objects/CommunalHelper/strawberryJam/laserEmitter/base00"
local tintTexture = "objects/CommunalHelper/strawberryJam/laserEmitter/tint00"

function laserEmitter.sprite(room, entity)
    local rotation =
        entity.orientation == "Up" and 0 or
        entity.orientation == "Down" and math.pi or
        entity.orientation == "Left" and -math.pi / 2 or
        math.pi / 2

    local baseSprite = drawableSprite.fromTexture(baseTexture, entity)
    baseSprite:setJustification(0.5, 1.0)
    baseSprite.rotation = rotation
    baseSprite:setColor({1,1,1,1})
    if not entity.useTintOverlay then return baseSprite end

    local tintSprite = drawableSprite.fromTexture(tintTexture, entity)
    tintSprite:setJustification(0.5, 1.0)
    tintSprite.rotation = rotation
    tintSprite:setColor(entity.color)

    return { baseSprite, tintSprite }
end

return laserEmitter
