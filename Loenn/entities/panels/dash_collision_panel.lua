local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dashCollisionPanel = {}

local directions = communalHelper.panelDirections
local dashCollisionResults = {
    "Rebound",
    "NormalCollision",
    "NormalOverride",
    "Bounce",
    "Ignore"
}

dashCollisionPanel.name = "CommunalHelper/DashCollisionPanel"
dashCollisionPanel.depth = -10001
dashCollisionPanel.canResize = {true, true}
dashCollisionPanel.fieldInformation = {
    dashCollideResult = {
        editable = false,
        options = dashCollisionResults
    },
    orientation = {
        editable = false,
        options = directions
    }
}

dashCollisionPanel.placements = {}
for i, direction in ipairs(directions) do
    dashCollisionPanel.placements[i] = {
        name = string.lower(direction),
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            orientation = direction,
            dashCollideResult = "None",
            overrideAllowStaticMovers = false,
        }
    }
end

function dashCollisionPanel.sprite(room, entity)
    return communalHelper.getPanelSprite(entity)
end

function dashCollisionPanel.rectangle(room, entity)
    return communalHelper.fixAndGetPanelRectangle(entity)
end

return dashCollisionPanel
