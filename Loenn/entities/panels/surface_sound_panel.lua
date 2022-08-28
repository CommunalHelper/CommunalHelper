local enums = require("consts.celeste_enums")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local surfaceSoundPanel = {}

local directions = communalHelper.panelDirectionsOmitDown

surfaceSoundPanel.name = "CommunalHelper/SurfaceSoundPanel"
surfaceSoundPanel.depth = -10001
surfaceSoundPanel.canResize = {true, true}
surfaceSoundPanel.fieldInformation = {
    orientation = {
        options = directions,
        editable = false
    },
    soundIndex = {
        options = enums.tileset_sound_ids,
        fieldType = "integer"
    }
}

surfaceSoundPanel.placements = {}
for i, direction in ipairs(directions) do
    surfaceSoundPanel.placements[i] = {
        name = string.lower(direction),
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            orientation = direction,
            overrideAllowStaticMovers = false,
            soundIndex = 11
        }
    }
end

function surfaceSoundPanel.sprite(room, entity)
    return communalHelper.getPanelSprite(entity)
end

function surfaceSoundPanel.rectangle(room, entity)
    return communalHelper.fixAndGetPanelRectangle(entity)
end

return surfaceSoundPanel
