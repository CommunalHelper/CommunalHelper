local triggers = require("triggers")
local depths = require("consts.object_depths")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableText = require("structs.drawable_text")
local colors = require("consts.colors")

local dreamTunnelBlocker = {}

dreamTunnelBlocker.name = "CommunalHelper/DreamTunnelBlocker"
dreamTunnelBlocker.depth = depths.fakeWalls + 2000;

dreamTunnelBlocker.placements = {
    name = "normal",
    data = {
        width = 16,
        height = 16,
        blockDreamTunnelDashes = true,
        blockDreamDashes = false
    }
}

-- adapted from triggers.getDrawable
function dreamTunnelBlocker.sprite(room, entity)
    local displayName = triggers.triggerText(room, entity)

    local x = entity.x or 0
    local y = entity.y or 0

    local width = entity.width or 16
    local height = entity.height or 16

    local borderedRectangle = drawableRectangle.fromRectangle("bordered", x, y, width, height, colors.triggerColor, colors.triggerBorderColor)
    local textDrawable = drawableText.fromText(displayName, x, y, width, height, nil, triggers.triggerFontSize, colors.triggerTextColor)

    local drawables = borderedRectangle:getDrawableSprite()
    table.insert(drawables, textDrawable)

    textDrawable.depth = dreamTunnelBlocker.depth - 1

    return drawables
end

return dreamTunnelBlocker
