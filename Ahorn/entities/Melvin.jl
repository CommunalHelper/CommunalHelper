module CommunalHelperMelvin

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/Melvin" Melvin(
    x::Integer,
    y::Integer,
    width::Integer=24,
    height::Integer=24,
    weakTop::Bool=false,
    weakRight::Bool=false,
    weakBottom::Bool=false,
    weakLeft::Bool=false,
)

const placements = Ahorn.PlacementDict(
    "Melvin (All Strong) (Communal Helper)" => Ahorn.EntityPlacement(
        Melvin,
        "rectangle",
    ),
    "Melvin (All Weak) (Communal Helper)" => Ahorn.EntityPlacement(
        Melvin,
        "rectangle",
        Dict{String,Any}(
            "weakTop" => true,
            "weakRight" => true,
            "weakBottom" => true,
            "weakLeft" => true,
        ),
    ),
    "Melvin (Horizontally Weak) (Communal Helper)" => Ahorn.EntityPlacement(
        Melvin,
        "rectangle",
        Dict{String,Any}(
            "weakRight" => true,
            "weakLeft" => true,
        ),
    ),
    "Melvin (Vertically Weak) (Communal Helper)" => Ahorn.EntityPlacement(
        Melvin,
        "rectangle",
        Dict{String,Any}(
            "weakTop" => true,
            "weakBottom" => true,
        ),
    ),
)

Ahorn.minimumSize(entity::Melvin) = 24, 24
Ahorn.resizable(entity::Melvin) = true, true

Ahorn.selection(entity::Melvin) = Ahorn.getEntityRectangle(entity)

const strongTiles = "objects/CommunalHelper/melvin/block_strong"
const weakTiles = "objects/CommunalHelper/melvin/block_weak"
const weakCornersHTiles = "objects/CommunalHelper/melvin/corners_weak_h"
const weakCornersVTiles = "objects/CommunalHelper/melvin/corners_weak_v"

const insideTiles = "objects/CommunalHelper/melvin/inside"
const eye = "objects/CommunalHelper/melvin/eye/idle_small00"

const kevinColor = (98, 34, 43) ./ 255

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Melvin, room::Maple.Room)
    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))

    eyeSprite = Ahorn.getSprite(eye, "Gameplay")

    tileRands = Ahorn.getDrawableRoom(Ahorn.loadedState.map, room).fgTileStates.rands

    weakTop = Bool(get(entity.data, "weakTop", false))
    weakBottom = Bool(get(entity.data, "weakBottom", false))
    weakRight = Bool(get(entity.data, "weakRight", false))
    weakLeft = Bool(get(entity.data, "weakLeft", false))

    x, y = Ahorn.position(entity)
    rtx = floor(Int, x / 8)
    rty = floor(Int, y / 8)

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

    Ahorn.drawRectangle(ctx, 2, 2, width - 4, height - 4, kevinColor)

    # edges and interior
    for i in 1:tileWidth, j in 1:tileHeight
        tx = (i == 1) ? 0 : ((i == tileWidth) ? 24 : 8)
        ty = (j == 1) ? 0 : ((j == tileHeight) ? 24 : 8)
        isEdge = (i == 1 || i == tileWidth || j == 1 || j == tileHeight)
        isCorner = ((i == 1 || i == tileWidth) && (j == 1 || j == tileHeight))
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
        if !isEdge
            Ahorn.drawImage(ctx, insideTiles, (i - 1) * 8, (j - 1) * 8, tx, ty, 8, 8)
        elseif !isCorner
            block = strongTiles
            if i == 1
                block = weakLeft ? weakTiles : strongTiles
            elseif i == tileWidth
                block = weakRight ? weakTiles : strongTiles
            elseif j == 1
                block = weakTop ? weakTiles : strongTiles
            elseif j == tileHeight
                block = weakBottom ? weakTiles : strongTiles
            end
            Ahorn.drawImage(ctx, block, (i - 1) * 8, (j - 1) * 8, tx, ty, 8, 8)
        end
    end

    topleft = strongTiles
    topright = strongTiles
    bottomleft = strongTiles
    bottomright = strongTiles

    # topleft corner
    if weakLeft && weakTop
        topleft = weakTiles
    elseif weakLeft && !weakTop
        topleft = weakCornersHTiles
    elseif !weakLeft && weakTop
        topleft = weakCornersVTiles
    end
    # topright corner
    if weakRight && weakTop
        topright = weakTiles
    elseif weakRight && !weakTop
        topright = weakCornersHTiles
    elseif !weakRight && weakTop
        topright = weakCornersVTiles
    end
    # bottomleft corner
    if weakLeft && weakBottom
        bottomleft = weakTiles
    elseif weakLeft && !weakBottom
        bottomleft = weakCornersHTiles
    elseif !weakLeft && weakBottom
        bottomleft = weakCornersVTiles
    end
    # bottomright corner
    if weakRight && weakBottom
        bottomright = weakTiles
    elseif weakRight && !weakBottom
        bottomright = weakCornersHTiles
    elseif !weakRight && weakBottom
        bottomright = weakCornersVTiles
    end

    Ahorn.drawImage(ctx, topleft, 0, 0, 0, 0, 8, 8)
    Ahorn.drawImage(ctx, topright, width - 8, 0, 24, 0, 8, 8)
    Ahorn.drawImage(ctx, bottomleft, 0, height - 8, 0, 24, 8, 8)
    Ahorn.drawImage(ctx, bottomright, width - 8, height - 8, 24, 24, 8, 8)

    Ahorn.drawImage(ctx, eyeSprite, div(width - eyeSprite.width, 2), div(height - eyeSprite.height, 2))
end

end
