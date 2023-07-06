module CommunalHelperTimedTriggerSpikes

using ..Ahorn, Maple
using Ahorn.CommunalHelper

const pos = :(spike(
    x::Integer,
    y::Integer,
)) 
const data = :(
    type::String="default",
    Delay::Number=0.4,
    WaitForPlayer::Bool=false,
    Grouped::Bool=false,
    Rainbow::Bool=false,
    TriggerAlways::Bool=false,
)
const heightData = appendkwargs(pos, :(height::Integer=Maple.defaultSpikeHeight), data)
const widthData = appendkwargs(pos, :(width::Integer=Maple.defaultSpikeWidth), data)

@mapdefdata Entity "CommunalHelper/TimedTriggerSpikesLeft" TimedTriggerSpikesLeft heightData
@mapdefdata Entity "CommunalHelper/TimedTriggerSpikesUp" TimedTriggerSpikesUp widthData
@mapdefdata Entity "CommunalHelper/TimedTriggerSpikesRight" TimedTriggerSpikesRight heightData
@mapdefdata Entity "CommunalHelper/TimedTriggerSpikesDown" TimedTriggerSpikesDown widthData

const placements = Ahorn.PlacementDict()

const TimedTriggerSpikes = Dict{String,Type}(
    "up" => TimedTriggerSpikesUp,
    "down" => TimedTriggerSpikesDown,
    "left" => TimedTriggerSpikesLeft,
    "right" => TimedTriggerSpikesRight,
)

const spikeTypes = String["default", "outline", "cliffside", "reflection"]

const triggerSpikeColors = [
    (242, 90, 16, 255) ./ 255,
    (255, 0, 0, 255) ./ 255,
    (242, 16, 103, 255) ./ 255,
]

const TriggerSpikesUnion = Union{
    TimedTriggerSpikesUp,
    TimedTriggerSpikesDown,
    TimedTriggerSpikesLeft,
    TimedTriggerSpikesRight,
}

for variant in spikeTypes, (dir, entity) in TimedTriggerSpikes
    key = "Timed Trigger Spikes ($(uppercasefirst(dir)), $(uppercasefirst(variant))) (Communal Helper)"
    placements[key] = Ahorn.EntityPlacement(
        entity,
        "rectangle",
        Dict{String,Any}(
            "type" => variant,
        )
    )
end

Ahorn.editingOptions(entity::TriggerSpikesUnion) = Dict{String,Any}(
    "type" => spikeTypes,
    "Grouped" => Dict{String,Bool}(
        "Not Grouped" => false,
        "Grouped (Requires Max's Maddie Hand)" => true,
    ),
    "Rainbow" => Dict{String,Bool}(
        "Default" => false,
        "Rainbow (Requires Viv's Helper)" => true,
    ),
)

const directions = Dict{String,String}(
    "CommunalHelper/TimedTriggerSpikesUp" => "up",
    "CommunalHelper/TimedTriggerSpikesDown" => "down",
    "CommunalHelper/TimedTriggerSpikesLeft" => "left",
    "CommunalHelper/TimedTriggerSpikesRight" => "right",
)

const offsets = Dict{String,Tuple{Integer,Integer}}(
    "up" => (4, -4),
    "down" => (4, 4),
    "left" => (-4, 4),
    "right" => (4, 4),
)

const renderOffsets = Dict{String,Tuple{Integer,Integer}}(
    "up" => (0, 5),
    "down" => (0, -4),
    "left" => (5, 0),
    "right" => (-4, 0),
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

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::TriggerSpikesUnion)
    direction = get(directions, entity.name, "up")
    theta = rotations[direction] - pi / 2

    width = Int(get(entity.data, "width", 0))
    height = Int(get(entity.data, "height", 0))

    x, y = Ahorn.position(entity)
    cx, cy = x + floor(Int, width / 2) - 8 * (direction == "left"),
    y + floor(Int, height / 2) - 8 * (direction == "up")

    Ahorn.drawArrow(ctx, cx, cy, cx + cos(theta) * 24, cy + sin(theta) * 24, Ahorn.colors.selection_selected_fc, headLength=6)
end

function Ahorn.selection(entity::TriggerSpikesUnion)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    direction = get(directions, entity.name, "up")

    ox, oy = offsets[direction]

    return Ahorn.Rectangle(x + ox - 4, y + oy - 4, width, height)
end

Ahorn.minimumSize(entity::TriggerSpikesUnion) = 8, 8

function Ahorn.resizable(entity::TriggerSpikesUnion)
    direction = get(directions, entity.name, "up")
    return resizeDirections[direction]
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TriggerSpikesUnion)
    variant = get(entity.data, "type", "default")
    direction = get(directions, entity.name, "up")
    TriggerSpikesOffset = renderOffsets[direction]

    width = get(entity.data, "width", 8)
    height = get(entity.data, "height", 8)

    if variant == "dust"
        rng = Ahorn.getSimpleEntityRng(entity)

        updown = direction == "up" || direction == "down"

        for ox in 0:8:width-8, oy in 0:8:height-8
            color1 = rand(rng, triggerSpikeColors)
            color2 = rand(rng, triggerSpikeColors)

            drawX = ox + rotationOffsets[direction][1]
            drawY = oy + rotationOffsets[direction][2]

            Ahorn.drawSprite(ctx, "danger/triggertentacle/wiggle_v06", drawX, drawY, rot=rotations[direction], tint=color1)
            Ahorn.drawSprite(ctx, "danger/triggertentacle/wiggle_v03", drawX + 3 * updown, drawY + 3 * !updown, rot=rotations[direction], tint=color2)
        end
    else
        for ox in 0:8:width-8, oy in 0:8:height-8
            drawX, drawY = (ox, oy) .+ offsets[direction] .+ TriggerSpikesOffset
            Ahorn.drawSprite(ctx, "danger/spikes/$(variant)_$(direction)00", drawX, drawY)
        end
    end
end

const spikes = [
    TimedTriggerSpikesLeft,
    TimedTriggerSpikesUp,
    TimedTriggerSpikesRight,
    TimedTriggerSpikesDown,
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
