module CommunalHelperSJPhotosensitiveFlagController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/PhotosensitiveFlagController" PhotosensitiveFlagController(x::Integer, y::Integer, flag::String="")

const placements = Ahorn.PlacementDict(
    "Photosensitive Flag Controller (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        PhotosensitiveFlagController
    )
)

const sprite = "objects/CommunalHelper/strawberryJam/photosensitiveFlagController/icon"

function Ahorn.selection(entity::PhotosensitiveFlagController)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PhotosensitiveFlagController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end