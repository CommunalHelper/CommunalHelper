module CommunalHelperStationBlockTrack
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/StationBlockTrack" StationBlockTrack(
                    x::Integer, y::Integer,
                    width::Integer = 24, height::Integer = 24,
                    horizontal::Bool = false)

const placements = Ahorn.PlacementDict(
    "Station Block Track (Vertical) (Communal Helper)" => Ahorn.EntityPlacement(
        StationBlockTrack,
        "rectangle",
        Dict{String, Any}(
            "horizontal" => false
        )
    ),
    "Station Block Track (Horizontal) (Communal Helper)" => Ahorn.EntityPlacement(
        StationBlockTrack,
        "rectangle",
        Dict{String, Any}(
            "horizontal" => true
        )
    )
)

Ahorn.minimumSize(entity::StationBlockTrack) = 24, 24

Ahorn.resizable(entity::StationBlockTrack) = Bool(get(entity.data, "horizontal", false)), !(Bool(get(entity.data, "horizontal", false)))

function Ahorn.selection(entity::StationBlockTrack)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))
    horiz = Bool(get(entity.data, "horizontal", false))
	
    return horiz ? Ahorn.Rectangle(x, y, width, 8) : Ahorn.Rectangle(x, y, 8, height)
end

nodeSprite = "objects/CommunalHelper/stationBlock/tracks/track/ball"
vTrack = "objects/CommunalHelper/stationBlock/tracks/track/pipeV"
hTrack = "objects/CommunalHelper/stationBlock/tracks/track/pipeH"

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::StationBlockTrack, room::Maple.Room)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))

    horiz = Bool(get(entity.data, "horizontal", false))
    
    if horiz
        tilesWidth = div(width, 8)

        for i in 2:tilesWidth - 1
            Ahorn.drawImage(ctx, hTrack, x + (i - 1) * 8, y)
        end

        Ahorn.drawImage(ctx, nodeSprite, x, y)
        Ahorn.drawImage(ctx, nodeSprite, x + width - 8, y)
    else
        tilesHeight = div(height, 8)

        for i in 2:tilesHeight - 1
            Ahorn.drawImage(ctx, vTrack, x, y + (i - 1) * 8)
        end

        Ahorn.drawImage(ctx, nodeSprite, x, y)
        Ahorn.drawImage(ctx, nodeSprite, x, y + height - 8)
    end

end

end