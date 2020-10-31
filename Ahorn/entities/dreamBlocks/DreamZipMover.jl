module CommunalHelperDreamZipMover

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/DreamZipMover" DreamZipMover(x::Integer, y::Integer,
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
	noReturn::Bool=false, dreamAesthetic::Bool=false, featherMode::Bool=false, oneUse::Bool=false, refillCount::Integer=-1, below::Bool=false)

const placements = Ahorn.PlacementDict(
    "Dream Zip Mover (Communal Helper)" => Ahorn.EntityPlacement(
        DreamZipMover,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
        end
    )
)

Ahorn.nodeLimits(entity::DreamZipMover) = 1, 1

Ahorn.minimumSize(entity::DreamZipMover) = 16, 16
Ahorn.resizable(entity::DreamZipMover) = true, true

function Ahorn.selection(entity::DreamZipMover)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(nx + floor(Int, width / 2) - 5, ny + floor(Int, height / 2) - 5, 10, 10)]
end

function getTextures(entity::DreamZipMover)
    dreamAesthetic = Bool(get(entity.data, "dreamAesthetic", false))
    return "objects/zipmover/block", "objects/zipmover/light01", (dreamAesthetic ? "objects/CommunalHelper/dreamZipMover/cog" : "objects/zipmover/cog")
end

const crossSprite = "objects/CommunalHelper/dreamMoveBlock/x"
const ropeColor = (102, 57, 49) ./ 255

function renderDreamZipMover(ctx::Ahorn.Cairo.CairoContext, entity::DreamZipMover)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    dreamAesthetic = Bool(get(entity.data, "dreamAesthetic", false))

    block, light, cog = getTextures(entity)

    cx, cy = x + width / 2, y + height / 2
    cnx, cny = nx + width / 2, ny + height / 2

    length = sqrt((x - nx)^2 + (y - ny)^2)
    theta = atan(cny - cy, cnx - cx)

    # Dream block stuff
    renderDreamBlock(ctx, x, y, width, height, entity.data)
    
    Ahorn.Cairo.save(ctx)

    Ahorn.translate(ctx, cx, cy)
    Ahorn.rotate(ctx, theta)

    Ahorn.setSourceColor(ctx, dreamAesthetic ? (1.0, 1.0, 1.0, 1.0) : ropeColor)

    # Offset for rounding errors
    Ahorn.move_to(ctx, 0, 4 + (theta <= 0))
    Ahorn.line_to(ctx, length, 4 + (theta <= 0))

    Ahorn.move_to(ctx, 0, -4 - (theta > 0))
    Ahorn.line_to(ctx, length, -4 - (theta > 0))

    Ahorn.stroke(ctx)

    Ahorn.Cairo.restore(ctx)

    Ahorn.drawSprite(ctx, cog, cnx, cny)

    if Bool(get(entity.data, "noReturn", false))
        noReturnSprite = Ahorn.getSprite(crossSprite, "Gameplay")
        Ahorn.drawImage(ctx, noReturnSprite, x + div(width - noReturnSprite.width, 2), y + div(height - noReturnSprite.height, 2))
    end
end

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamZipMover) =
    renderDreamZipMover(ctx, entity)

end
