module CommunalHelperSyncedZipMoverActivationController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SyncedZipMoverActivationController" SyncedZipMoverActivationController(x::Integer, 
                                                                                                      y::Integer, 
                                                                                                      colorCode::String="ff00ff",
                                                                                                      zipMoverSpeedMultiplier::Number=1.0) 

const placements = Ahorn.PlacementDict(
    "Synced Zip Mover Activation Controller (Communal Helper)" => Ahorn.EntityPlacement(
        SyncedZipMoverActivationController,
        "point",
        Dict{String, Any}(),
    )
)

sprite = "objects/CommunalHelper/syncedZipMoverActivationController/syncedZipMoverActivationController.png"

function Ahorn.selection(entity::SyncedZipMoverActivationController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SyncedZipMoverActivationController, room::Maple.Room)
	Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end