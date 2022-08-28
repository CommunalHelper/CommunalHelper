module CommunalHelperCassetteFallingBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper
using Ahorn.CommunalHelperEntityPresets: CustomCassetteBlockData

@mapdefdata Entity "CommunalHelper/CassetteFallingBlock" CassetteFallingBlock CustomCassetteBlockData

const placements = Ahorn.PlacementDict(
    "Cassette Falling Block ($index - $color) (Communal Helper)" => Ahorn.EntityPlacement(
        CassetteFallingBlock,
        "rectangle",
        Dict{String,Any}(
            "index" => index,
        ),
    ) for (color, index) in cassetteColorNames
)

Ahorn.editingOptions(entity::CassetteFallingBlock) = Dict{String,Any}(
    "index" => cassetteColorNames
)

Ahorn.nodeLimits(entity::CassetteFallingBlock) = 1, 1

Ahorn.minimumSize(entity::CassetteFallingBlock) = 16, 16
Ahorn.resizable(entity::CassetteFallingBlock) = true, true

Ahorn.selection(entity::CassetteFallingBlock) = Ahorn.getEntityRectangle(entity)

const block = "objects/cassetteblock/solid"

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteFallingBlock)
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
