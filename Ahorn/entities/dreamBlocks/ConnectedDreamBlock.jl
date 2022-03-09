module CommunalHelperConnectedDreamBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper
using Ahorn.CommunalHelperEntityPresets: CustomDreamBlockData

@mapdefdata Entity "CommunalHelper/ConnectedDreamBlock" ConnectedDreamBlock CustomDreamBlockData

const placements = Ahorn.PlacementDict(
    "Connected Dream Block (Normal) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedDreamBlock,
        "rectangle",
    ),
    "Connected Dream Block (Feather Mode) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedDreamBlock,
        "rectangle",
        Dict{String,Any}(
            "featherMode" => true,
        ),
    ),
    "Connected Dream Block (Normal, One Use) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedDreamBlock,
        "rectangle",
        Dict{String,Any}(
            "oneUse" => true,
        ),
    ),
    "Connected Dream Block (Feather Mode, One Use) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedDreamBlock,
        "rectangle",
        Dict{String,Any}(
            "featherMode" => true,
            "oneUse" => true,
        ),
    ),
)

Ahorn.minimumSize(entity::ConnectedDreamBlock) = 8, 8
Ahorn.resizable(entity::ConnectedDreamBlock) = true, true

Ahorn.selection(entity::ConnectedDreamBlock) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ConnectedDreamBlock)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    renderDreamBlock(ctx, 0, 0, width, height, entity.data)
end

end
