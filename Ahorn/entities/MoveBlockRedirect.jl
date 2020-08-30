module CommunalHelperMoveBlockRedirect

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/MoveBlockRedirect" MoveBlockRedirect(x::Integer, y::Integer, 
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, 
	direction::String="Up", fastRedirect::Bool=false)

const placements = Ahorn.PlacementDict(
	"Move Block Redirect (Communal Helper)" => Ahorn.EntityPlacement(
		MoveBlockRedirect,
		"rectangle"
	)
)

Ahorn.editingOptions(entity::MoveBlockRedirect) = Dict{String, Any}(
    "direction" => Maple.move_block_directions
)
Ahorn.minimumSize(entity::MoveBlockRedirect) = Maple.defaultBlockWidth, Maple.defaultBlockHeight 
Ahorn.resizable(entity::MoveBlockRedirect) = true, true

Ahorn.selection(entity::MoveBlockRedirect) = Ahorn.getEntityRectangle(entity)

function getRotation(dir::String)
	if dir == "Up"
		return pi * 1.5
	elseif dir == "Right"
		return 0
	elseif dir == "Down"
		return pi/2
	elseif dir == "Left"
		return pi
	elseif (fAngle = tryparse(Float64, dir)) != nothing
		return fAngle
	else 
		return 0
	end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::MoveBlockRedirect) 
	width = Int(get(entity.data, "width", 32))
   height = Int(get(entity.data, "height", 32))
	
	direction = String(get(entity.data, "direction", "Up"))
	 
	Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.0,0.0,0.0,0.0), (1.0, 0.82, 0.0, 0.75))
	
	sprite = Ahorn.getSprite("objects/CommunalHelper/moveBlockRedirect/bigarrow", "Gameplay")
	
	# finicky
	Ahorn.Cairo.save(ctx)
	Ahorn.Cairo.translate(ctx, width/2, height/2)
	Ahorn.Cairo.rotate(ctx, getRotation(direction))
	Ahorn.Cairo.translate(ctx, -sprite.width/2, -sprite.height/2)
	Ahorn.drawImage(ctx, sprite, 0, 0, alpha=0.75)
	Ahorn.Cairo.restore(ctx)
end

end 