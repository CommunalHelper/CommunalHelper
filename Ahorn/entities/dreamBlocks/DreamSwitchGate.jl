module CommunalHelperDreamSwitchGate

using ..Ahorn, Maple
using Ahorn.CommunalHelper
using Ahorn.CommunalHelperEntityPresets: CustomDreamBlockData

const entityData = appendkwargs(CustomDreamBlockData, :(
    permanent::Bool=false,
))
@mapdefdata Entity "CommunalHelper/DreamSwitchGate" DreamSwitchGate entityData

const placements = Ahorn.PlacementDict(
    "Dream Switch Gate (Communal Helper)" => Ahorn.EntityPlacement(
        DreamSwitchGate,
        "rectangle",
        Dict{String,Any}(),
        function (entity)
            entity.data["nodes"] = [(
                Int(entity.data["x"]) + Int(entity.data["width"]),
                Int(entity.data["y"]),
            )]
        end,
    ),
)

const iconSprite = Ahorn.getSprite("objects/switchgate/icon00", "Gameplay")

Ahorn.nodeLimits(entity::DreamSwitchGate) = 1, 1

Ahorn.minimumSize(entity::DreamSwitchGate) = 16, 16
Ahorn.resizable(entity::DreamSwitchGate) = true, true

function Ahorn.selection(entity::DreamSwitchGate)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [
        Ahorn.Rectangle(x, y, width, height),
        Ahorn.Rectangle(stopX, stopY, width, height),
    ]
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamSwitchGate)
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderDreamBlock(ctx, stopX, stopY, width, height, entity.data)

    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)

    Ahorn.drawImage(ctx, iconSprite, stopX + div(width - iconSprite.width, 2), stopY + div(height - iconSprite.height, 2))
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamSwitchGate)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    renderDreamBlock(ctx, x, y, width, height, entity.data)

    Ahorn.drawImage(ctx, iconSprite, x + div(width - iconSprite.width, 2), y + div(height - iconSprite.height, 2))
end

end
