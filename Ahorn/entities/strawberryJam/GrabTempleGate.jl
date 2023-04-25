module CommunalHelperSJGrabTempleGate

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/GrabTempleGate" GrabTempleGate(x::Integer, y::Integer, closed::Bool = false)

const placements = Ahorn.PlacementDict(
    "Grab Temple Gate (Open) (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        GrabTempleGate,
        "point",
        Dict{String, Any}(
            "closed" => false
        )
    ),
    "Grab Temple Gate (Closed) (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        GrabTempleGate,
        "point",
        Dict{String, Any}(
            "closed" => true
        )
    )
)

const texture = "objects/CommunalHelper/strawberryJam/grabTempleGate/TempleDoor05";

function Ahorn.selection(entity::GrabTempleGate)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 3, y, 15, 48)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GrabTempleGate, room::Maple.Room)
    Ahorn.drawImage(ctx, texture, -8, 0)
end

end