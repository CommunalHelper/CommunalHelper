module CommunalHelperAttachedFallingBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/AttachedFallingBlock" AttachedFallingBlock(
    x::Integer,
    y::Integer,
    width::Integer=8,
    height::Integer=8,
    tiletype::String="3",
    smoothDetach::Bool = true,
    climbFall=true,
)

const placements = Ahorn.PlacementDict(
    "Attached Falling Block (Communal Helper)" => Ahorn.EntityPlacement(
        AttachedFallingBlock,
        "rectangle",
        Dict{String, Any}(),
        Ahorn.tileEntityFinalizer,
    ),
)

Ahorn.editingOptions(entity::AttachedFallingBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::AttachedFallingBlock) = 8, 8
Ahorn.resizable(entity::AttachedFallingBlock) = true, true

Ahorn.selection(entity::AttachedFallingBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::AttachedFallingBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

end
