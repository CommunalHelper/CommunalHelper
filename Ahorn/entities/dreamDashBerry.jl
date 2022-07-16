module DreamDashListenerDreamDashBerry

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamStrawberry" DreamDashBerry(x::Integer, y::Integer, order::Integer=-1, checkpointID::Integer=-1)

const placements = Ahorn.PlacementDict(
	"Dream Berry (Communal Helper)" => Ahorn.EntityPlacement(
		DreamDashBerry
	)
)

function Ahorn.selection(entity::DreamDashBerry)
	x, y = Ahorn.position(entity)

	return Ahorn.getSpriteRectangle("collectables/strawberry/wings01", x, y)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamDashBerry, room::Maple.Room)
	x, y = Ahorn.position(entity)

	Ahorn.drawSprite(ctx, "collectables/strawberry/wings01", x, y)
end

end