local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dreamZipMover = {}

dreamZipMover.name = "CommunalHelper/DreamZipMover"
dreamZipMover.minimumSize = {16, 16}
dreamZipMover.nodeVisibility = "never"
dreamZipMover.nodeLimits = {1, -1}
dreamZipMover.fieldInformation = {
    refillCount = {
        fieldType = "integer"
    },
    ropeColor = {
        fieldType = "color",
        allowEmpty = true,
    },
}

function dreamZipMover.depth(room, entity)
    return entity.below and 5000 or -11000
end

dreamZipMover.placements = {
    {
        name = "dream_zip_mover",
        placementType = "rectangle",
        data = {
            width = 16,
            height = 16,
            featherMode = false,
            dashSpeed = 240.0,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
            dreamAesthetic = false,
            permanent = false,
            waiting = false,
            ticking = false,
            noReturn = false,
            ropeColor = "",
            linked = false,
        }
    }
}

local zipMoverRopeColor = {102 / 255, 57 / 255, 49 / 255}
local dreamAestheticRopeColor = {0.9, 0.9, 0.9}

function dreamZipMover.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)
    local centerX, centerY = x + halfWidth, y + halfHeight

    local sprites = {}

    local dreamAesthetic = entity.dreamAesthetic
    local cogTexture = dreamAesthetic and "objects/CommunalHelper/dreamZipMover/cog" or "objects/zipmover/cog"

    local ropeColor
    if entity.dreamAesthetic then
        ropeColor = dreamAestheticRopeColor
    elseif entity.ropeColor and entity.ropeColor ~= "" then
        ropeColor = entity.ropeColor
    else
        ropeColor = zipMoverRopeColor
    end

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeSprites = communalHelper.getZipMoverNodeSprites(x, y, width, height, nodes, cogTexture, {1, 1, 1}, ropeColor)
    for _, sprite in ipairs(nodeSprites) do
        table.insert(sprites, sprite)
    end

    if entity.noReturn then
        local cross = drawableSprite.fromTexture("objects/CommunalHelper/dreamMoveBlock/x")
        cross:setPosition(centerX, centerY)
        cross.depth = -1

        table.insert(sprites, cross)
    end

    table.insert(sprites, communalHelper.getCustomDreamBlockSprites(x, y, width, height, entity.featherMode, entity.oneUse))

    return sprites
end

function dreamZipMover.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local mainRectangle = utils.rectangle(x, y, width, height)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeRectangles = {}
    for _, node in ipairs(nodes) do
        local centerNodeX, centerNodeY = node.x + halfWidth, node.y + halfHeight

        table.insert(nodeRectangles, utils.rectangle(centerNodeX - 5, centerNodeY - 5, 10, 10))
    end

    return mainRectangle, nodeRectangles
end

return dreamZipMover
