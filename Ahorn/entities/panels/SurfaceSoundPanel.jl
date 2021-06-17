module CommunalHelperSurfaceSoundPanel

using ..Ahorn, Maple
using Ahorn.CommunalHelper

const directions = String["Up", "Left", "Right"]

@mapdef Entity "CommunalHelper/SurfaceSoundPanel" Panel(
    x::Integer,
    y::Integer,
    soundIndex::Integer=11,
)

const placements = Ahorn.PlacementDict(
    "Sound Surface Panel ($dir) (Communal Helper)" => Ahorn.EntityPlacement(
        Panel,
        "rectangle",
        Dict{String,Any}(
            "orientation" => dir,
        ),
    ) for dir in directions
)

Ahorn.editingOptions(entity::Panel) = Dict{String,Any}(
    "orientation" => Maple.spike_directions,
    "soundIndex" => CommunalHelper.surfaceSounds,
)

Ahorn.minimumSize(entity::Panel) = 8, 8

const resizeDirections = Dict{String,Tuple{Bool,Bool}}(
    "Up" => (true, false),
    "Left" => (false, true),
    "Right" => (false, true),
)

function Ahorn.resizable(entity::Panel)
    orientation = get(entity.data, "orientation", "Up")
    return resizeDirections[orientation]
end

function Ahorn.selection(entity::Panel)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::Panel)
    orientation = get(entity.data, "orientation", "Up")

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    x, y = Ahorn.position(entity)
    ox, oy = x + width * (orientation == "Right"), y + height * (orientation == "Down")
    cx, cy = x + width * in(orientation, ("Up", "Down", "Right")),
    y + height * in(orientation, ("Down", "Left", "Right"))

    Ahorn.drawRectangle(ctx, x, y, width, height, (1.0, 0.5, 0.0, 0.4))
    Ahorn.drawLines(ctx, ((ox, oy), (cx, cy)), (1.0, 1.0, 1.0, 1.0), thickness=1)
end

end
