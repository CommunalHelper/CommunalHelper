module CommunalHelperMelvin
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/Melvin" Melvin(
			x::Integer, y::Integer,
            width::Integer=24, height::Integer=24,
            axes::String = "none")
           
const placements = Ahorn.PlacementDict(
    "Melvin ($(uppercasefirst(axes))) (Communal Helper)" => Ahorn.EntityPlacement(
        Melvin,
        "rectangle",
        Dict{String, Any}(
            "axes" => axes
        )
    ) for axes in Maple.kevin_axes
)

Ahorn.minimumSize(entity::Melvin) = 24, 24
Ahorn.resizable(entity::Melvin) = true, true

Ahorn.selection(entity::Melvin) = Ahorn.getEntityRectangle(entity)

Ahorn.editingOptions(entity::Melvin) = Dict{String, Any}(
    "axes" => Maple.kevin_axes
)

const edges = Dict{String, String}(
    "none" => "objects/CommunalHelper/melvin/surf_A",
    "horizontal" => "objects/CommunalHelper/melvin/surf_H",
    "vertical" => "objects/CommunalHelper/melvin/surf_V",
    "both" => "objects/CommunalHelper/melvin/surf_A"
)
const insideTiles = "objects/CommunalHelper/melvin/inside"
const eye = "objects/CommunalHelper/melvin/eye/idle_small00"

const kevinColor = (98, 34, 43) ./ 255

function getBlockTextures(axes::String)
    return insideTiles, edges[axes]
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Melvin, room::Maple.Room)
    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))
    
    inside, block = getBlockTextures(lowercase(get(entity.data, "axes", "none")))
    eyeSprite = Ahorn.getSprite(eye, "Gameplay")

    tileRands = Ahorn.getDrawableRoom(Ahorn.loadedState.map, room).fgTileStates.rands

    x, y = Ahorn.position(entity)
    rtx = floor(Int, x / 8)
    rty = floor(Int, y / 8)

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

    Ahorn.drawRectangle(ctx, 2, 2, width - 4, height - 4, kevinColor)

    for i in 1:tileWidth, j in 1:tileHeight
        tx = (i == 1) ? 0 : ((i == tileWidth) ? 24 : 8)
        ty = (j == 1) ? 0 : ((j == tileHeight) ? 24 : 8)
        isEdge = (i == 1 || i == tileWidth || j == 1 || j == tileHeight)
        tileChoice = mod(get(tileRands, (rty + j, rtx + i), 0), 4)
        
        if tx == 8
            if tileChoice == 1 || tileChoice == 3
                tx += 8
            end
        end
        if ty == 8
            if tileChoice == 2 || tileChoice == 3
                ty += 8
            end
        end
        if !isEdge
            tx -= 8
            ty -= 8
        end
        Ahorn.drawImage(ctx, isEdge ? block : inside, (i - 1) * 8, (j - 1) * 8, tx, ty, 8, 8)
    end
    Ahorn.drawImage(ctx, eyeSprite, div(width - eyeSprite.width, 2), div(height - eyeSprite.height, 2))
end

end