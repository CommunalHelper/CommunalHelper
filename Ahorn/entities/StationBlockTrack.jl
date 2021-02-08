module CommunalHelperStationBlockTrack
using ..Ahorn, Maple

const switchStates = ["None", "On", "Off"]

@mapdef Entity "CommunalHelper/StationBlockTrack" StationBlockTrack(x::Integer, y::Integer,
    width::Integer = 24, height::Integer = 24,
    horizontal::Bool = false,
    trackSwitchState::String = "None",
    offrampNode1::Bool = false, offrampNode2::Bool = false, multiBlockTrack::Bool = false)

const placements = Ahorn.PlacementDict(
    "Station Block Track ($orientation, $stateLabel) (Communal Helper)" => Ahorn.EntityPlacement(
        StationBlockTrack,
        "rectangle",
        Dict{String, Any}(
            "horizontal" => orientation == "Horizontal",
            "trackSwitchState" => state,
        )
    ) for orientation in ["Vertical", "Horizontal"], (state, stateLabel) in zip(switchStates, ["No Switching", "Switch On", "Switch Off"])
) 

Ahorn.editingIgnored(entity::StationBlockTrack, multiple::Bool=false) = multiple ? String["x", "y", "width", "height", "nodes", "offrampNode1", "offrampNode2"] : String["offrampNode1", "offrampNode2"]

Ahorn.minimumSize(entity::StationBlockTrack) = 24, 24

Ahorn.resizable(entity::StationBlockTrack) = Bool(get(entity.data, "horizontal", false)), !(Bool(get(entity.data, "horizontal", false)))

Ahorn.editingOptions(entity::StationBlockTrack) = Dict{String, Any}(
    "trackSwitchState" => switchStates
)

# very weird rotation
function Ahorn.rotated(entity::StationBlockTrack, steps::Int) 
    horiz = Bool(get(entity.data, "horizontal", false))
    trackSwitchState = get(entity.data, "trackSwitchState", "None")
    return StationBlockTrack(entity.x, entity.y, entity.height, entity.width, !horiz, trackSwitchState)
end

function Ahorn.selection(entity::StationBlockTrack)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))
    horiz = Bool(get(entity.data, "horizontal", false))
	
    return horiz ? Ahorn.Rectangle(x, y, width, 8) : Ahorn.Rectangle(x, y, 8, height)
end

const nodeSprite = "objects/CommunalHelper/stationBlock/tracks/outline/node"
const hTrack = "objects/CommunalHelper/stationBlock/tracks/outline/h"
const vTrack = "objects/CommunalHelper/stationBlock/tracks/outline/v"
const arrows = "objects/CommunalHelper/stationBlock/tracks/outline/arrows"

const noneColor = (255, 255, 255, 255) ./ 255
const onColor = (66, 167, 255, 255) ./ 255
const offColor = (255, 48, 131, 255) ./ 255

function getTrackColor(entity::StationBlockTrack)
    trackSwitchState = get(entity.data, "trackSwitchState", "None")
    if trackSwitchState == "On"
        return onColor
    elseif trackSwitchState == "Off"
        return offColor
    else
        return noneColor
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::StationBlockTrack)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 24))
    height = Int(get(entity.data, "height", 24))

    horiz = Bool(get(entity.data, "horizontal", false))

    color = getTrackColor(entity)

    offramp1 = Bool(get(entity.data, "offrampNode1", false))
    offramp2 = Bool(get(entity.data, "offrampNode2", false))
    
    if horiz
        tilesWidth = div(width, 8)

        for i in 2:tilesWidth - 1
            Ahorn.drawImage(ctx, hTrack, x + (i - 1) * 8, y, tint=color)
        end

        Ahorn.drawImage(ctx, nodeSprite, x, y, 0, 0, 8, 8, tint=color)
        Ahorn.drawImage(ctx, nodeSprite, x + width - 8, y, 8, 0, 8, 8, tint=color)
        if offramp1
            Ahorn.drawImage(ctx, arrows, x - 6, y, 0, 8, 8, 8)
        end
        if offramp2
            Ahorn.drawImage(ctx, arrows, x + width - 2, y, 8, 0, 8, 8)
        end
    else
        tilesHeight = div(height, 8)

        for i in 2:tilesHeight - 1
            Ahorn.drawImage(ctx, vTrack, x, y + (i - 1) * 8, tint=color)
        end

        Ahorn.drawImage(ctx, nodeSprite, x, y, 8, 8, 8, 8, tint=color)
        Ahorn.drawImage(ctx, nodeSprite, x, y + height - 8, 0, 8, 8, 8, tint=color)
        if offramp1
            Ahorn.drawImage(ctx, arrows, x, y - 6, 0, 0, 8, 8)
        end
        if offramp2
            Ahorn.drawImage(ctx, arrows, x, y + height - 2, 8, 8, 8, 8)
        end
    end

end

end