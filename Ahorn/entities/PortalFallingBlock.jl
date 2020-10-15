module CommunalHelperPortalFallingBlock
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/PortalFallingBlock" PortalFallingBlock(
								x::Integer, y::Integer, 
								width::Integer = 8, 
								height::Integer = 8,
								tiletype::String = "3",
								autoFalling::Bool = false)
								
const placements = Ahorn.PlacementDict(
	"Portal Falling Block (Communal Helper)" => Ahorn.EntityPlacement(
		PortalFallingBlock,
		"rectangle",
		Dict{String, Any}(),
		Ahorn.tileEntityFinalizer
	),
	"Portal Falling Block (Autofalling) (Communal Helper)" => Ahorn.EntityPlacement(
		PortalFallingBlock,
		"rectangle",
		Dict{String, Any}(
			"autoFalling" => true
		),
		Ahorn.tileEntityFinalizer
	)
)

Ahorn.editingOptions(entity::PortalFallingBlock) = Dict{String, Any}(
	"tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.minimumSize(entity::PortalFallingBlock) = 8, 8
Ahorn.resizable(entity::PortalFallingBlock) = true, true

Ahorn.selection(entity::PortalFallingBlock) = Ahorn.getEntityRectangle(entity)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::PortalFallingBlock, room::Maple.Room) = Ahorn.drawTileEntity(ctx, room, entity)

end
