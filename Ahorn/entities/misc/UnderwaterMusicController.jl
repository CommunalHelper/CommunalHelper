module CommunalHelperUnderwaterMusicController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/UnderwaterMusicController" Controller(
    x::Int,
    y::Int,
    enable::Bool=false,
    dashSFX::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Underwater Music Controller (Communal Helper)" => Ahorn.EntityPlacement(
        Controller,
    ),
)

const sprite = "objects/CommunalHelper/underwaterMusicController/icon"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Controller) = Ahorn.drawSprite(ctx, sprite, 0, 0)

Ahorn.selection(entity::Controller) = Ahorn.getSpriteRectangle(sprite, Ahorn.position(entity)...)

end

