module CommunalHelperChainedKevin

using ..Ahorn, Maple
using Ahorn.Cairo

@mapdef Entity "CommunalHelper/ChainedKevin" ChainedKevin(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    height::Integer=Maple.defaultBlockHeight,
    chillout::Bool=false,
    chainLength::Integer=64,
    direction::String="Right",
    chainOutline::Bool=true,
    centeredChain::Bool=false,
    chainTexture::String="objects/CommunalHelper/chains/chain",
)

const placements = Ahorn.PlacementDict(
    "Kevin (Chained, $direction) (Communal Helper)" => Ahorn.EntityPlacement(
        ChainedKevin,
        "rectangle",
        Dict{String, Any}(
            "direction" => direction,
        ),
    ) for direction in Maple.move_block_directions
)

const smallFace = "objects/crushblock/idle_face"
const giantFace = "objects/crushblock/giant_block00"

const ChainedKevinColor = (98, 34, 43) ./ 255

Ahorn.editingOptions(entity::ChainedKevin) = Dict{String, Any}(
    "direction" => Maple.move_block_directions,
)

Ahorn.minimumSize(entity::ChainedKevin) = 24, 24
Ahorn.resizable(entity::ChainedKevin) = true, true

Ahorn.selection(entity::ChainedKevin) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ChainedKevin, room::Maple.Room)
    direction = String(get(entity.data, "direction", "Right"))
    chillout = get(entity.data, "chillout", false)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    face = height >= 48 && width >= 48 && chillout ? giantFace : smallFace
    frame = "objects/CommunalHelper/chainedKevin/block" * direction
    faceSprite = Ahorn.getSprite(face, "Gameplay")

    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    chainLength = Int(get(entity.data, "chainLength", 64))
    fillColor = (0.0, 0.0, 0.0, 0.0)
    outlineColor = (1.0, 1.0, 1.0, 0.5)

    save(ctx)

    set_dash(ctx, [0.6, 0.2])
    set_antialias(ctx, 1)
    set_line_width(ctx, 1)

    if direction == "Up"
        Ahorn.drawRectangle(ctx, 1, -chainLength + 1, width - 1, height + chainLength - 1, fillColor, outlineColor)
    elseif direction == "Right"
        Ahorn.drawRectangle(ctx, 1, 1, width + chainLength - 1, height - 1, fillColor, outlineColor)
    elseif direction == "Left"
        Ahorn.drawRectangle(ctx, -chainLength + 1, 1, width + chainLength - 1, height - 1, fillColor, outlineColor)
    else
        Ahorn.drawRectangle(ctx, 1, 1, width - 1, height + chainLength - 1, fillColor, outlineColor)
    end

    restore(ctx)

    Ahorn.drawRectangle(ctx, 2, 2, width - 4, height - 4, ChainedKevinColor)
    Ahorn.drawImage(ctx, faceSprite, div(width - faceSprite.width, 2), div(height - faceSprite.height, 2))

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, 0, 8, 0, 8, 8)
        Ahorn.drawImage(ctx, frame, (i - 1) * 8, height - 8, 8, 24, 8, 8)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, frame, 0, (i - 1) * 8, 0, 8, 8, 8)
        Ahorn.drawImage(ctx, frame, width - 8, (i - 1) * 8, 24, 8, 8, 8)
    end

    Ahorn.drawImage(ctx, frame, 0, 0, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, 0, 24, 0, 8, 8)
    Ahorn.drawImage(ctx, frame, 0, height - 8, 0, 24, 8, 8)
    Ahorn.drawImage(ctx, frame, width - 8, height - 8, 24, 24, 8, 8)
end

end