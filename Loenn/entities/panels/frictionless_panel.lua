local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local frictionlessPanel = {}

local directions = communalHelper.panelDirectionsOmitDown

frictionlessPanel.name = "CommunalHelper/FrictionlessPanel"
frictionlessPanel.depth = -10001
frictionlessPanel.canResize = {true, true}
frictionlessPanel.fieldInformation = {
    orientation = {
        options = directions,
        editable = false
    }
}

frictionlessPanel.placements = {}
for i, direction in ipairs(directions) do
    frictionlessPanel.placements[i] = {
        name = string.lower(direction),
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            orientation = direction
        }
    }
end

function frictionlessPanel.sprite(room, entity)
    return communalHelper.getPanelSprite(entity)
end

function frictionlessPanel.rectangle(room, entity)
    return communalHelper.fixAndGetPanelRectangle(entity)
end

return frictionlessPanel
