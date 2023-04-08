module CommunalHelperSJSolarElevator

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/SolarElevator" SolarElevator(
    x::Integer,
    y::Integer,
    distance::Integer=128,
    bgDepth::Integer=9000,
    time::Number=3.0,
    delay::Number=1.0,
    oneWay::Bool=false,
    startPosition::String="Closest",
    moveSfx::String="event:/CommunalHelperEvents/game/strawberryJam/game/solar_elevator/elevate",
    haltSfx::String="event:/CommunalHelperEvents/game/strawberryJam/game/solar_elevator/halt",
    requiresHoldable::Bool=false,
    holdableHintDialog::String="communalhelper_entities_strawberry_jam_solar_elevator_hint",
    reskinDirectory::String="",
)

const placements = Ahorn.PlacementDict(
    "Solar Elevator (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        SolarElevator,
        "point",
    ),
)

const startPositions = String["Closest", "Top", "Bottom"]

Ahorn.editingOptions(entity::SolarElevator) = Dict{String, Any}(
    "startPosition" => startPositions,
)

function Ahorn.selection(entity::SolarElevator)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 24, y - 70, 48, 80)
end

const transparentTint = (1.0, 1.0, 1.0, 1.0) .* 0.45

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SolarElevator, room::Maple.Room)
    rail = Ahorn.getSprite("objects/CommunalHelper/strawberryJam/solarElevator/rails", "Gameplay")
    railOffsetX = -floor(Int, rail.width / 2)
    distance = max(0, Int(get(entity.data, "distance", 128)))
    y = 0
    while y < distance + 60
        Ahorn.drawImage(ctx, rail, railOffsetX, -rail.height - y)
        y += rail.height
    end
    
    front = Ahorn.getSprite("objects/CommunalHelper/strawberryJam/solarElevator/front", "Gameplay")
    back = Ahorn.getSprite("objects/CommunalHelper/strawberryJam/solarElevator/back", "Gameplay")

    oxf, oyf = -floor(front.width / 2), -front.height
    oxb, oyb = -floor(back.width / 2), -back.height

    Ahorn.drawImage(ctx, back, oxb, oyb + 10)
    Ahorn.drawImage(ctx, front, oxf, oyf + 10)

    Ahorn.drawImage(ctx, back, oxb, oyb + 10 - distance, tint=transparentTint)
    Ahorn.drawImage(ctx, front, oxf, oyf + 10 - distance, tint=transparentTint)
end

end