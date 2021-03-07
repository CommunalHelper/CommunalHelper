module CommunalHelperManualCassetteController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/ManualCassetteController" ManualCassetteController(x::Integer, y::Integer) 

const placements = Ahorn.PlacementDict(
    "Manual Cassette Controller (Communal Helper)" => Ahorn.EntityPlacement(
        ManualCassetteController,
        "point",
        Dict{String, Any}(),
    )
)

const sprite = "objects/CommunalHelper/manualCassetteController/icon.png"

function Ahorn.selection(entity::ManualCassetteController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ManualCassetteController)
	Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end