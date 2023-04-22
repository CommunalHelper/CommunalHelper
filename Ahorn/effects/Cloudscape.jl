module CommunalHelperCloudscape

using ..Ahorn, Maple

@mapdef Effect "CommunalHelper/Cloudscape" Cloudscape(
    only::String="*",
    exclude::String="",
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
    offsetX::Number=0.0,
    offsetY::Number=0.0,
    parallaxX::Number=0.05,
    parallaxY::Number=0.05,
    innerDensity::Number=1.0,
    outerDensity::Number=1.0,
    innerRotation::Number=0.002,
    outerRotation::Number=0.2,
    rotationExponent::Number=2.0,
    hasBackgroundColor::Bool=false,
    additive::Bool=false,
)

placements = Cloudscape

function Ahorn.canFgBg(effect::Cloudscape)
    return true, true
end

end