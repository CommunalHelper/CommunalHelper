module CommunalHelperDreamCrumbleWallOnRumble

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/DreamCrumbleWallOnRumble" DreamCrumbleWallOnRumble(x::Integer, y::Integer,
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
	featherMode::Bool = false, oneUse::Bool = false, refillCount::Integer=-1, below::Bool=false, persistent::Bool=false)


const placements = Ahorn.PlacementDict(
    "Dream Crumble Wall On Rumble (Communal Helper)" => Ahorn.EntityPlacement(
        DreamCrumbleWallOnRumble,
		"rectangle"
    )
)

Ahorn.minimumSize(entity::DreamCrumbleWallOnRumble) = 8, 8
Ahorn.resizable(entity::DreamCrumbleWallOnRumble) = true, true

function Ahorn.selection(entity::DreamCrumbleWallOnRumble)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamCrumbleWallOnRumble, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderDreamBlock(ctx, 0, 0, width, height, entity.data)
end

end
