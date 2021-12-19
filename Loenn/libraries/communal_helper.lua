local utils = require("utils")

local communalHelper = {}

function communalHelper.hexToColor(hex)
    local success, r, g, b, a = utils.parseHexColor(hex)
    local color = {1.0, 1.0, 1.0, 1.0}
    if success then
        color = {r, g, b, a}
    end
    return color
end

return communalHelper