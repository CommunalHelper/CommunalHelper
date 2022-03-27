local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dreamSwitchGate = {}

dreamSwitchGate.name = "CommunalHelper/DreamSwitchGate"
dreamSwitchGate.nodeLimits = {1, 1}
dreamSwitchGate.nodeLineRenderType = "line"
dreamSwitchGate.minimumSize = {16, 16}

function dreamSwitchGate.depth(room, entity)
    return entity.below and 5000 or -11000
end

dreamSwitchGate.placements = {
    {
        name = "dream_switch_gate",
        data = {
            width = 16,
            height = 16,
            featherMode = false,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
            permanent = false,
        }
    }
}

local function addBlockSprites(sprites, x, y, width, height, feather)
    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)
    local centerX, centerY = x + halfWidth, y + halfHeight

    table.insert(sprites, communalHelper.getCustomDreamBlockSprites(x, y, width, height, feather))
    
    local icon = drawableSprite.fromTexture("objects/switchgate/icon00")
    icon:setPosition(centerX, centerY)
    icon.depth = -1
    
    table.insert(sprites, icon)
end

function dreamSwitchGate.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local feather = entity.featherMode

    local sprites = {}

    addBlockSprites(sprites, x, y, width, height, feather)

    return sprites
end

function dreamSwitchGate.nodeSprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local feather = entity.featherMode

    local sprites = {}

    addBlockSprites(sprites, nodeX, nodeY, width, height, feather)

    return sprites
end

function dreamSwitchGate.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 16, entity.height or 16

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return dreamSwitchGate
