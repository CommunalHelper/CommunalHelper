module CommunalHelperDashThroughSpikes

using ..Ahorn, Maple
using Ahorn.CommunalHelper

const pos = :(spike(
    x::Integer,
    y::Integer,
)) 
const data = :(
    type::String="default",
)
const heightData = appendkwargs(pos, :(height::Integer=Maple.defaultSpikeHeight), data)
const widthData = appendkwargs(pos, :(width::Integer=Maple.defaultSpikeWidth), data)

@mapdefdata Entity "CommunalHelper/DashThroughSpikesLeft" DashThroughSpikesLeft heightData
@mapdefdata Entity "CommunalHelper/DashThroughSpikesUp" DashThroughSpikesUp widthData
@mapdefdata Entity "CommunalHelper/DashThroughSpikesRight" DashThroughSpikesRight heightData
@mapdefdata Entity "CommunalHelper/DashThroughSpikesDown" DashThroughSpikesDown widthData

const placements = Ahorn.PlacementDict()

const DashThroughSpikes = Dict{String,Type}(
    "up" => DashThroughSpikesUp,
    "down" => DashThroughSpikesDown,
    "left" => DashThroughSpikesLeft,
    "right" => DashThroughSpikesRight,
)

const spikeTypes = String["default", "outline", "cliffside", "reflection"]

const triggerSpikeColors = [
    (242, 90, 16, 255) ./ 255,
    (255, 0, 0, 255) ./ 255,
    (242, 16, 103, 255) ./ 255,
]

const DashThroughSpikesUnion = Union{
    DashThroughSpikesUp,
    DashThroughSpikesDown,
    DashThroughSpikesLeft,
    DashThroughSpikesRight,
}

for variant in spikeTypes, (dir, entity) in DashThroughSpikes
    key = "Dash-through Spikes ($(uppercasefirst(dir)), $(uppercasefirst(variant))) (Communal Helper)"
    placements[key] = Ahorn.EntityPlacement(
        entity,
        "rectangle",
        Dict{String,Any}(
            "type" => variant,
        )
    )
end

Ahorn.editingOptions(entity::DashThroughSpikesUnion) = Dict{String,Any}(
    "type" => spikeTypes,
)

const directions = Dict{String,String}(
    "CommunalHelper/DashThroughSpikesUp" => "up",
    "CommunalHelper/DashThroughSpikesDown" => "down",
    "CommunalHelper/DashThroughSpikesLeft" => "left",
    "CommunalHelper/DashThroughSpikesRight" => "right",
)

const offsets = Dict{String,Tuple{Integer,Integer}}(
    "up" => (4, -4),
    "down" => (4, 4),
    "left" => (-4, 4),
    "right" => (4, 4),
)

const renderOffsets = Dict{String,Tuple{Integer,Integer}}(
    "up" => (0, 0),
    "down" => (0, 0),
    "left" => (0, 0),
    "right" => (0, 0),
)

const rotations = Dict{String,Number}(
    "up" => 0,
    "right" => pi / 2,
    "down" => pi,
    "left" => 3 * pi / 2,
)

const rotationOffsets = Dict{String,Tuple{Number,Number}}(
    "up" => (3, -1),
    "right" => (4, 3),
    "down" => (5, 5),
    "left" => (-1, 4),
)

const resizeDirections = Dict{String,Tuple{Bool,Bool}}(
    "up" => (true, false),
    "down" => (true, false),
    "left" => (false, true),
    "right" => (false, true),
)

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::DashThroughSpikesUnion)
    direction = get(directions, entity.name, "up")
    theta = rotations[direction] - pi / 2

    width = Int(get(entity.data, "width", 0))
    height = Int(get(entity.data, "height", 0))

    x, y = Ahorn.position(entity)
    cx, cy = x + floor(Int, width / 2) - 8 * (direction == "left"),
    y + floor(Int, height / 2) - 8 * (direction == "up")

    Ahorn.drawArrow(ctx, cx, cy, cx + cos(theta) * 24, cy + sin(theta) * 24, Ahorn.colors.selection_selected_fc, headLength=6)
end

function Ahorn.selection(entity::DashThroughSpikesUnion)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    direction = get(directions, entity.name, "up")

    ox, oy = offsets[direction]

    return Ahorn.Rectangle(x + ox - 4, y + oy - 4, width, height)
end

Ahorn.minimumSize(entity::DashThroughSpikesUnion) = 8, 8

function Ahorn.resizable(entity::DashThroughSpikesUnion)
    direction = get(directions, entity.name, "up")
    return resizeDirections[direction]
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DashThroughSpikesUnion)
    variant = get(entity.data, "type", "default")
    direction = get(directions, entity.name, "up")
    TriggerSpikesOffset = renderOffsets[direction]

    width = get(entity.data, "width", 8)
    height = get(entity.data, "height", 8)

    for ox in 0:8:width-8, oy in 0:8:height-8
        drawX, drawY = (ox, oy) .+ offsets[direction] .+ TriggerSpikesOffset
        Ahorn.drawSprite(ctx, "danger/spikes/$(variant)_$(direction)00", drawX, drawY)
    end
end

const spikes = [
    DashThroughSpikesLeft,
    DashThroughSpikesUp,
    DashThroughSpikesRight,
    DashThroughSpikesDown,
]

for i in 0:3
    left, current, right, opposite = circshift(spikes, i)
    size = iseven(i) ? :(entity.width) : :(entity.height)
    dirCheck = isodd(i) ? :(horizontal) : :(!horizontal)

    @eval begin
        function Ahorn.flipped(entity::$current, horizontal::Bool)
            if $dirCheck
                return $opposite(entity.x, entity.y, $size, entity.type)
            end
        end

        function Ahorn.rotated(entity::$current, steps::Int)
            if steps > 0
                return Ahorn.rotated($right(entity.x, entity.y, $size, entity.type), steps - 1)

            elseif steps < 0
                return Ahorn.rotated($left(entity.x, entity.y, $size, entity.type), steps + 1)
            end

            return entity
        end
    end
end

end