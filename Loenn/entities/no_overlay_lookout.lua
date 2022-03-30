local watchtower = {}

watchtower.name = "CommunalHelper/NoOverlayLookout"
watchtower.depth = -8500
watchtower.justification = {0.5, 1.0}
watchtower.nodeLimits = {0, -1}
watchtower.nodeLineRenderType = "line"
watchtower.placements = {
    name = "watchtower",
    data = {
        summit = false,
        onlyY = false
    }
}

watchtower.texture = "objects/lookout/lookout05"

return watchtower
