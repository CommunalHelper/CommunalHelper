module CommunalHelperPoisonGas

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/PoisonGas" SusanPoison(x::Integer, y::Integer, spritePath::String="objects/CommunalHelper/poisonGas/gas", radius::Integer=48)


const placements = Ahorn.PlacementDict(
   "Poison Gas (Communal Helper)" => Ahorn.EntityPlacement(
	  SusanPoison,
	  "point"
   )
)



function Ahorn.selection(entity::SusanPoison)
    x, y = Ahorn.position(entity)
    r = get(entity.data, "radius", 48)
    return Ahorn.Rectangle(x - r, y - r, r*2, r*2)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SusanPoison, room::Maple.Room) = Ahorn.drawSprite(ctx, string(get(entity.data, "spritePath", "objects/CommunalHelper/poisonGas/gas"), "00"), 0, 0; sx=get(entity.data, "radius", 48)/24.0, sy=get(entity.data, "radius", 48)/24.0)

end

