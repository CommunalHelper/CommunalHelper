local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")

local dreamTunnelEntry = {}

local directions = communalHelper.panelDirections

dreamTunnelEntry.name = "CommunalHelper/DreamTunnelEntry"
dreamTunnelEntry.canResize = {true, true}
dreamTunnelEntry.fieldInformation = {
    orientation = {
        editable = false,
        options = directions,
    }
}

function dreamTunnelEntry.depth(room, entity, viewport)
    return entity.depth or -10001
end

dreamTunnelEntry.placements = {}
for i, direction in ipairs(directions) do
    dreamTunnelEntry.placements[i] = {
        name = string.lower(direction),
        placementType = "rectangle",
        data = {
            width = 8,
            height = 8,
            orientation = direction,
            overrideAllowStaticMovers = false,
            depth = -13000,
        }
    }
end

function dreamTunnelEntry.sprite(room, entity)
    return communalHelper.getPanelSprite(entity, {0, 0, 0})
end

function dreamTunnelEntry.rectangle(room, entity)
    return communalHelper.fixAndGetPanelRectangle(entity)
end

return dreamTunnelEntry
