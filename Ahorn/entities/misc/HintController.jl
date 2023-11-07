module CommunalHelperHintController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/HintController" HintController(
    x::Integer, y::Integer, 
    titleDialog::String="communalhelper_entities_hint_controller_menu",
    dialogIds::String="",
    singleUses::String="",
    selectorCounter::String="",
    selectNextHint::Bool=false
)

const placements = Ahorn.PlacementDict(
    "Hint Controller (CommunalHelper)" => Ahorn.EntityPlacement(
        HintController
    )
)

Ahorn.editingOrder(entity::GlowController) = String[
    "x","y","titleDialog","dialogIDs","singleUses","selectorCounter","selectNextHint"
]

const sprite = "objects/CommunalHelper/hintController/icon"

function Ahorn.selection(entity::GlowController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GlowController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end