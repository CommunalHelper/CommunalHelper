module CommunalHelperSJLoopBlock

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SJ/LoopBlock" LoopBlock(x::Integer, y::Integer, width::Integer=16, height::Integer=16, edgeThickness::Integer=1, color::String="FFFFFF")

const placements = Ahorn.PlacementDict(
    "Loop Block (Strawberry Jam) (Communal Helper)" => Ahorn.EntityPlacement(
        LoopBlock,
        "rectangle"
    )
)

Ahorn.minimumSize(entity::LoopBlock) = 24, 24
Ahorn.resizable(entity::LoopBlock) = true, true

function Ahorn.selection(entity::LoopBlock)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))

    return Ahorn.Rectangle(x, y, width, height)
end

const tileset = "objects/CommunalHelper/strawberryJam/loopBlock/tiles"

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LoopBlock, room::Maple.Room)
    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)
    
    tileRands = Ahorn.getDrawableRoom(Ahorn.loadedState.map, room).fgTileStates.rands

    minSize = min(width, height) / 8
    edgeThickness = clamp(Int(get(entity.data, "edgeThickness", 1)), 1, floor((minSize - 1) / 2))

    # borrowed from Communal Helper
    color = tuple(Ahorn.argb32ToRGBATuple(parse(Int, String(get(entity.data, "color", "FFFFFF")), base=16))[1:3] ./ 255..., 1.0)

    tiles = Array{Bool}(undef, tileWidth, tileHeight)
    for i in 1:tileWidth, j in 1:tileHeight
        tiles[i, j] = (i <= edgeThickness || i > tileWidth - edgeThickness || j <= edgeThickness || j > tileHeight - edgeThickness)
    end

    # not a super elegant way of doing it
    for i in 1:tileWidth, j in 1:tileHeight
        if tiles[i, j] 
            up = get(tiles, (i, j - 1), false)
            down = get(tiles, (i, j + 1), false)
            left = get(tiles, (i - 1, j), false)
            right = get(tiles, (i + 1, j), false)
            upleft = get(tiles, (i - 1, j - 1), false)
            upright = get(tiles, (i + 1, j - 1), false)
            downleft = get(tiles, (i - 1, j + 1), false)
            downright = get(tiles, (i + 1, j + 1), false)

            tileChoice = mod(get(tileRands, (j, i), 0), 3)

            innerCorner = up && down && left && right
            full = innerCorner && upleft && upright && downleft && downright

            tx = 0
            ty = 0

            if full
                # used to be confusing, we are mapping [0, 1, 2, 3] to [0, 1, 0, 1] for tx, and [0, 0, 1, 1] for ty.
                # thanks vexatos
                r = mod(get(tileRands, (j, i), 0), 4)
                tx = mod(r, 2) + 1
                ty = div(r, 2) + 7
                tileChoice = 0
            elseif innerCorner
                tileChoice = 0
                if !downright
                    ty = 7
                elseif !downleft
                    tx = 3
                    ty = 7
                elseif !upright
                    ty = 8
                else 
                    tx = 3
                    ty = 8
                end
            else
                if !up && left && down && right
                    ty = 2
                elseif up && !left && down && right
                    tx = 3
                    ty = 2
                elseif up && left && !down && right
                    ty = 3
                elseif up && left && down && !right
                    tx = 3
                    ty = 3
                elseif !up && !left && down && right
                    ty = downright ? 0 : 4
                elseif !up && left && down && !right
                    tx = 3
                    ty = downleft ? 0 : 4
                elseif up && !left && !down && right
                    ty = upright ? 1 : 5
                elseif up && left && !down && !right
                    tx = 3
                    ty = upleft ? 1 : 5
                elseif up && down && !left && !right
                    tx = 3
                    ty = 6
                elseif !up && !down && left && right
                    ty = 6
                end
            end

            tx += tileChoice
            Ahorn.drawImage(ctx, tileset, (i - 1) * 8, (j - 1) * 8, tx * 8, ty * 8, 8, 8, tint=color)
        end
    end
end

end