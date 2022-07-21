local utils = require("utils")

local seekerDashRefill = {}

seekerDashRefill.name = "CommunalHelper/SeekerDashRefill"
seekerDashRefill.depth = -100

seekerDashRefill.placements = {
    {
        name = "seeker_dash_refill",
        data = {
            oneUse = false
        }
    }
}

seekerDashRefill.texture = "objects/CommunalHelper/seekerDashRefill/idle00"

function seekerDashRefill.selection(room, entity)
    local x, y = entity.x, entity.y
    return utils.rectangle(x - 5, y - 5, 10, 10)
end

return seekerDashRefill
