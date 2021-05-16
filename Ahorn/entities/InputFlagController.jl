module CommunalHelperInputFlagController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/InputFlagController" Controller(
    x::Int,
    y::Int,
    flags::String="",
    toggle::Bool=true,
    resetFlags::Bool=false,
    delay::Float64=0.0,
    grabOverride::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Input Flag Controller (Communal Helper)" => Ahorn.EntityPlacement(
        Controller,
    ),
)

const sprite = "objects/CommunalHelper/inputFlagController/icon"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Controller) = Ahorn.drawSprite(ctx, sprite, 0, 0)

Ahorn.selection(entity::Controller) = Ahorn.getSpriteRectangle(sprite, Ahorn.position(entity)...)

end

