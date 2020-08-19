module CommunalHelperMelvin
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/Melvin" Melvin(
			x::Integer, y::Integer,
            width::Integer=24, height::Integer=24)
           
const placements = Ahorn.PlacementDict(
    "Melvin (Communal Helper)" => Ahorn.EntityPlacement(
        Melvin,
        "rectangle"
    )
)

Ahorn.minimumSize(entity::Melvin) = 24, 24
Ahorn.resizable(entity::Melvin) = true, true

Ahorn.selection(entity::Melvin) = Ahorn.getEntityRectangle(entity)

const block = "objects/CommunalHelper/melvin/block"

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Melvin, room::Maple.Room)
    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))
    
    tileRands = Ahorn.getDrawableRoom(Ahorn.loadedState.map, room).fgTileStates.rands

    x, y = Ahorn.position(entity)
    rtx = floor(Int, x / 8)
    rty = floor(Int, y / 8)

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

    for i in 1:tileWidth, j in 1:tileHeight
        tx = (i == 1) ? 0 : ((i == tileWidth) ? 24 : 8)
        ty = (j == 1) ? 0 : ((j == tileHeight) ? 24 : 8)
        
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
        

        Ahorn.drawImage(ctx, block, (i - 1) * 8, (j - 1) * 8, tx, ty, 8, 8)
    end
end

end