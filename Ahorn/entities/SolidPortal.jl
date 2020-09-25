module CommunalHelperSolidPortal
using ..Ahorn, Maple

const facing_options = ["Up", "Down", "Left", "Right"]

@mapdef Entity "CommunalHelper/SolidPortal" SolidPortal(
                    x::Integer, y::Integer,
                    width::Integer = 24, height::Integer = 24,
                    facingEntrance::String = "Up", facingExit::String = "Up",
                    nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])

const placements = Ahorn.PlacementDict(
    "Solid Portal ($(facing1), $(facing2)) (Communal Helper)" => Ahorn.EntityPlacement(
        SolidPortal,
        "rectangle",
        Dict{String, Any}(
            "facingEntrance" => facing1,
            "facingExit" => facing2
        ),
        function(entity)
            horiz = isHorizontal(String(entity.data["facingEntrance"]))
            entity.data["nodes"] = [(Int(entity.data["x"]) + (horiz ? Int(entity.data["width"]) : 16), Int(entity.data["y"]) + (horiz ? 16 : Int(entity.data["height"])))]
        end

    ) for facing1 in facing_options for facing2 in facing_options
)

Ahorn.minimumSize(entity::SolidPortal) = 16, 16
Ahorn.editingOptions(entity::SolidPortal) = Dict{String, Any}(
    "facingEntrance" => facing_options,
    "facingExit" => facing_options
)
Ahorn.nodeLimits(entity::SolidPortal) = 1, -1
Ahorn.resizable(entity::SolidPortal) = true, true

function isHorizontal(facing::String)
    return facing == "Up" || facing == "Down"
end

function Ahorn.selection(entity::SolidPortal)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    horizStart = isHorizontal(get(entity.data, "facingEntrance", "Up"))
    horizEnd = isHorizontal(get(entity.data, "facingExit", "Up"))

    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))
    widthStart = horizStart ? width : 8
    heightStart = horizStart ? 8 : height
    widthEnd = horizStart == horizEnd ? widthStart : heightStart
    heightEnd = horizStart == horizEnd ? heightStart : widthStart
	
    return [Ahorn.Rectangle(x, y, widthStart, heightStart), Ahorn.Rectangle(nx, ny, widthEnd, heightEnd)]
end

lineColor = (194, 51, 255, 200) ./ 255

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::SolidPortal, room::Maple.Room)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(entity.data["nodes"][1])

    facingEntrance = get(entity.data, "facingEntrance", "Up")
    facingExit = get(entity.data, "facingExit", "Up")

    horizStart = isHorizontal(facingEntrance)
    horizEnd = isHorizontal(facingExit)

    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))
    widthStart = horizStart ? width : 8
    heightStart = horizStart ? 8 : height
    widthEnd = horizStart == horizEnd ? widthStart : heightStart
    heightEnd = horizStart == horizEnd ? heightStart : widthStart
    
    renderWIPPortal(ctx, facingEntrance, x, y, widthStart, heightStart)
    renderWIPPortal(ctx, facingExit, nx, ny, widthEnd, heightEnd)

    lineStartX = x + widthStart / 2
    lineStartY = y + heightStart / 2
    lineEndX = nx + widthEnd / 2
    lineEndY = ny + heightEnd / 2
    Ahorn.drawArrow(ctx, lineStartX, lineStartY, lineEndX, lineEndY, lineColor, headLength=6)
    Ahorn.drawArrow(ctx, lineEndX, lineEndY, lineStartX, lineStartY, lineColor, headLength=6)
end

function renderWIPPortal(ctx::Ahorn.Cairo.CairoContext, facing::String, x::Integer, y::Integer, width::Integer, height::Integer)
    if facing == "Up"
        Ahorn.drawRectangle(ctx, x, y + 6, width, 2)
        Ahorn.drawRectangle(ctx, x, y, 2, height)
        Ahorn.drawRectangle(ctx, x + width - 2, y, 2, height)
    elseif facing == "Down"
        Ahorn.drawRectangle(ctx, x, y, width, 2)
        Ahorn.drawRectangle(ctx, x, y, 2, height)
        Ahorn.drawRectangle(ctx, x + width - 2, y, 2, height)
    elseif facing == "Left"
        Ahorn.drawRectangle(ctx, x + 6, y, 2, height)
        Ahorn.drawRectangle(ctx, x, y, width, 2)
        Ahorn.drawRectangle(ctx, x, y + height - 2, width, 2)
    elseif facing == "Right"
        Ahorn.drawRectangle(ctx, x, y, 2, height)
        Ahorn.drawRectangle(ctx, x, y, width, 2)
        Ahorn.drawRectangle(ctx, x, y + height - 2, width, 2)
    end
end

end