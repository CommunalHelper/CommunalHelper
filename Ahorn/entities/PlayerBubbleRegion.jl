module CommunalHelperPlayerBubbleRegion

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/PlayerBubbleRegion" PlayerBubbleRegion(x::Integer, y::Integer, 
    width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])

const placements = Ahorn.PlacementDict(
    "Player Bubble Region (Communal Helper)" => Ahorn.EntityPlacement(
        PlayerBubbleRegion,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            cx = Int(entity.data["x"]) + Int(entity.data["width"]) / 2
            cy = Int(entity.data["y"]) + Int(entity.data["height"]) / 2
            entity.data["nodes"] = [
                (cx + 32, cy),
                (cx + 64, cy)
            ]
        end
    )
)

Ahorn.nodeLimits(entity::PlayerBubbleRegion) = 2, 2
Ahorn.resizable(entity::PlayerBubbleRegion) = true, true
Ahorn.minimumSize(entity::PlayerBubbleRegion) = 8, 8

const sprite = "characters/player/bubble"
const color = (45, 103, 111, 200) ./ 255

function Ahorn.selection(entity::PlayerBubbleRegion)
    x, y = Ahorn.position(entity)
    nodes = entity.data["nodes"]
    controllX, controllY = Int.(nodes[1])
    endX, endY = Int.(nodes[2])

    return [
        Ahorn.getEntityRectangle(entity),
        Ahorn.getSpriteRectangle(sprite, controllX, controllY),
        Ahorn.getSpriteRectangle(sprite, endX, endY)
    ]
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PlayerBubbleRegion, room::Maple.Room) 
    width = Int(entity.data["width"])
    height = Int(entity.data["height"])

    Ahorn.drawRectangle(ctx, 0, 0, width, height, color)

    Ahorn.drawSprite(ctx, sprite, width / 2, height / 2)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::PlayerBubbleRegion)
    px, py = Ahorn.position(entity)
    nodes = entity.data["nodes"]
    
    width = Int(entity.data["width"])
    height = Int(entity.data["height"])
    px += width / 2
    py += height / 2

    cx, cy = Int.(nodes[1])
    ex, ey = Int.(nodes[1])
    
    Ahorn.drawArrow(ctx, px, py, cx, cy, Ahorn.colors.selection_selected_fc, headLength=6)
    Ahorn.drawArrow(ctx, cx, cy, ex, ey, Ahorn.colors.selection_selected_fc, headLength=6)
    Ahorn.drawSprite(ctx, sprite, ex, ey)
end

end