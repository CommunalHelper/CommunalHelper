module CommunalHelperTrackSwitchBox

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/TrackSwitchBox" TrackSwitchBox(
    x::Integer,
    y::Integer,
    globalSwitch::Bool=false,
    floaty::Bool=true,
    bounce::Bool=true,
)

const placements = Ahorn.PlacementDict(
    "Track Switch Box (Communal Helper)" => Ahorn.EntityPlacement(
        TrackSwitchBox,
        "point",
    ),
    "Track Switch Box (Session Switch) (Communal Helper)" => Ahorn.EntityPlacement(
        TrackSwitchBox,
        "point",
        Dict{String,Any}(
            "globalSwitch" => true,
        ),
    ),
)

const sprite = "objects/CommunalHelper/trackSwitchBox/idle00"

function Ahorn.selection(entity::TrackSwitchBox)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y, 32, 32)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TrackSwitchBox, room::Maple.Room)
    Ahorn.drawRectangle(ctx, 1, 1, 30, 30, (0.3, 0.3, 0.4, 1.0))
    Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.25, jy=0.25)
end

end
