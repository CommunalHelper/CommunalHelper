local mods = require("mods")

-- this entity plugin can only be shown with Maddie's Helping Hand loaded
if not mods.hasLoadedMod("MaxHelpingHand") then
    return
end

local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")
local communalHelper = mods.requireFromPlugin("libraries.communal_helper")

local dreamFlagSwitchGate = {}

local bundledIcons = {
    "vanilla",
    "tall",
    "triangle",
    "circle",
    "diamond",
    "double",
    "heart",
    "square",
    "wide",
    "winged"
}

dreamFlagSwitchGate.name = "CommunalHelper/MaxHelpingHand/DreamFlagSwitchGate"
dreamFlagSwitchGate.nodeLimits = {1, 1}
dreamFlagSwitchGate.nodeLineRenderType = "line"
dreamFlagSwitchGate.minimumSize = {16, 16}
dreamFlagSwitchGate.fieldInformation = {
    inactiveColor = {
        fieldType = "color"
    },
    activeColor = {
        fieldType = "color"
    },
    finishColor = {
        fieldType = "color"
    },
    icon = {
        options = bundledIcons
    },
    moveSound = {
        options = {"event:/game/general/touchswitch_gate_open"}
    },
    finishedSound = {
        options = {"event:/game/general/touchswitch_gate_finish"}
    },
    refillCount = {
        fieldType = "integer"
    }
}

function dreamFlagSwitchGate.depth(room, entity)
    return entity.below and 5000 or -11000
end

dreamFlagSwitchGate.placements = {
    {
        name = "dream_flag_switch_gate",
        data = {
            width = 16,
            height = 16,
            featherMode = false,
            oneUse = false,
            refillCount = -1,
            below = false,
            quickDestroy = false,
            persistent = false,
            flag = "flag_touch_switch",
            icon = "vanilla",
            inactiveColor = "5fcde4",
            activeColor = "ffffff",
            finishColor = "f141df",
            shakeTime = 0.5,
            moveTime = 1.8,
            moveEased = true,
            allowReturn = false,
            moveSound = "event:/game/general/touchswitch_gate_open",
            finishedSound = "event:/game/general/touchswitch_gate_finish"
        }
    }
}

local function addBlockSprites(sprites, x, y, width, height, feather, oneUse, icon)
    local halfWidth, halfHeight = math.floor(width / 2), math.floor(height / 2)
    local centerX, centerY = x + halfWidth, y + halfHeight

    table.insert(sprites, communalHelper.getCustomDreamBlockSprites(x, y, width, height, feather))

    local iconResource = "objects/switchgate/icon00"
    if icon ~= "vanilla" then
        iconResource = "objects/MaxHelpingHand/flagSwitchGate/" .. icon .. "/icon00"
    end

    local iconSprite = drawableSprite.fromTexture(iconResource)
    iconSprite:setPosition(centerX, centerY)
    iconSprite.depth = -1

    table.insert(sprites, iconSprite)
end

function dreamFlagSwitchGate.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local feather = entity.featherMode
    local oneUse = entity.oneUse
    local icon = entity.icon or "vanilla"

    local sprites = {}

    addBlockSprites(sprites, x, y, width, height, feather, oneUse, icon)

    return sprites
end

function dreamFlagSwitchGate.nodeSprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local nodes = entity.nodes or {}
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local feather = entity.featherMode
    local oneUse = entity.oneUse
    local icon = entity.icon or "vanilla"

    local sprites = {}

    addBlockSprites(sprites, nodeX, nodeY, width, height, feather, oneUse, icon)

    return sprites
end

function dreamFlagSwitchGate.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 16, entity.height or 16

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return dreamFlagSwitchGate
