module CommunalHelperCassetteFallingBlock

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/CassetteFallingBlock" CassetteFallingBlock(x::Integer, 
                                                                  y::Integer, 
                                                                  width::Integer=Maple.defaultBlockWidth, 
                                                                  height::Integer=Maple.defaultBlockHeight,
                                                                  index::Integer=0,
                                                                  tempo::Number=1.0) 

const colorNames = Dict{String, Int}(
    "Blue" => 0,
    "Rose" => 1,
    "Bright Sun" => 2,
    "Malachite" => 3
)

const colors = Dict{Int, Ahorn.colorTupleType}(
    1 => (240, 73, 190, 255) ./ 255,
	2 => (252, 220, 58, 255) ./ 255,
	3 => (56, 224, 78, 255) ./ 255
)
const defaultColor = (73, 170, 240, 255) ./ 255

const placements = Ahorn.PlacementDict(
    "Cassette Falling Block ($index - $color) (Communal Helper)" => Ahorn.EntityPlacement(
        CassetteFallingBlock,
        "rectangle",
        Dict{String, Any}(
            "index" => index,
        )
    ) for (color, index) in colorNames
)

Ahorn.editingOptions(entity::CassetteFallingBlock) = Dict{String, Any}(
    "index" => colorNames
)

Ahorn.nodeLimits(entity::CassetteFallingBlock) = 1, 1

Ahorn.minimumSize(entity::CassetteFallingBlock) = 16, 16
Ahorn.resizable(entity::CassetteFallingBlock) = true, true

function Ahorn.selection(entity::CassetteFallingBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

const block = "objects/cassetteblock/solid"

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CassetteFallingBlock, room::Maple.Room)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

    index = Int(get(entity.data, "index", 0))
    color = get(colors, index, defaultColor)

    for i in 1:tileWidth, j in 1:tileHeight
        tx = (i == 1) ? 0 : ((i == tileWidth) ? 16 : 8)
        ty = (j == 1) ? 0 : ((j == tileHeight) ? 16 : 8)

        Ahorn.drawImage(ctx, block, x + (i - 1) * 8, y + (j - 1) * 8, tx, ty, 8, 8, tint=color)
    end
end

end