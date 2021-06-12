module CommunalHelperDreamBooster

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamBooster" DreamBooster(
    x::Integer,
    y::Integer,
    hidePath::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Dream Booster (Communal Helper)" => Ahorn.EntityPlacement(
        DreamBooster,
        "line",
    ),
    "Dream Booster (Hidden Path) (Communal Helper)" => Ahorn.EntityPlacement(
        DreamBooster,
        "line",
        Dict{String,Any}(
            "hidePath" => true,
        ),
    ),
)

Ahorn.nodeLimits(entity::DreamBooster) = 1, 1

function Ahorn.selection(entity::DreamBooster)
    x, y = Ahorn.position(entity)
    endX, endY = Int.(entity.data["nodes"][1])

    return [
        Ahorn.Rectangle(x - 9, y - 9, 18, 18),
        Ahorn.Rectangle(endX - 9, endY - 9, 18, 18),
    ]
end

const dreamBoosterSprite = "objects/CommunalHelper/boosters/dreamBooster/idle00"

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamBooster, room::Maple.Room)
    x, y = Ahorn.position(entity)
    node = entity.data["nodes"][1]

    Ahorn.drawLines(ctx, [(x, y), node], (1.0, 1.0, 1.0, 1.0), thickness=1)
    Ahorn.drawSprite(ctx, dreamBoosterSprite, x, y)
end

end
