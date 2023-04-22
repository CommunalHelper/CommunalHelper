module CommunalHelperUFO

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/UFO" UFO(x::Integer, y::Integer, nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[], raySizeX::Integer = 13, raySizeY::Integer = 60)

#== hidden for now

const placements = Ahorn.PlacementDict(
    "UFO (Communal Helper)" => Ahorn.EntityPlacement(
        UFO
    )				
)

==#

UFOsprite = "objects/CommunalHelper/ufo/idle00.png"

function Ahorn.selection(entity::UFO)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    res = [Ahorn.Rectangle(x - 13, y - 21, 26, 26)]
	
    for node in nodes
        nx, ny = node
        push!(res, Ahorn.getSpriteRectangle(UFOsprite, nx, ny))
    end
	
    return res
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::UFO, room::Maple.Room)
    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = node
        Ahorn.drawSprite(ctx, UFOsprite, nx, ny)
    end
    x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, UFOsprite, x, y)
end

Ahorn.nodeLimits(entity::UFO) = 1, -1

end