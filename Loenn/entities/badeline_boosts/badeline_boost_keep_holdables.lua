-- pretty much a copy of Loenn's badeline_boost.lua

local badelineBoostKeepHoldables = {}

badelineBoostKeepHoldables.name = 'CommunalHelper/BadelineBoostKeepHoldables'
badelineBoostKeepHoldables.depth = -1000000
badelineBoostKeepHoldables.nodeLineRenderType = "line"
badelineBoostKeepHoldables.texture = "objects/badelineboost/idle00"
badelineBoostKeepHoldables.nodeLimits = {0, -1}
badelineBoostKeepHoldables.placements = {
    name = "boost",
    data = {
        lockCamera = true,
        canSkip = false,
        finalCh9Boost = false,
        finalCh9GoldenBoost = false,
        finalCh9Dialog = false
    }
}

return badelineBoostKeepHoldables
