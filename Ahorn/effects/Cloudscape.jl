module CommunalHelperCloudscape

using ..Ahorn, Maple

@mapdef Effect "CommunalHelper/Cloudscape" Cloudscape(
    seed::String="",
    colors::String="6d8ada,aea0c1,d9cbbc",
    bgColor::String="4f9af7",
    innerRadius::Number=40.0,
    outerRadius::Number=400.0,
    rings::Integer=24,
    lightning::Bool=false,
    lightningColors::String="384bc8,7a50d0,c84ddd,3397e2",
    lightningFlashColor::String="ffffff",
    lightningMinDelay::Number=5.0,
    lightningMaxDelay::Number=40.0,
    lightningMinDuration::Number=0.5,
    lightningMaxDuration::Number=1.0,
    lightningIntensity::Number=0.4,
)

placements = Cloudscape

function Ahorn.canFgBg(effect::Cloudscape)
    return false, true
end

end