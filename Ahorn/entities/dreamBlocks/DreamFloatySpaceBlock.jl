module CommunalHelperDreamFloatySpaceBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/DreamFloatySpaceBlock" DreamFloatySpaceBlock(x::Integer, y::Integer,
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
	featherMode::Bool = false, oneUse::Bool = false, refillCount::Integer=-1, below::Bool=false)


const placements = Ahorn.PlacementDict(
    "Dream Floaty Space Block (Communal Helper)" => Ahorn.EntityPlacement(
        DreamFloatySpaceBlock,
		"rectangle"
    )
)

Ahorn.minimumSize(entity::DreamFloatySpaceBlock) = 8, 8
Ahorn.resizable(entity::DreamFloatySpaceBlock) = true, true

Ahorn.selection(entity::DreamFloatySpaceBlock) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamFloatySpaceBlock, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderDreamBlock(ctx, 0, 0, width, height, entity.data)
end

end
