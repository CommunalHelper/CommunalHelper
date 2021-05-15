module CommunalHelperMusicParamTrigger

using ..Ahorn, Maple

@mapdef Trigger "CommunalHelper/MusicParamTrigger" MusicParamTrigger(
					x::Integer, y::Integer, 
					width::Integer = Maple.defaultTriggerWidth, 
					height::Integer = Maple.defaultTriggerHeight,
					param::String="", enterValue::Number=1.0, exitValue::Number=0.0)
					

const placements = Ahorn.PlacementDict(
	"Music Paramater Trigger (Communal Helper)" => Ahorn.EntityPlacement(
		MusicParamTrigger,
		"rectangle"
	)
)

end