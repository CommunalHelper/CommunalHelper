module CommunalHelperShieldedRefill
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/ShieldedRefill" ShieldedRefill(x::Integer, y::Integer, 
	twoDashes::Bool = false, oneUse::Bool = false, bubbleRepel::Bool = true)    

const placements = Ahorn.PlacementDict(
    "Shielded Refill (Two Dashes) (Communal Helper)" => Ahorn.EntityPlacement(
        ShieldedRefill,
        "point",
        Dict{String, Any}(
            "twoDashes" => true
        )
    ),
    "Shielded Refill (Communal Helper)" => Ahorn.EntityPlacement(
        ShieldedRefill,
        "point",
        Dict{String, Any}(
            "twoDashes" => false
        )
    )    
)

const spriteOneDash = "objects/refill/idle00"
const spriteTwoDash = "objects/refillTwo/idle00"

getSprite(entity::ShieldedRefill) = get(entity.data, "twoDashes", false) ? spriteTwoDash : spriteOneDash

function Ahorn.selection(entity::ShieldedRefill)
    x, y = Ahorn.position(entity)
    sprite = getSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ShieldedRefill)
    Ahorn.Cairo.save(ctx)
    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1);

    Ahorn.drawCircle(ctx, 0, 0, 8, (1.0, 1.0, 1.0, 1.0))
    Ahorn.Cairo.restore(ctx)

    sprite = getSprite(entity)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end