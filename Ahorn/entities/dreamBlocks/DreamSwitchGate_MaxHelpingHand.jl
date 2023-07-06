module CommunalHelperDreamFlagSwitchGate

using ..Ahorn, Maple
using Ahorn.CommunalHelper
using Ahorn.CommunalHelperEntityPresets: CustomDreamBlockData

const entityData = appendkwargs(CustomDreamBlockData, :(
    persistent::Bool=false,
    flag::String="flag_touch_switch",
    icon::String="vanilla",
    inactiveColor::String="5FCDE4",
    activeColor::String="FFFFFF",
    finishColor::String="F141DF",
    shakeTime::Number=0.5,
    moveTime::Number=1.8,
    moveEased::Bool=true,
    allowReturn::Bool=false,
    moveSound::String="event:/game/general/touchswitch_gate_open",
    finishedSound::String="event:/game/general/touchswitch_gate_finish",
))
@mapdefdata Entity "CommunalHelper/MaxHelpingHand/DreamFlagSwitchGate" DreamFlagSwitchGate entityData

const placements = Ahorn.PlacementDict()

if isdefined(Ahorn, :MaxHelpingHand)
    placements["Dream Flag Switch Gate (Communal Helper, Maddie's Helping Hand)"] = Ahorn.EntityPlacement(
        DreamFlagSwitchGate,
        "rectangle",
        Dict{String,Any}(),
        function (entity)
            entity.data["nodes"] = [(
                Int(entity.data["x"]) + Int(entity.data["width"]),
                Int(entity.data["y"]),
            )]
        end,
    )
else
    @warn "Maddie's Helping Hand not detected: CommunalHelper+MaxHelpingHand plugins not loaded."
end

function getIconSprite(entity::DreamFlagSwitchGate)
    icon = get(entity.data, "icon", "vanilla")

    iconResource = "objects/switchgate/icon00"
    if icon != "vanilla"
        iconResource = "objects/MaxHelpingHand/flagSwitchGate/$(icon)/icon00"
    end

    return Ahorn.getSprite(iconResource, "Gameplay")
end

Ahorn.nodeLimits(entity::DreamFlagSwitchGate) = 1, 1

Ahorn.minimumSize(entity::DreamFlagSwitchGate) = 16, 16
Ahorn.resizable(entity::DreamFlagSwitchGate) = true, true

Ahorn.editingOrder(entity::DreamFlagSwitchGate) = String[
    "x",
    "y",
    "width",
    "height",
    "flag",
    "inactiveColor",
    "activeColor",
    "finishColor",
    "hitSound",
    "moveSound",
    "finishedSound",
    "shakeTime",
    "moveTime",
]

Ahorn.editingOptions(entity::DreamFlagSwitchGate) =
    Dict{String,Any}("icon" => Ahorn.MaxHelpingHandFlagSwitchGate.bundledIcons)

function Ahorn.selection(entity::DreamFlagSwitchGate)
    x, y = Ahorn.position(entity)
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [
        Ahorn.Rectangle(x, y, width, height),
        Ahorn.Rectangle(stopX, stopY, width, height),
    ]
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamFlagSwitchGate)
    startX, startY = Int(entity.data["x"]), Int(entity.data["y"])
    stopX, stopY = Int.(entity.data["nodes"][1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    renderDreamBlock(ctx, stopX, stopY, width, height, entity.data)

    Ahorn.drawArrow(ctx, startX + width / 2, startY + height / 2, stopX + width / 2, stopY + height / 2, Ahorn.colors.selection_selected_fc, headLength=6)

    iconSprite = getIconSprite(entity)
    Ahorn.drawImage(ctx, iconSprite, stopX + div(width - iconSprite.width, 2), stopY + div(height - iconSprite.height, 2))
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::DreamFlagSwitchGate)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    renderDreamBlock(ctx, x, y, width, height, entity.data)

    iconSprite = getIconSprite(entity)
    Ahorn.drawImage(ctx, iconSprite, x + div(width - iconSprite.width, 2), y + div(height - iconSprite.height, 2))
end

end
