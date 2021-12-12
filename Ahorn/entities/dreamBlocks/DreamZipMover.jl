module CommunalHelperDreamZipMover

using ..Ahorn, Maple
using Ahorn.CommunalHelper
using Ahorn.CommunalHelperEntityPresets: CustomDreamBlockData

const entityData = appendkwargs(CustomDreamBlockData, :(
    dreamAesthetic::Bool=false,
    nodes::Array{Tuple{Integer,Integer},1}=Tuple{Integer,Integer}[],
    permanent::Bool=false,
    waiting::Bool=false,
    ticking::Bool=false,
    noReturn::Bool=false,
))
@mapdefdata Entity "CommunalHelper/DreamZipMover" DreamZipMover entityData

const placements = Ahorn.PlacementDict(
    "Dream Zip Mover (Communal Helper)" => Ahorn.EntityPlacement(
        DreamZipMover,
        "rectangle",
        Dict{String,Any}(),
        function (entity)
            entity.data["nodes"] = [(
                Int(entity.data["x"]) + Int(entity.data["width"]) + 8,
                Int(entity.data["y"]),
            )]
        end,
    ),
)

Ahorn.nodeLimits(entity::DreamZipMover) = 1, -1

Ahorn.minimumSize(entity::DreamZipMover) = 16, 16
Ahorn.resizable(entity::DreamZipMover) = true, true

function Ahorn.selection(entity::DreamZipMover)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    res = [Ahorn.Rectangle(x, y, width, height)]

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)

        push!(res, Ahorn.Rectangle(nx + floor(Int, width / 2) - 5, ny + floor(Int, height / 2) - 5, 10, 10))
    end

    return res
end

const crossSprite = "objects/CommunalHelper/dreamMoveBlock/x"
const ropeColor = (102, 57, 49) ./ 255

function renderDreamZipMover(ctx::Ahorn.Cairo.CairoContext, entity::DreamZipMover)
    x, y = Ahorn.position(entity)
    px, py = x, y

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    dreamAesthetic = Bool(get(entity.data, "dreamAesthetic", false))

    cog = dreamAesthetic ? "objects/CommunalHelper/dreamZipMover/cog" : "objects/zipmover/cog"

    # Dream block stuff
    renderDreamBlock(ctx, x, y, width, height, entity.data)

    # Iteration through all the nodes
    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)
        cx, cy = px + width / 2, py + height / 2
        cnx, cny = nx + width / 2, ny + height / 2

        length = sqrt((px - nx)^2 + (py - ny)^2)
        theta = atan(cny - cy, cnx - cx)

        Ahorn.Cairo.save(ctx)

        Ahorn.translate(ctx, cx, cy)
        Ahorn.rotate(ctx, theta)

        Ahorn.setSourceColor(ctx, dreamAesthetic ? (0.9, 0.9, 0.9, 1.0) : ropeColor)
        Ahorn.set_antialias(ctx, 1)
        Ahorn.set_line_width(ctx, 1)

        # Offset for rounding errors
        Ahorn.move_to(ctx, 0, 4 + (theta <= 0))
        Ahorn.line_to(ctx, length, 4 + (theta <= 0))

        Ahorn.move_to(ctx, 0, -4 - (theta > 0))
        Ahorn.line_to(ctx, length, -4 - (theta > 0))

        Ahorn.stroke(ctx)

        Ahorn.Cairo.restore(ctx)

        Ahorn.drawSprite(ctx, cog, cnx, cny)

        px, py = nx, ny
    end

    if Bool(get(entity.data, "noReturn", false))
        noReturnSprite = Ahorn.getSprite(crossSprite, "Gameplay")
        Ahorn.drawImage(ctx, noReturnSprite, x + div(width - noReturnSprite.width, 2), y + div(height - noReturnSprite.height, 2))
    end
end

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamZipMover) =
    renderDreamZipMover(ctx, entity)

end
