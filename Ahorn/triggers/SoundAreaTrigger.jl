module CommunalHelperSoundAreaTrigger

using ..Ahorn, Maple

@mapdef Trigger "CommunalHelper/SoundAreaTrigger" SoundAreaTrigger(
    x::Integer, 
    y::Integer, 
    width::Integer = Maple.defaultTriggerWidth, 
    height::Integer = Maple.defaultTriggerHeight,
    nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[],
    event::String="",
)

const placements = Ahorn.PlacementDict(
    "Sound Area Trigger (Communal Helper)" => Ahorn.EntityPlacement(
        SoundAreaTrigger,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            x, y = Ahorn.position(entity)
            width = Int(get(entity.data, "width", 8))
            height = Int(get(entity.data, "height", 8))
            entity.data["nodes"] = [(x + width / 2, y + height / 2)]
        end
    ),
)

Ahorn.nodeLimits(entity::SoundAreaTrigger) = 1, 1

end