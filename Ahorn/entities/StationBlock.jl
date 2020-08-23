module CommunalHelperStationBlock
using ..Ahorn, Maple

const themes = ["Normal", "Moon"]
const behaviors = ["Pulling", "Pushing"]

@mapdef Entity "CommunalHelper/StationBlock" StationBlock(
			x::Integer, y::Integer,
			width::Integer=16, height::Integer=16,
            theme::String="Normal", behavior::String="Pulling",
            customBlockPath::String="", customArrowPath::String="", customTrackPath::String="")

const placements = Ahorn.PlacementDict(
    "Station Block ($theme, $behavior) (Communal Helper)" => Ahorn.EntityPlacement(
        StationBlock,
		"rectangle",
		Dict{String, Any}(
			"theme" => theme,
			"behavior" => behavior,
		)
    ) for theme in themes for behavior in behaviors
)

Ahorn.editingOptions(entity::StationBlock) = Dict{String, Any}(
    "theme" => themes,
	"behavior" => behaviors
)

Ahorn.minimumSize(entity::StationBlock) = 16, 16
Ahorn.resizable(entity::StationBlock) = true, true

function Ahorn.selection(entity::StationBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::StationBlock, room::Maple.Room)
    renderStationBlock(ctx, entity)
end

function getSprites(blockSize::Integer, entity::StationBlock)
	theme = lowercase(get(entity, "theme", "normal"))
	behavior = lowercase(get(entity, "behavior", "pulling"))

	path = "objects/CommunalHelper/stationBlock/"
	arrowDirectory, block = (theme == "normal") ? 
					   ((behavior == "pulling") ? ("arrow/", "/block") : ("altArrow/", "/alt_block")) : 
					   ((behavior == "pulling") ? ("moonArrow/", "/moon_block") : ("altMoonArrow/", "/alt_moon_block"))
	
    arrow = (blockSize <= 16) ? "small00" : ((blockSize <= 24) ? "med00" : "big00")
    
    return (path * arrowDirectory * arrow), (path * "blocks" * block)
	
	return
end

function renderStationBlock(ctx::Ahorn.Cairo.CairoContext, entity::StationBlock)
	x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))
	
	arrow, block = getSprites(Integer(min(width, height)), entity)
	arrowSprite = Ahorn.getSprite(arrow, "Gameplay")
	
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)
	
    for i in 1:tilesWidth, j in 1:tilesHeight
        tx = (i == 1) ? 0 : ((i == tilesWidth) ? 16 : 8)
        ty = (j == 1) ? 0 : ((j == tilesHeight) ? 16 : 8)

        Ahorn.drawImage(ctx, block, x + (i - 1) * 8, y + (j - 1) * 8, tx, ty, 8, 8)
    end
	
	Ahorn.drawImage(ctx, arrowSprite, x + floor(Int, (width - arrowSprite.width) / 2), y + floor(Int, (height - arrowSprite.height) / 2))
end

end
