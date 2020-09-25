module CommunalHelperPortalZipMover
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/PortalZipMover" PortalZipMover(
			x::Integer, y::Integer,
			width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Zip Mover (Portal-compatible) (Communal Helper)" => Ahorn.EntityPlacement(
        PortalZipMover,
		"rectangle"
    )
)

Ahorn.minimumSize(entity::PortalZipMover) = 16, 16
Ahorn.resizable(entity::PortalZipMover) = true, true

function Ahorn.selection(entity::PortalZipMover)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::PortalZipMover, room::Maple.Room)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    Ahorn.drawRectangle(ctx, x, y, width, height)
end

end
