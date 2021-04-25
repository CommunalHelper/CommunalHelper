module CommunalHelperCassetteSwapBlock

using ..Ahorn, Maple
using Ahorn.CommunalHelper

function swapFinalizer(entity)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    entity.data["nodes"] = [(x + width, y)]
end

@mapdef Entity "CommunalHelper/CassetteSwapBlock" CassetteSwapBlock(x::Integer, y::Integer, 
    width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    index::Integer=0, tempo::Number=1.0, noReturn::Bool=false,
    customColor="") 

const ropeColors = Dict{Int, Ahorn.colorTupleType}(
    1 => (194, 116, 171, 255) ./ 255,
	2 => (227, 214, 148, 255) ./ 255,
	3 => (128, 224, 141, 255) ./ 255
)

const defaultRopeColor = (110, 189, 245, 255) ./ 255


const placements = Ahorn.PlacementDict(
    "Cassette Swap Block ($index - $color) (Communal Helper)" => Ahorn.EntityPlacement(
        CassetteSwapBlock,
        "rectangle",
        Dict{String, Any}(
            "index" => index,
        ),
        swapFinalizer
    ) for (color, index) in cassetteColorNames
)

Ahorn.editingOptions(entity::CassetteSwapBlock) = Dict{String, Any}(
    "index" => cassetteColorNames
)

Ahorn.nodeLimits(entity::Maple.SwapBlock) = 1, 1

Ahorn.minimumSize(entity::CassetteSwapBlock) = 16, 16
Ahorn.resizable(entity::CassetteSwapBlock) = true, true

function Ahorn.selection(entity::CassetteSwapBlock)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(stopX, stopY, width, height)]
end

const block = "objects/cassetteblock/solid"
const crossSprite = "objects/CommunalHelper/cassetteMoveBlock/x"

function renderTrail(ctx, x::Number, y::Number, width::Number, height::Number, trail::String, index::Int)
    tilesWidth = div(width, 8)
    tilesHeight = div(height, 8)

    ropeColor = get(ropeColors, index, defaultRopeColor)

    for i in 2:tilesWidth - 1
        Ahorn.drawImage(ctx, trail, x + (i - 1) * 8, y + 2, 6, 0, 8, 6, tint=ropeColor)
        Ahorn.drawImage(ctx, trail, x + (i - 1) * 8, y + height - 8, 6, 14, 8, 6, tint=ropeColor)
    end

    for i in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, trail, x + 2, y + (i - 1) * 8, 0, 6, 6, 8, tint=ropeColor)
        Ahorn.drawImage(ctx, trail, x + width - 8, y + (i - 1) * 8, 14, 6, 6, 8, tint=ropeColor)
    end

    for i in 2:tilesWidth - 1, j in 2:tilesHeight - 1
        Ahorn.drawImage(ctx, trail, x + (i - 1) * 8, y + (j - 1) * 8, 6, 6, 8, 8, tint=ropeColor)
    end

    Ahorn.drawImage(ctx, trail, x + width - 8, y + 2, 14, 0, 6, 6, tint=ropeColor)
    Ahorn.drawImage(ctx, trail, x + width - 8, y + height - 8, 14, 14, 6, 6, tint=ropeColor)
    Ahorn.drawImage(ctx, trail, x + 2, y + 2, 0, 0, 6, 6, tint=ropeColor)
    Ahorn.drawImage(ctx, trail, x + 2, y + height - 8, 0, 14, 6, 6, tint=ropeColor)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::CassetteSwapBlock, room::Maple.Room)
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    index = Int(get(entity.data, "index", 0))

    renderCassetteBlock(ctx, stopX, stopY, width, height, index)

    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)
end


function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CassetteSwapBlock, room::Maple.Room)
    startX, startY = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    index = Int(get(entity.data, "index", 0))
    color = getCassetteColor(index)

    renderTrail(ctx, min(startX, stopX), min(startY, stopY), abs(startX - stopX) + width, abs(startY - stopY) + height, "objects/swapblock/target", index)

    renderCassetteBlock(ctx, startX, startY, width, height, index)

    if Bool(get(entity.data, "noReturn", false))
        noReturnSprite = Ahorn.getSprite(crossSprite, "Gameplay")
        Ahorn.drawImage(ctx, noReturnSprite, startX + div(width - noReturnSprite.width, 2), startY + div(height - noReturnSprite.height, 2), tint=color)
    end
end

end