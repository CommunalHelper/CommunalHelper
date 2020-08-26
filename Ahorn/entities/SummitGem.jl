module CommunalHelperCustomSummitGem

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/CustomSummitGem" CustomSummitGem(x::Integer, y::Integer, 
	index::Integer=0)
	
const placements = Ahorn.PlacementDict(
	"Summit Gem (Communal Helper)" => Ahorn.EntityPlacement(
		CustomSummitGem
	)
)

const sprites = ["collectables/summitgems/$i/gem00" for i = 0:7]

function getSprite(index)
	if index > length(sprites)
		return sprites[end]
	end
	return sprites[index]
end

# positive numbers only
function getClampedIndex(entity::CustomSummitGem)
	index = Int(get(entity.data, "index", 0))
	if index < 0
		index = 0
	end
	entity.data["index"] = index
	return index
end

function Ahorn.selection(entity::CustomSummitGem)
	x, y = Ahorn.position(entity)
	
	return Ahorn.getSpriteRectangle(getSprite(getClampedIndex(entity) + 1) , x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomSummitGem) = Ahorn.drawSprite(ctx, getSprite(getClampedIndex(entity) + 1), 0, 0)

end 