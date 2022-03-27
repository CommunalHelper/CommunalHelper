local summitGem = {}

summitGem.name = "CommunalHelper/CustomSummitGem"
summitGem.depth = 0

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
