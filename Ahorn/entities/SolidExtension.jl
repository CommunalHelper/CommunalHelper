module CommunalHelperSolidExtension
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/SolidExtension" SolidExtension(
			x::Integer, y::Integer,
			width::Integer=16, height::Integer=16)

const placements = Ahorn.PlacementDict(
    "Solid Extension (Communal Helper)" => Ahorn.EntityPlacement(
        SolidExtension,
		"rectangle"
    )
)

Ahorn.minimumSize(entity::SolidExtension) = 16, 16
Ahorn.resizable(entity::SolidExtension) = true, true

function Ahorn.selection(entity::SolidExtension)
    x, y = Ahorn.position(entity)
	
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
	
	return Ahorn.Rectangle(x, y, width, height)
end

const block = "objects/CommunalHelper/connectedZipMover/extension_outline"

const connectTo = [
                  "CommunalHelper/SolidExtension", 
                  "CommunalHelper/ConnectedZipMover",
                  "CommunalHelper/ConnectedSwapBlock",
                  "CommunalHelper/ConnectedMoveBlock"
                  ]

# Gets rectangles from Zip Mover Extensions & Zip Movers
function getZipMoverRectangles(room::Maple.Room)
    entities = filter(e -> e.name in connectTo, room.entities)
    rects = []

    for e in entities
        push!(rects, Ahorn.Rectangle(
            Int(get(e.data, "x", 0)),
            Int(get(e.data, "y", 0)),
            Int(get(e.data, "width", 8)),
            Int(get(e.data, "height", 8))
        ))
    end
        
    return rects
end

# Checks for collision with an array of rectangles at specified tile position
function notAdjacent(entity::SolidExtension, ox, oy, rects)
    x, y = Ahorn.position(entity)
    rect = Ahorn.Rectangle(x + ox + 4, y + oy + 4, 1, 1)

    for r in rects
        if Ahorn.checkCollision(r, rect)
            return false
        end
    end

    return true
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::SolidExtension, room::Maple.Room)
    rects = getZipMoverRectangles(room)

    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    tileWidth = ceil(Int, width / 8)
    tileHeight = ceil(Int, height / 8)

    rect = Ahorn.Rectangle(x, y, width, height)
	if !(rect in rects)
        push!(rects, rect)
    end

    for i in 1:tileWidth, j in 1:tileHeight
        drawX, drawY = (i - 1) * 8, (j - 1) * 8

        closedLeft = !notAdjacent(entity, drawX - 8, drawY, rects)
        closedRight = !notAdjacent(entity, drawX + 8, drawY, rects)
        closedUp = !notAdjacent(entity, drawX, drawY - 8, rects)
        closedDown = !notAdjacent(entity, drawX, drawY + 8, rects)
        completelyClosed = closedLeft && closedRight && closedUp && closedDown
        
        if completelyClosed
            if notAdjacent(entity, drawX + 8, drawY + 8, rects)
                # down right
                Ahorn.drawImage(ctx, block, x + drawX + 7, y + drawY + 7, 0, 0, 8, 8)

            elseif notAdjacent(entity, drawX - 8, drawY + 8, rects)
                # down left
                Ahorn.drawImage(ctx, block, x + drawX - 7, y + drawY + 7, 16, 0, 8, 8)

            elseif notAdjacent(entity, drawX + 8, drawY - 8, rects)
                # up right
                Ahorn.drawImage(ctx, block, x + drawX + 7, y + drawY - 7, 0, 16, 8, 8)

            elseif notAdjacent(entity, drawX - 8, drawY - 8, rects)
                # up left
                Ahorn.drawImage(ctx, block, x + drawX - 7, y + drawY - 7, 16, 16, 8, 8)
                
            end

		else 
            if closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 8, 0, 8, 8)

            elseif closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 8, 16, 8, 8)

            elseif closedLeft && !closedRight && closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 16, 8, 8, 8)

            elseif !closedLeft && closedRight && closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 0, 8, 8, 8)

            elseif closedLeft && !closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 16, 0, 8, 8)

            elseif !closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 0, 0, 8, 8)

            elseif !closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 0, 16, 8, 8)

            elseif closedLeft && !closedRight && closedUp && !closedDown
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 16, 16, 8, 8)
            end
        end

    end
end

end
