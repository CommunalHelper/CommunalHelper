local spikeHelper = require("helpers.spikes")

local timedTriggerSpikes = {}

local directions = {"Up", "Down", "Left", "Right"}

-- for each spike direction, we'll let lönn's spike helper generate most of what we need and won't need to take care of
-- then we can replace or modify what was already generated to finish up the plugins
for _, dir in ipairs(directions) do
    local dirLower = string.lower(dir)
    local spikes = spikeHelper.createEntityHandler("CommunalHelper/TimedTriggerSpikes" .. dir, dirLower, false, true)

    for _, placement in ipairs(spikes.placements) do
        placement.data.Delay = 0.4
        placement.data.WaitForPlayer = false
        placement.data.Grouped = false
        placement.data.Rainbow = false
        placement.data.Reusable = false
        placement.data.ReusableTimer = 0.0
    end

    table.insert(timedTriggerSpikes, spikes)
end

return timedTriggerSpikes
