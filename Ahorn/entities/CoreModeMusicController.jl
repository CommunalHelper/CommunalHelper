module CommunalHelperCoreModeMusicController

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/CoreModeMusicController" Controller(
    x::Int,
    y::Int,
    params::String="",
    hot::Number=1.0,
    cold::Number=0.0,
    none::Number=0.0,
    disable::Bool=false
)

const placements = Ahorn.PlacementDict(
    "Core Mode Music Controller (Communal Helper)" => Ahorn.EntityPlacement(
        Controller,
    ),
    "Core Mode Music Controller (Communal Helper) (Disable)" => Ahorn.EntityPlacement(
        Controller,
        "point",
        Dict{String, Any}(
            "disable" => true,
        ),
    ),
)

const spriteEnable = "objects/CommunalHelper/coreModeMusicController/iconEnable"
const spriteDisable = "objects/CommunalHelper/coreModeMusicController/iconDisable"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Controller) = Ahorn.drawImage(ctx, Bool(get(entity.data, "disable", false)) ? spriteDisable : spriteEnable, -8, -11)

function Ahorn.selection(entity::Controller)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 8, y - 11, 22, 22)
end

end
