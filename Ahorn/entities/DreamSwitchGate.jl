module CommunalHelperDreamSwitchGate
using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamSwitchGate" DreamSwitchGate(x::Integer, y::Integer, 
	width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    featherMode::Bool = false, oneUse::Bool = false, doubleRefill::Bool=false, permanent::Bool = false)


const placements = Ahorn.PlacementDict(
    "Dream Switch Gate (Communal Helper)" => Ahorn.EntityPlacement(
        DreamSwitchGate,
        "rectangle",
        Dict{String, Any}(),
		function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]), Int(entity.data["y"]))]
        end
    )
)

iconSprite = Ahorn.getSprite("objects/switchgate/icon00", "Gameplay")

Ahorn.nodeLimits(entity::DreamSwitchGate) = 1, 1

Ahorn.minimumSize(entity::DreamSwitchGate) = 16, 16
Ahorn.resizable(entity::DreamSwitchGate) = true, true

function Ahorn.selection(entity::DreamSwitchGate)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(stopX, stopY, width, height)]
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamSwitchGate, room::Maple.Room)
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)

    fillColor = get(entity.data, "featherMode", false) ? (0.31, 0.69, 1.0, 0.4) : (0.0, 0.0, 0.0, 0.4)
	 lineColor = get(entity.data, "oneUse", false) ? (1.0, 0.0, 0.0, 1.0) : (1.0, 1.0, 1.0, 1.0)
    Ahorn.drawRectangle(ctx, stopX, stopY, width, height, fillColor, lineColor)

    Ahorn.restore(ctx)
                
    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)
    
    Ahorn.drawImage(ctx, iconSprite, stopX + div(width - iconSprite.width, 2), stopY + div(height - iconSprite.height, 2))
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamSwitchGate, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)

    fillColor = get(entity.data, "featherMode", false) ? (0.31, 0.69, 1.0, 0.4) : (0.0, 0.0, 0.0, 0.4)
	 lineColor = get(entity.data, "oneUse", false) ? (1.0, 0.0, 0.0, 1.0) : (1.0, 1.0, 1.0, 1.0)
    Ahorn.drawRectangle(ctx, x, y, width, height, fillColor, lineColor)

    Ahorn.restore(ctx)
    
    Ahorn.drawImage(ctx, iconSprite, x + div(width - iconSprite.width, 2), y + div(height - iconSprite.height, 2))
end

end