module CommunalHelperElytra

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/Elytra" Elytra(
    x::Integer,
    y::Integer,
)

const placements = Ahorn.PlacementDict(
    "Elytra (Communal Helper)" => Ahorn.EntityPlacement(
        Elytra,
    ),
)

function Ahorn.selection(entity::Elytra)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 8, y - 8, 16, 16)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Elytra, room::Maple.Room) = Ahorn.drawSprite(ctx, "objects/flyFeather/idle00.png", 0, 0)

end