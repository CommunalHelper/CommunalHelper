module CommunalHelperDreamStrawberry

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamStrawberry" DreamDashBerry(
	x::Integer,
	y::Integer,
	order::Integer=-1,
	checkpointID::Integer=-1,
)

const placements = Ahorn.PlacementDict(
	"Dream Strawberry (Communal Helper)" => Ahorn.EntityPlacement(
		DreamDashBerry
	),
)

const sprite = "collectables/CommunalHelper/dreamberry/wings01"
const seedSprite = "collectables/CommunalHelper/dreamberry/seed02"

Ahorn.nodeLimits(entity::DreamDashBerry) = 0, -1

function Ahorn.selection(entity::DreamDashBerry)
    x, y = Ahorn.position(entity)

    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]
    
    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = node
        push!(res, Ahorn.getSpriteRectangle(seedSprite, nx, ny))
    end

    return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamDashBerry)
    x, y = Ahorn.position(entity)

    for node in get(entity.data, "nodes", ())
        nx, ny = node
        Ahorn.drawLines(ctx, Tuple{Number, Number}[(x, y), (nx, ny)], Ahorn.colors.selection_selected_fc)
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamDashBerry, room::Maple.Room)
	x, y = Ahorn.position(entity)
	Ahorn.drawSprite(ctx, sprite, x, y)

    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = node
        Ahorn.drawSprite(ctx, seedSprite, nx, ny)
    end
end

end