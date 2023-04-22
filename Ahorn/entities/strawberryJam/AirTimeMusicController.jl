module CommunalHelperSJAirTimeMusicController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/AirTimeMusicController" AirTimeMusicController(
    x::Integer,
    y::Integer,
    activationThreshold::Number=0.5,
    musicParam=""
)

const placements = Ahorn.PlacementDict(
    "Airtime Music Controller (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        AirTimeMusicController
    )
)

const sprite = "objects/CommunalHelper/strawberryJam/airTimeMusicController/icon"

function Ahorn.selection(entity::AirTimeMusicController)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::AirTimeMusicController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end