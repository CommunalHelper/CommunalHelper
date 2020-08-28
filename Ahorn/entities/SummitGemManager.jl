module CommunalHelperCustomSummitGemManager

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/CustomSummitGemManager" SummitGemManager(x::Integer, y::Integer, 
	gemIds::String="", melody::String="", heartOffset::String="0.0,0.0", nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])
	
const placements = Ahorn.PlacementDict(
	"Summit Gem Manager (Communal Helper)" => Ahorn.EntityPlacement(
		SummitGemManager
	)
)

Ahorn.nodeLimits(entity::SummitGemManager) = 0, -1

const gemSprite = "collectables/summitgems/0/gem00"

const smallGemSprites = String["collectables/summitgems/$i/small00" for i=0:7]

function Ahorn.selection(entity::SummitGemManager)
	x, y = Ahorn.position(entity)

	nodes = get(entity.data, "nodes", ())

	res = Ahorn.Rectangle[Ahorn.Rectangle(x - 15, y - 15, 30, 30)]
	
	for node in nodes
		nx, ny = node
		push!(res, Ahorn.getSpriteRectangle(gemSprite, nx, ny))
	end
	
	hx, hy = getHeartOffset(entity)
	if hx + hy != 0
		push!(res, Ahorn.getSpriteRectangle("collectables/heartGem/ghost00.png", x + hx, y + hy))
	end

	return res
end

# Cruor please don't yell at me
# todo: pester Cruor into making it possible to do this with `handleDeletion`
function Ahorn.Selection.applyMovement!(target::SummitGemManager, ox, oy, node=0)
	if node == 0
		target.data["x"] += ox
		target.data["y"] += oy
	else
		nodes = get(target.data, "nodes", ())

		if length(nodes) >= node
			nodes[node] = nodes[node] .+ (ox, oy)
		elseif node == length(nodes) + 1
			heartOffset = getHeartOffset(target)
			(hx, hy) = heartOffset .+ (ox, oy)
			target.data["heartOffset"] = string(hx, ",", hy)
		end
	end

	return true
end

function getHeartOffset(entity::SummitGemManager)
	heartOffset = get(entity.data, "heartOffset", "")
	if length(heartOffset) > 3
		heartOffset = (split(heartOffset, ","))
		heartOffset = tryparse.(Float64, heartOffset)
		if all(f -> isnothing(f), heartOffset)
			return (0, 0)
		end
		return (heartOffset[1], heartOffset[2])
	end
	return (0, 0)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::SummitGemManager)
	x, y = Ahorn.position(entity)
	
	nodes = get(entity.data, "nodes", ())
	
	for node in nodes
		nx, ny = node

		Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], Ahorn.colors.selection_selected_fc)
	end
	
	hx, hy = getHeartOffset(entity)
	if hx + hy != 0
		Ahorn.drawArrow(ctx, x, y, x + hx, y + hy, Ahorn.colors.selection_selected_fc; headLength=6, headTheta=pi/8)
	end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::SummitGemManager)
	x, y = Ahorn.position(entity)
	
	hx, hy = getHeartOffset(entity)
	if hx + hy != 0
		Ahorn.drawSprite(ctx, "collectables/heartGem/ghost00.png", x + hx, y + hy)
	end
		
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
		gx, gy = Base.Math.cospi((i / length(smallGemSprites)) * 2) * 14 , Base.Math.sinpi((i / length(smallGemSprites)) * 2) * 14
		Ahorn.drawSprite(ctx, smallGemSprites[i], x + gx, y + gy)
	end
	
	nodes = get(entity.data, "nodes", ())

	for node in nodes
		nx, ny = node
		Ahorn.drawSprite(ctx, gemSprite, nx, ny)
	end
end

end 