local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local bouncyPanel = {}

local directions = communalHelper.panelDirectionsOmitDown

bouncyPanel.name = "CommunalHelper/BouncyPanel"
bouncyPanel.depth = -10001
bouncyPanel.canResize = {true, true}
bouncyPanel.fieldInformation = {
    orientation = {
        editable = false,
        options = directions
    }
}

bouncyPanel.placements = {}
for i, direction in ipairs(directions) do
    bouncyPanel.placements[i] = {
        name = string.lower(direction),
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            orientation = direction,
            overrideAllowStaticMovers = false,
            sfx = "event:/game/general/assist_dreamblockbounce"
        }
    }
end

function bouncyPanel.sprite(room, entity)
    return communalHelper.getPanelSprite(entity)
end

function bouncyPanel.rectangle(room, entity)
    return communalHelper.fixAndGetPanelRectangle(entity)
end

return bouncyPanel
