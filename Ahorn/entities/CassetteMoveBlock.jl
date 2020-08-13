module CommunalHelperCassetteMoveBlock
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/CassetteMoveBlock" CassetteMoveBlock(x::Integer, 
                                                              y::Integer, 
                                                              width::Integer=Maple.defaultBlockWidth, 
                                                              height::Integer=Maple.defaultBlockHeight,
                                                              direction::String="Right", 
                                                              fast::Bool=false,
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
    "Cassette Move Block ($index - $color) (Communal Helper)" => Ahorn.EntityPlacement(
        CassetteMoveBlock,
        "rectangle",
        Dict{String, Any}(
            "index" => index,
        )
    ) for (color, index) in colorNames
)

Ahorn.editingOptions(entity::CassetteMoveBlock) = Dict{String, Any}(
    "direction" => Maple.move_block_directions
)
Ahorn.minimumSize(entity::CassetteMoveBlock) = 16, 16
Ahorn.resizable(entity::CassetteMoveBlock) = true, true

Ahorn.selection(entity::CassetteMoveBlock) = Ahorn.getEntityRectangle(entity)

const arrows = Dict{String, String}(
    "up" => "objects/CommunalHelper/dreamMoveBlock/arrow02",
    "left" => "objects/CommunalHelper/dreamMoveBlock/arrow04",
    "right" => "objects/CommunalHelper/dreamMoveBlock/arrow00",
    "down" => "objects/CommunalHelper/dreamMoveBlock/arrow06",
)

const block = "objects/cassetteblock/solid"

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteMoveBlock, room::Maple.Room)
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

        Ahorn.drawImage(ctx, block, (i - 1) * 8, (j - 1) * 8, tx, ty, 8, 8, tint=color)
    end

    direction = lowercase(get(entity.data, "direction", "up"))
    arrowSprite = Ahorn.getSprite(arrows[lowercase(direction)], "Gameplay")

    Ahorn.drawImage(ctx, arrowSprite, div(width - arrowSprite.width, 2), div(height - arrowSprite.height, 2))
end

end