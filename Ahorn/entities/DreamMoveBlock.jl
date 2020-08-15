module CommunalHelperDreamMoveBlock

using ..Ahorn, Maple

@mapdef Entity "CommunalHelper/DreamMoveBlock" DreamMoveBlock(x::Integer, 
                                                              y::Integer, 
                                                              width::Integer=Maple.defaultBlockWidth, 
                                                              height::Integer=Maple.defaultBlockHeight,
                                                              direction::String="Right", 
                                                              fast::Bool=false,
                                                              noCollide::Bool=false,
                                                              featherMode::Bool=false,
                                                              oneUse::Bool=false) 

const placements = Ahorn.PlacementDict(
    "Dream Move Block ($direction) (Communal Helper)" => Ahorn.EntityPlacement(
        DreamMoveBlock,
        "rectangle",
        Dict{String, Any}(
            "direction" => direction
        )
    ) for direction in Maple.move_block_directions
)

Ahorn.editingOptions(entity::DreamMoveBlock) = Dict{String, Any}(
    "direction" => Maple.move_block_directions
)
Ahorn.minimumSize(entity::DreamMoveBlock) = 16, 16
Ahorn.resizable(entity::DreamMoveBlock) = true, true

Ahorn.selection(entity::DreamMoveBlock) = Ahorn.getEntityRectangle(entity)

const arrows = Dict{String, String}(
    "up" => "objects/CommunalHelper/dreamMoveBlock/arrow02",
    "left" => "objects/CommunalHelper/dreamMoveBlock/arrow04",
    "right" => "objects/CommunalHelper/dreamMoveBlock/arrow00",
    "down" => "objects/CommunalHelper/dreamMoveBlock/arrow06",
)

function renderSpaceJam(ctx::Ahorn.Cairo.CairoContext, x::Number, y::Number, width::Number, height::Number, featherMode::Bool, oneUse::Bool)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1)

    fillColor = featherMode ? (0.31, 0.69, 1.0, 0.4) : (0.0, 0.0, 0.0, 0.4)
	lineColor = oneUse ? (1.0, 0.0, 0.0, 1.0) : (1.0, 1.0, 1.0, 1.0)
    Ahorn.drawRectangle(ctx, x, y, width, height, fillColor, lineColor)

    Ahorn.restore(ctx)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DreamMoveBlock, room::Maple.Room)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    renderSpaceJam(ctx, 0, 0, width, height, get(entity.data, "featherMode", false), get(entity.data, "oneUse", false))

    direction = lowercase(get(entity.data, "direction", "up"))
    arrowSprite = Ahorn.getSprite(arrows[lowercase(direction)], "Gameplay")
    Ahorn.drawImage(ctx, arrowSprite, div(width - arrowSprite.width, 2), div(height - arrowSprite.height, 2))
end

end