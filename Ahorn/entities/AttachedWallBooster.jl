module CommunalHelperAttachedWallBooster
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/AttachedWallBooster" AttachedWallBooster(
                        x::Integer, y::Integer,
                        width::Integer = 8, height::Integer = 8,
                        left::Bool = false, notCoreMode::Bool = false)

                        
const placements = Ahorn.PlacementDict(
    "Attached Wall Booster (Right) (Communal Helper)" => Ahorn.EntityPlacement(
        AttachedWallBooster,
        "rectangle",
        Dict{String, Any}(
            "left" => true
        )
    ),
    "Attached Wall Booster (Left) (Communal Helper)" => Ahorn.EntityPlacement(
        AttachedWallBooster,
        "rectangle",
        Dict{String, Any}(
            "left" => false
        )
    )
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

    # Values need to be system specific integer
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    height = Int(get(entity.data, "height", 8))
    tileHeight = div(height, 8)

    if left
        for i in 2:tileHeight - 1
            Ahorn.drawImage(ctx, "objects/wallBooster/fireMid00", 0, (i - 1) * 8)
        end

        Ahorn.drawImage(ctx, "objects/wallBooster/fireTop00", 0, 0)
        Ahorn.drawImage(ctx, "objects/wallBooster/fireBottom00", 0, (tileHeight - 1) * 8)

    else
        Ahorn.Cairo.save(ctx)
        Ahorn.scale(ctx, -1, 1)

        for i in 2:tileHeight - 1
            Ahorn.drawImage(ctx, "objects/wallBooster/fireMid00", -8, (i - 1) * 8)
        end

        Ahorn.drawImage(ctx, "objects/wallBooster/fireTop00", -8, 0)
        Ahorn.drawImage(ctx, "objects/wallBooster/fireBottom00", -8, (tileHeight - 1) * 8)

        Ahorn.restore(ctx)
    end
end

end