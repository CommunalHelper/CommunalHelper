module CommunalHelperInputFlagController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/InputFlagController" Controller(x::Int, y::Int, flags::String="", toggle::Bool=true, resetFlags::Bool=false, delay::Float64=0.0)

const placements = Ahorn.PlacementDict(
   "Input Flag Controller (Communal Helper)" => Ahorn.EntityPlacement(
      Controller
   )
)

end 