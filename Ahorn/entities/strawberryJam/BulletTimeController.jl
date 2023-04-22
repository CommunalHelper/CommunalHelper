module CommunalHelperSJBulletTimeController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/BulletTimeController" BTController(x::Integer, y::Integer, timerate::Number = 0.5, flag::String="", minDashes::Integer)

const placements = Ahorn.PlacementDict(
    "Bullet Time Controller (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        BTController,
        "rectangle"
    )
)

function Ahorn.selection(entity::BTController)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y, 8, 8)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BTController, room::Maple.Room)
    Ahorn.drawRectangle(ctx, 0, 0, 8, 8, Ahorn.defaultWhiteColor, Ahorn.defaultBlackColor)
end


end