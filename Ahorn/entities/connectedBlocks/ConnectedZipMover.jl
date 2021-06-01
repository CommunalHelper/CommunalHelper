module CommunalHelperConnectedZipMover

using ..Ahorn, Maple
using Ahorn.CommunalHelper

const theme_options = String["Normal", "Moon", "Cliffside"]

@mapdef Entity "CommunalHelper/ConnectedZipMover" ConnectedZipMover(
    x::Integer,
    y::Integer,
    width::Integer=16,
    height::Integer=16,
    theme::String="Normal",
    permanent::Bool=false,
    waiting::Bool=false,
    ticking::Bool=false,
    nodes::Array{Tuple{Integer,Integer},1}=Tuple{Integer,Integer}[],
    customBlockTexture::String="",
)

const placements = Ahorn.PlacementDict(
    "Connected Zip Mover ($(uppercasefirst(theme))) (Communal Helper)" => Ahorn.EntityPlacement(
        ConnectedZipMover,
        "rectangle",
        Dict{String,Any}(
            "theme" => theme,
        ),
        function (entity)
            entity.data["nodes"] = [(
                Int(entity.data["x"]) + Int(entity.data["width"]) + 8,
                Int(entity.data["y"]),
            )]
        end,
    ) for theme in theme_options
)

placements["Connected Zip Mover (Reskinnable) (Communal Helper)"] = Ahorn.EntityPlacement(
    ConnectedZipMover,
    "rectangle",
    Dict{String,Any}(
        "customBlockTexture" => "CommunalHelper/customConnectedBlock/customConnectedBlock",
    ),
)

Ahorn.editingOptions(entity::ConnectedZipMover) = Dict{String,Any}(
    "theme" => theme_options,
)

Ahorn.nodeLimits(entity::ConnectedZipMover) = 1, -1

Ahorn.minimumSize(entity::ConnectedZipMover) = 16, 16
Ahorn.resizable(entity::ConnectedZipMover) = true, true

function Ahorn.selection(entity::ConnectedZipMover)
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

function getTextures(entity::ConnectedZipMover)
    theme = lowercase(get(entity, "theme", "normal"))
    isCliffside = theme == "cliffside"
    folder = isCliffside ? "CommunalHelper/connectedZipMover" : "zipmover"
    themePath = (theme == "normal") ? "" : string(theme, "/")

    return (
        "objects/$(folder)/$(themePath)block",
        "objects/$(folder)/$(themePath)light01",
        "objects/$(folder)/$(themePath)cog",
        "objects/$((isCliffside ? "" : "CommunalHelper/") * folder)/$(themePath)innerCorners"
    )
end

const ropeColor = (102, 57, 49) ./ 255

function renderZipMover(ctx::Ahorn.Cairo.CairoContext, entity::ConnectedZipMover, room::Maple.Room)
    rects = getExtensionRectangles(room)

    x, y = Ahorn.position(entity)
    px, py = x, y

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    block, light, cog, incorners = getTextures(entity)
    lightSprite = Ahorn.getSprite(light, "Gameplay")

    customBlockTexture = String(get(entity.data, "customBlockTexture", ""))
    hasCustomTexture = customBlockTexture != ""
    txOffset = 0

    if hasCustomTexture 
        block = incorners = "objects/" * customBlockTexture
        txOffset = 24
    end

    tileWidth = div(width, 8)
    tileHeight = div(height, 8)

    rect = Ahorn.Rectangle(x, y, width, height)

    if !(rect in rects)
        push!(rects, rect)
    end

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

        Ahorn.setSourceColor(ctx, ropeColor)
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

    Ahorn.drawRectangle(ctx, x + 2, y + 2, width - 4, height - 4, (0.0, 0.0, 0.0, 1.0))

    for i in 1:tileWidth, j in 1:tileHeight
        drawX, drawY = (i - 1) * 8, (j - 1) * 8

        closedLeft = !notAdjacent(entity, drawX - 8, drawY, rects)
        closedRight = !notAdjacent(entity, drawX + 8, drawY, rects)
        closedUp = !notAdjacent(entity, drawX, drawY - 8, rects)
        closedDown = !notAdjacent(entity, drawX, drawY + 8, rects)
        completelyClosed = closedLeft && closedRight && closedUp && closedDown

        if completelyClosed
            Ahorn.drawRectangle(ctx, x + drawX, y + drawY, 8, 8, (0.0, 0.0, 0.0, 1.0))
            if notAdjacent(entity, drawX + 8, drawY - 8, rects)
                # up right
                Ahorn.drawImage(ctx, incorners, x + drawX, y + drawY, 8 + txOffset, 0, 8, 8)

            elseif notAdjacent(entity, drawX - 8, drawY - 8, rects)
                # up left
                Ahorn.drawImage(ctx, incorners, x + drawX, y + drawY, 0 + txOffset, 0, 8, 8)

            elseif notAdjacent(entity, drawX + 8, drawY + 8, rects)
                # down right
                Ahorn.drawImage(ctx, incorners, x + drawX, y + drawY, 8 + txOffset, 8, 8, 8)

            elseif notAdjacent(entity, drawX - 8, drawY + 8, rects)
                # down left
                Ahorn.drawImage(ctx, incorners, x + drawX, y + drawY, 0 + txOffset, 8, 8, 8)
            end
        else
            if closedLeft && closedRight && !closedUp && closedDown
                Ahorn.drawRectangle(ctx, x + drawX, y + drawY, 8, 8, (0.0, 0.0, 0.0, 1.0))
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 8, 0, 8, 8)

            elseif closedLeft && closedRight && closedUp && !closedDown
                Ahorn.drawRectangle(ctx, x + drawX, y + drawY, 8, 8, (0.0, 0.0, 0.0, 1.0))
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 8, 16, 8, 8)

            elseif closedLeft && !closedRight && closedUp && closedDown
                Ahorn.drawRectangle(ctx, x + drawX, y + drawY, 8, 8, (0.0, 0.0, 0.0, 1.0))
                Ahorn.drawImage(ctx, block, x + drawX, y + drawY, 16, 8, 8, 8)

            elseif !closedLeft && closedRight && closedUp && closedDown
                Ahorn.drawRectangle(ctx, x + drawX, y + drawY, 8, 8, (0.0, 0.0, 0.0, 1.0))
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

    Ahorn.drawImage(ctx, lightSprite, x + floor(Int, (width - lightSprite.width) / 2), y)
end

Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::ConnectedZipMover, room::Maple.Room) = renderZipMover(ctx, entity, room)

end
