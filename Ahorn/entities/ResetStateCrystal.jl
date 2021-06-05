module CommunalHelperResetCrystal

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/ResetStateCrystal" ResetCrystal(
    x::Integer,
    y::Integer,
    oneUse::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Reset State Crystal (Communal Helper)" => Ahorn.EntityPlacement(
        ResetCrystal,
    ),
)

const sprite = "objects/CommunalHelper/resetStateCrystal/ghostIdle00"

function Ahorn.selection(entity::ResetCrystal)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ResetCrystal) =
    Ahorn.drawSprite(ctx, sprite, 0, 0; tint=(0.35, 0.35, 0.35, 1.0))

end
