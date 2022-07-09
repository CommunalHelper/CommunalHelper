module CommunalHelperAttachedWallBooster

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/AttachedWallBooster" AttachedWallBooster(
    x::Integer,
    y::Integer,
    width::Integer=8,
    height::Integer=8,
    left::Bool=false,
    notCoreMode::Bool=false,
    legacyBoost::Bool=true,
)

const placements = Ahorn.PlacementDict(
    "Attached Wall Booster (Right) (Communal Helper)" => Ahorn.EntityPlacement(
        AttachedWallBooster,
        "rectangle",
        Dict{String,Any}(
            "left" => true,
        ),
    ),
    "Attached Wall Booster (Left) (Communal Helper)" => Ahorn.EntityPlacement(
        AttachedWallBooster,
        "rectangle",
        Dict{String,Any}(
            "left" => false,
        ),
    ),
)

Ahorn.minimumSize(entity::AttachedWallBooster) = 8, 8
Ahorn.resizable(entity::AttachedWallBooster) = false, true

function Ahorn.selection(entity::AttachedWallBooster)
    x, y = Ahorn.position(entity)
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, 8, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::AttachedWallBooster)
    left = get(entity.data, "left", false)

    height = Int(get(entity.data, "height", 8))
    tileHeight = div(height, 8)

    legacyBoost = get(entity.data, "legacyBoost", true)
    topTexture = legacyBoost ? "objects/wallBooster/fireTop00" : "objects/CommunalHelper/attachedWallBooster/fireTop00"
    bottomTexture = legacyBoost ? "objects/wallBooster/fireBottom00" : "objects/CommunalHelper/attachedWallBooster/fireBottom00"

    if left
        for i in 2:tileHeight-1
            Ahorn.drawImage(ctx, "objects/wallBooster/fireMid00", 0, (i - 1) * 8)
        end

        Ahorn.drawImage(ctx, topTexture, 0, 0)
        Ahorn.drawImage(ctx, bottomTexture, 0, (tileHeight - 1) * 8)

    else
        Ahorn.Cairo.save(ctx)
        Ahorn.scale(ctx, -1, 1)

        for i in 2:tileHeight-1
            Ahorn.drawImage(ctx, "objects/wallBooster/fireMid00", -8, (i - 1) * 8)
        end

        Ahorn.drawImage(ctx, topTexture, -8, 0)
        Ahorn.drawImage(ctx, bottomTexture, -8, (tileHeight - 1) * 8)

        Ahorn.restore(ctx)
    end
end

end
