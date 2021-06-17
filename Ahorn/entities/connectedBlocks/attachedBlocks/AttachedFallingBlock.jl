module CommunalHelperAttachedFallingBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/AttachedFallingBlock" AttachedFallingBlock(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    height::Integer=Maple.defaultBlockWidth,
    tiletype::String="3",
    smoothDetach::Bool=true,
    climbFall::Bool=true,
)

const placements = Ahorn.PlacementDict(
    "Falling Block (Attached) (Communal Helper)" => Ahorn.EntityPlacement(
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
