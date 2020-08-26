module CommunalHelperCrystalHeart

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/CrystalHeart" CrystalHeart(x::Integer, y::Integer, 
	removeCameraTriggers::Bool=false)

const placements = Ahorn.PlacementDict(
	"Crystal Heart (Communal Helper)" => Ahorn.EntityPlacement(
	CrystalHeart,
	"point",
	Dict{String, Any}(),
	function(entity)
		entity.data["nodes"] = [
			(Int(entity.data["x"]) - 20, Int(entity.data["y"]) - 20),
			(Int(entity.data["x"]), Int(entity.data["y"]) - 20),
			(Int(entity.data["x"]) + 20, Int(entity.data["y"]) - 20),
		]
	end
	),
)

Ahorn.nodeLimits(entity::CrystalHeart) = 1, -1

const sprite = "collectables/heartGem/ghost00.png"

const shardSprites = Tuple{String, String}[
	("collectables/CommunalHelper/heartGemShard/shard_outline0$i.png",
	"collectables/CommunalHelper/heartGemShard/shard0$i.png"
	) for i=0:2
]

function Ahorn.selection(entity::CrystalHeart)
	x, y = Ahorn.position(entity)

	nodes = get(entity.data, "nodes", ())

	res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]

	for i = 1:length(nodes)
		nx, ny = nodes[i]
		push!(res, Ahorn.getSpriteRectangle(shardSprites[mod1(i, 3)][1], nx, ny))
	end

	return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::CrystalHeart)
	x, y = Ahorn.position(entity)
	
	nodes = get(entity.data, "nodes", ())
	
	for i = 1:length(nodes)
		nx, ny = nodes[i]

		Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], Ahorn.colors.selection_selected_fc)
	end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CrystalHeart)
	x, y = Ahorn.position(entity)

	nodes = get(entity.data, "nodes", ())

	for i = 1:length(nodes)
		nx, ny = nodes[i]
		sprIdx = mod1(i, 3)
		Ahorn.drawSprite(ctx, shardSprites[sprIdx][1], nx, ny)
		Ahorn.drawSprite(ctx, shardSprites[sprIdx][2], nx, ny)
	end

	Ahorn.drawSprite(ctx, sprite, x, y)
end


end