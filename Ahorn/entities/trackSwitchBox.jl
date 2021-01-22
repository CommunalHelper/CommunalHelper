module CommunalHelperTrackSwitchBox
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/TrackSwitchBox" TrackSwitchBox(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Track Switch Box" => Ahorn.EntityPlacement(
        TrackSwitchBox
    )
)

sprite = "objects/CommunalHelper/trackSwitchBox/idle00"

function Ahorn.selection(entity::TrackSwitchBox)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x, y, 32, 32)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TrackSwitchBox, room::Maple.Room)
    Ahorn.drawRectangle(ctx, 1, 1, 30, 30, (0.85, 0.3, 0.45, 1.0))
    Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.25, jy=0.25)
end

end