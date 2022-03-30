local summitGem = {}

local summitGemNames = {
    ["0 - Reflection"] = 0,
    ["1 - Forsaken City"] = 1,
    ["2 - Old Site"] = 2,
    ["3 - Celestial Resort"] = 3,
    ["4 - Golden Ridge"] = 4,
    ["5 - Mirror Temple"] = 5,
    ["6 - Custom Blue Star Gem"] = 6,
    ["7 - Custom Red Triangle Gem"] = 7
}

summitGem.name = "CommunalHelper/CustomSummitGem"
summitGem.depth = 0
summitGem.fieldInformation = {
    index = {
        options = summitGemNames,
        minimumValue = 0,
        fieldType = "integer"
    }
}

summitGem.placements = {
    {
        name = "summit_gem",
        data = {
            index = 0
        }
    }
}

function summitGem.texture(room, entity)
    return "collectables/summitgems/" .. entity.index .. "/gem00"
end

return summitGem
