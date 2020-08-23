module CommunalHelperCustomSummitGem

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/CustomSummitGem" CustomSummitGem(x::Integer, y::Integer, 
	index::Integer=0)
	
const placements = Ahorn.PlacementDict(
	"Summit Gem (Communal Helper)" => Ahorn.EntityPlacement(
		CustomSummitGem
	)
)

const sprites = [
	["collectables/summitgems/$i/gem00" for i = 0:5]; 
	["collectables/summitgems/5/gem00",
	"collectables/summitgems/5/gem00"]
]

# error checking, if people want other indices, too bad
function getClampedIndex(entity::CustomSummitGem)
	index = Int(get(entity.data, "index", 0))
	entity.data["index"] = index = Base.Math.clamp(index, 0, 7)
	return index
end

function Ahorn.selection(entity::CustomSummitGem)
	x, y = Ahorn.position(entity)
	
	return Ahorn.getSpriteRectangle(sprites[getClampedIndex(entity) + 1] , x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomSummitGem) = Ahorn.drawSprite(ctx, sprites[getClampedIndex(entity) + 1], 0, 0)

end 