module CommunalHelperSJMomentumBlock

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/MomentumBlock" MomentumBlock(x::Integer, y::Integer, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, speed::Number=10.0, direction::Number=0.0, speedFlagged::Number=10.0, directionFlagged::Number=0.0, startColor::String="9a0000", endColor::String="00ffff", flag::String="")

const placements = Ahorn.PlacementDict(
   "Boost Block (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
      MomentumBlock,
      "rectangle"
   )
)

Ahorn.minimumSize(entity::MomentumBlock) = 8, 8
Ahorn.resizable(entity::MomentumBlock) = true, true

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::MomentumBlock, room::Maple.Room)
    startColor = String(get(entity.data, "startColor","9A0000"))
    endColor = String(get(entity.data, "endColor", "00FFFF"))

    startColorC = Ahorn.argb32ToRGBATuple(parse(Int, replace(startColor, "#" => ""), base=16))[1:3] ./ 255
    endColorC = Ahorn.argb32ToRGBATuple(parse(Int, replace(endColor, "#" => ""), base=16))[1:3] ./ 255
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    spd = Int(get(entity.data, "speed", 0))
    g = 1 - abs((1.0 - spd / 282) % 2.0 - 1)
    g= -g + 1;

    Ahorn.drawRectangle(ctx, 0, 0, width, height, Ahorn.defaultBlackColor, (startColorC[1] + (endColorC[1] - startColorC[1]) * g, startColorC[2] + (endColorC[2] - startColorC[2]) * g, startColorC[3] + (endColorC[3] - startColorC[3]) * g)) #Ahorn.defaultWhiteColor)
end

function Ahorn.selection(entity::MomentumBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height)]
end

end