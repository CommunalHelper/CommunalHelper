module CommunalHelperSyncedZipMoverActivationController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SyncedZipMoverActivationController" Controller(
    x::Integer,
    y::Integer,
    colorCode::String="ff00ff",
    zipMoverSpeedMultiplier::Number=1.0,
)

const placements = Ahorn.PlacementDict(
    "Synced Zip Mover Activator (Communal Helper)" => Ahorn.EntityPlacement(
        Controller,
    ),
)

const sprite = "objects/CommunalHelper/syncedZipMoverActivationController/syncedZipMoverActivationController"

function Ahorn.selection(entity::Controller)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Controller)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end
