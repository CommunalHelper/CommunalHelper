module CommunalHelperSJFlagBreakerBox
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/FlagBreakerBox" FlagBreakerBox(x::Integer, y::Integer, width::Integer=32, height::Integer=32, flag::String="", music::String="", music_progress::Integer=-1, music_session::Bool=true, aliveState::Bool=true, flipX::Bool=false)

const placements = Ahorn.PlacementDict(
    "Flag Breaker Box (Strawberry Jam) (CommunalHelper)" => Ahorn.EntityPlacement(
        FlagBreakerBox,
        "rectangle"
    )
)

Ahorn.resizable(entity::FlagBreakerBox) = false, false

sprite = "objects/breakerBox/Idle00"

function Ahorn.selection(entity::FlagBreakerBox)
    x, y = Ahorn.position(entity)
    scaleX = get(entity, "flipX", false) ? -1 : 1
    return Ahorn.getSpriteRectangle(sprite, x, y, sx=scaleX, jx=0.25, jy=0.25)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FlagBreakerBox, room::Maple.Room)
    scaleX = get(entity, "flipX", false) ? -1 : 1
    Ahorn.drawSprite(ctx, sprite, 0, 0, sx=scaleX, jx=0.25, jy=0.25)
end

end