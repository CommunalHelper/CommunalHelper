module CommunalHelperStationBlock
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/StationBlock" StationBlock(
			x::Integer, y::Integer,
			width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Station Block (Communal Helper)" => Ahorn.EntityPlacement(
        StationBlock,
		"rectangle"
    )
)

Ahorn.minimumSize(entity::StationBlock) = 16, 16
Ahorn.resizable(entity::StationBlock) = true, true

const block = "objects/CommunalHelper/stationBlock/block"
const backColor = (123, 151, 171) ./ 255

function Ahorn.selection(entity::StationBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::StationBlock, room::Maple.Room)
    renderStationBlock(ctx, entity)
end

function getArrowSprite(blockSize::Integer)
	if blockSize <= 16
		return "objects/CommunalHelper/stationBlock/smallArrow/smal00"
	elseif blockSize <= 24
		return "objects/CommunalHelper/stationBlock/medArrow/med00"
	else 
		return "objects/CommunalHelper/stationBlock/bigArrow/big00"
	end
end

function renderStationBlock(ctx::Ahorn.Cairo.CairoContext, entity::StationBlock)
	x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))
	
	arrow = getArrowSprite(Integer(min(width, height)))
	arrowSprite = Ahorn.getSprite(arrow, "Gameplay")
	
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)
	
	Ahorn.drawRectangle(ctx, x + 8, y + 8, width - 16, height - 16, backColor)
	
    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, block, x + (i - 1) * 8, y, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, block, x + (i - 1) * 8, y + height - 8, 8, 16, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, block, x, y + (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, block, x + width - 8, y + (i - 1) * 8, 16, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, block, x, y, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, block, x + width - 8, y, 16, 0, 8, 8)
    Ahorn.drawImage(ctx, block, x, y + height - 8, 0, 16, 8, 8)
    Ahorn.drawImage(ctx, block, x + width - 8, y + height - 8, 16, 16, 8, 8)
	
	Ahorn.drawImage(ctx, arrowSprite, x + floor(Int, (width - arrowSprite.width) / 2), y + floor(Int, (height - arrowSprite.height) / 2))
end

end
