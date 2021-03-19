module CommunalHelperDreamTunnelEntry

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamTunnelEntry" DreamTunnelEntry(x::Integer, y::Integer, 
	overrideAllowStaticMovers=false)

const placements = Ahorn.PlacementDict(
	"DreamTunnelEntry ($dir) (Communal Helper)" => Ahorn.EntityPlacement(
		DreamTunnelEntry,
		"rectangle",
		Dict{String, Any}(
			"orientation" => dir
		),
	) for dir in Maple.spike_directions
)

Ahorn.editingOptions(entity::DreamTunnelEntry) = Dict{String, Any}(
    "orientation" => Maple.spike_directions
)

Ahorn.minimumSize(entity::DreamTunnelEntry) = 8, 8

const resizeDirections = Dict{String, Tuple{Bool, Bool}}(
	"Up" => (true, false),
	"Down" => (true, false),
	"Left" => (false, true),
	"Right" => (false, true),
)

function Ahorn.resizable(entity::DreamTunnelEntry)
	orientation = get(entity.data, "orientation", "Up")
	return resizeDirections[orientation]
end

function Ahorn.selection(entity::DreamTunnelEntry)
	x, y = Ahorn.position(entity)

	width = Int(get(entity.data, "width", 8))
	height = Int(get(entity.data, "height", 8))

	orientation = get(entity.data, "orientation", "Up")

	return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamTunnelEntry)
	orientation = get(entity.data, "orientation", "Up")

	width = Int(get(entity.data, "width", 8))
	height = Int(get(entity.data, "height", 8))
	
	x, y = Ahorn.position(entity)
	ox, oy = x + width * (orientation == "Right"), y + height * (orientation == "Down")
	cx, cy = x + width * in(orientation, ("Up", "Down", "Right")), y + height * in(orientation, ("Down", "Left", "Right"))

	Ahorn.drawRectangle(ctx, x, y, width, height, (0.0, 0.0, 0.0, 0.4))
	Ahorn.drawLines(ctx, ((ox, oy), (cx, cy)), (1.0, 1.0, 1.0, 1.0), thickness=1)

end

end 