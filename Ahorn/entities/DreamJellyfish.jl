module CommunalHelperDreamJellyfish

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamJellyfish" DreamJellyfish(
    x::Integer,
    y::Integer,
    bubble::Bool=false,
    tutorial::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Dream Jellyfish (Communal Helper)" => Ahorn.EntityPlacement(
        DreamJellyfish,
    ),
    "Dream Jellyfish (Floating) (Communal Helper)" => Ahorn.EntityPlacement(
        DreamJellyfish,
        "point",
        Dict{String, Any}(
            "bubble" => true,
        ),
    )
)

const sprite = "objects/CommunalHelper/dreamJellyfish/jello"

function Ahorn.selection(entity::DreamJellyfish)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 14, y - 15, 30, 19)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamJellyfish, room::Maple.Room)
    Ahorn.drawSprite(ctx, sprite, 0, 0)

    if get(entity, "bubble", false)
        curve = Ahorn.SimpleCurve((-7, -1), (7, -1), (0, -6))
        Ahorn.drawSimpleCurve(ctx, curve, (1.0, 1.0, 1.0, 1.0), thickness=1)
    end
end

end