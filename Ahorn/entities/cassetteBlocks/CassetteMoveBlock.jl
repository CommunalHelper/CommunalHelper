module CommunalHelperCassetteMoveBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/CassetteMoveBlock" CassetteMoveBlock(x::Integer, y::Integer,
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    direction::String="Right", moveSpeed::Number=60.0, index::Integer=0, tempo::Number=1.0,
    customColor="")

const placements = Ahorn.PlacementDict(
    "Cassette Move Block ($index - $color) (Communal Helper)" => Ahorn.EntityPlacement(
        CassetteMoveBlock,
        "rectangle",
        Dict{String, Any}(
            "index" => index,
        )
    ) for (color, index) in cassetteColorNames
)

Ahorn.editingOptions(entity::CassetteMoveBlock) = Dict{String, Any}(
    "index" => cassetteColorNames,
    "direction" => Maple.move_block_directions,
	"moveSpeed" => Dict{String, Number}(
		"Slow" => 60.0,
		"Fast" => 75.0
	)
)
Ahorn.minimumSize(entity::CassetteMoveBlock) = 16, 16
Ahorn.resizable(entity::CassetteMoveBlock) = true, true

Ahorn.selection(entity::CassetteMoveBlock) = Ahorn.getEntityRectangle(entity)

const arrows = Dict{String, String}(
    "up" => "objects/CommunalHelper/cassetteMoveBlock/arrow02",
    "left" => "objects/CommunalHelper/cassetteMoveBlock/arrow04",
    "right" => "objects/CommunalHelper/cassetteMoveBlock/arrow00",
    "down" => "objects/CommunalHelper/cassetteMoveBlock/arrow06",
)

const block = "objects/cassetteblock/solid"

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteMoveBlock)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    index = Int(get(entity.data, "index", 0))
    color = getCassetteColor(index)

    hexColor = String(get(entity.data, "customColor", ""))
    if hexColor != "" && length(hexColor) == 6
        color = tuple(Ahorn.argb32ToRGBATuple(parse(Int, hexColor, base=16))[1:3] ./ 255..., 1.0)
    end

    renderCassetteBlock(ctx, 0, 0, width, height, index, color)

    direction = lowercase(get(entity.data, "direction", "up"))
    arrowSprite = Ahorn.getSprite(arrows[lowercase(direction)], "Gameplay")

    Ahorn.drawImage(ctx, arrowSprite, div(width - arrowSprite.width, 2), div(height - arrowSprite.height, 2), tint=color)
end

end
