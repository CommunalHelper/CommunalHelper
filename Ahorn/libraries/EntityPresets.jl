module CommunalHelperEntityPresets

const CustomDreamBlockData = :(dreamblock(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    height::Integer=Maple.defaultBlockHeight,
    featherMode::Bool=false,
    oneUse::Bool=false,
    refillCount::Integer=-1,
    below::Bool=false,
    quickDestroy::Bool=false,
))

const CustomCassetteBlockData = :(cassetteblock(
    x::Integer,
    y::Integer,
    width::Integer=Maple.defaultBlockWidth,
    height::Integer=Maple.defaultBlockHeight,
    index::Integer=0,
    tempo::Number=1.0,
    customColor="",
))

end