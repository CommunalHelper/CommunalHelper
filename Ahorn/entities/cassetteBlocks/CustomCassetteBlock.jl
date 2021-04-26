module CommunalHelperCustomCassetteBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/CustomCassetteBlock" CustomCassetteBlock(x::Integer, 
                                                                  y::Integer, 
                                                                  width::Integer=Maple.defaultBlockWidth, 
                                                                  height::Integer=Maple.defaultBlockHeight,
                                                                  index::Integer=0,
                                                                  tempo::Number=1.0,
                                                                  customColor="") 

const placements = Ahorn.PlacementDict(
    "Custom Cassette Block ($index - $color) (Communal Helper)" => Ahorn.EntityPlacement(
        CustomCassetteBlock,
        "rectangle",
        Dict{String, Any}(
            "index" => index,
        )
    ) for (color, index) in cassetteColorNames
)

Ahorn.editingOptions(entity::CustomCassetteBlock) = Dict{String, Any}(
    "index" => cassetteColorNames
)

Ahorn.nodeLimits(entity::CustomCassetteBlock) = 1, 1

Ahorn.minimumSize(entity::CustomCassetteBlock) = 16, 16
Ahorn.resizable(entity::CustomCassetteBlock) = true, true

Ahorn.selection(entity::CustomCassetteBlock) = Ahorn.getEntityRectangle(entity)

const block = "objects/cassetteblock/solid"

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomCassetteBlock)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    index = Int(get(entity.data, "index", 0))

    hexColor = String(get(entity.data, "customColor", ""))
    if hexColor != "" && length(hexColor) == 6
        renderCassetteBlock(ctx, 0, 0, width, height, index, hexToRGBA(hexColor))
    else
        renderCassetteBlock(ctx, 0, 0, width, height, index)
    end
end

end