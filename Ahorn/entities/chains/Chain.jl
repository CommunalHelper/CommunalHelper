module CommunalHelperChain

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/Chain" Chain(
    x::Integer,
    y::Integer,
    extraJoints::Integer=0,
    outline::Bool=true,
    texture::String="objects/CommunalHelper/chains/chain",
)

const placements = Ahorn.PlacementDict(
    "Chain (Communal Helper)" => Ahorn.EntityPlacement(
        Chain,
        "line",
    ),
)

Ahorn.nodeLimits(entity::Chain) = 1, 1

function Ahorn.selection(entity::Chain)
    nodes = get(entity.data, "nodes", ())
    x, y = Ahorn.position(entity)

    res = Ahorn.Rectangle[Ahorn.Rectangle(x - 4, y - 4, 8, 8)]
    
    for node in nodes
        nx, ny = node

        push!(res, Ahorn.Rectangle(nx - 4, ny - 4, 8, 8))
    end

    return res
end

function renderChain(ctx::Ahorn.Cairo.CairoContext, entity::Chain, color::Ahorn.colorTupleType=ChainColor)
    x, y = Ahorn.position(entity)

    start = (x, y)
    stop = get(entity.data, "nodes", [start])[1]
    nx, ny = Int.(stop)
    control = (start .+ stop) ./ 2 .+ (0, 24)

    curve = Ahorn.SimpleCurve(start, stop, control)
    Ahorn.drawSimpleCurve(ctx, curve, color, thickness=1)
end

Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::Chain) = renderChain(ctx, entity, Ahorn.colors.selection_selected_fc)

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::Chain, room::Room) = renderChain(ctx, entity, (0.0, 0.5, 0.5, 1.0))

end