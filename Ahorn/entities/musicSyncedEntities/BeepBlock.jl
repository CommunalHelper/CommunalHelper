module CommunalHelperBeepBlock

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/BeepBlock" BeepBlock(
    x::Integer,
    y::Integer,
    width::Integer=24,
    height::Integer=24,
)

const placements = Ahorn.PlacementDict(
    "Beep Block (Communal Helper)" => Ahorn.EntityPlacement(
        BeepBlock,
        "rectangle",
        Dict{String,Any}(),
    ),
)

Ahorn.resizable(entity::BeepBlock) = true, true
Ahorn.minimumSize(entity::BeepBlock) = 8, 8

function Ahorn.selection(entity::BeepBlock)
    return Ahorn.getEntityRectangle(entity)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BeepBlock, room::Maple.Room)
    width = Int(entity.data["width"])
    height = Int(entity.data["height"])

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.0, 0.5, 0.5, 1.0))
end

end