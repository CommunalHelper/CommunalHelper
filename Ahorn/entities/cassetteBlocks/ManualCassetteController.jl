module CommunalHelperManualCassetteController

using ..Ahorn, Maple
using Ahorn.CommunalHelper

@mapdef Entity "CommunalHelper/ManualCassetteController" ManualCassetteController(
    x::Integer,
    y::Integer,
    startIndex::Int=0,
)

const placements = Ahorn.PlacementDict(
    "Manual Cassette Controller (Communal Helper)" => Ahorn.EntityPlacement(
        ManualCassetteController,
    ),
)

Ahorn.editingOptions(entity::ManualCassetteController) = Dict{String,Any}(
    "startIndex" => cassetteColorNames,
)

const alt = rand(1:100) == 42
const sprite = "objects/CommunalHelper/manualCassetteController/icon$(alt ? "_wacked" : "")"

function Ahorn.selection(entity::ManualCassetteController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ManualCassetteController)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end
