module CommunalHelperGlowController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/GlowController" GlowController(
    x::Integer, y::Integer,
    lightWhitelist::String="",
    lightBlacklist::String="",
    lightColor::String="FFFFFF",
    lightAlpha::Real=1.0,
    lightStartFade::Integer=24,
    lightEndFade::Integer=48,
    lightOffsetX::Integer=0,
    lightOffsetY::Integer=0,
    bloomWhitelist::String="",
    bloomBlacklist::String="",
    bloomAlpha::Real=1.0,
    bloomRadius::Real=8.0,
    bloomOffsetX::Integer=0,
    bloomOffsetY::Integer=0
)

const placements = Ahorn.PlacementDict(
    "Glow Controller (CommunalHelper)" => Ahorn.EntityPlacement(
        GlowController
    )
)

Ahorn.editingOrder(entity::GlowController) = String[
    "x", "y", "width", "height",
    "lightWhitelist",
    "lightBlacklist",
    "lightColor",
    "lightAlpha",
    "lightStartFade",
    "lightEndFade",
    "lightOffsetX",
    "lightOffsetY",
    "bloomWhitelist",
    "bloomBlacklist",
    "bloomAlpha",
    "bloomRadius",
    "bloomOffsetX",
    "bloomOffsetY"
]

const sprite = "objects/CommunalHelper/glowController/icon"

function Ahorn.selection(entity::GlowController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GlowController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end
