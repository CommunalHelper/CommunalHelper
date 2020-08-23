module CommunalHelperCustomSummitGemManager

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/CustomSummitGemManager" SummitGemManager(x::Integer, y::Integer, 
	gemIds::String="", nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])
	
const placements = Ahorn.PlacementDict(
	"Summit Gem Manager (Communal Helper)" => Ahorn.EntityPlacement(
		SummitGemManager
	)
)

Ahorn.nodeLimits(entity::SummitGemManager) = 0, -1

const gemSprite = "collectables/summitgems/0/gem00"

const smallGemSprites = String["collectables/summitgems/$i/small00" for i=0:5]

function Ahorn.selection(entity::SummitGemManager)
	x, y = Ahorn.position(entity)

	nodes = get(entity.data, "nodes", ())

	res = Ahorn.Rectangle[Ahorn.Rectangle(x - 15, y - 15, 30, 30)]

	for node in nodes
		nx, ny = node
		push!(res, Ahorn.getSpriteRectangle(gemSprite, nx, ny))
	end

	return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::SummitGemManager)
	x, y = Ahorn.position(entity)
	
	nodes = get(entity.data, "nodes", ())
	
	for node in nodes
		nx, ny = node

		Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], Ahorn.colors.selection_selected_fc)
	end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::SummitGemManager)
	x, y = Ahorn.position(entity)
	
	Ahorn.Cairo.save(ctx)
	Ahorn.set_antialias(ctx, 1)
	Ahorn.set_line_width(ctx, 1)
	Ahorn.Cairo.arc(ctx, x, y, 20, 0, pi * 2)
	Ahorn.Cairo.set_source_rgba(ctx, 0.0, 0.0, 0.0, 0.4)
	Ahorn.Cairo.fill_preserve(ctx)
	Ahorn.Cairo.set_source_rgba(ctx, 1.0, 1.0, 1.0, 1.0)
	Ahorn.Cairo.stroke(ctx)
	Ahorn.restore(ctx)
	
	for i = 1:length(smallGemSprites)
		gx, gy = Base.Math.cospi((i / length(smallGemSprites)) * 2) * 12 , Base.Math.sinpi((i / length(smallGemSprites)) * 2) * 12
		Ahorn.drawSprite(ctx, smallGemSprites[i], x + gx, y + gy)
	end
	
	nodes = get(entity.data, "nodes", ())

	for node in nodes
		nx, ny = node
		Ahorn.drawSprite(ctx, gemSprite, nx, ny)
	end
end

end 