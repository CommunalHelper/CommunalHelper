module CommunalHelperAdventureHelper

using ..Ahorn, Maple
using Ahorn.CommunalHelper

# Stolen from AdventureHelper
@mapdef Entity "CommunalHelper/AdventureHelper/CustomCrystalHeart" CustomCrystalHeart(
    x::Integer,
    y::Integer,
    color::String="00a81f",
    path::String="",
    nodes::Array{Tuple{Integer,Integer},1}=Tuple{Integer,Integer}[],
)

function getPlacements()
    # Check if there's an adventurehelper folder/zip in the mods folder
    # This is bad and can easily fail, but whatever
    if CommunalHelper.detectMod("adventurehelper")
        return Ahorn.PlacementDict(
            "Crystal Heart (Communal Helper, Adventure Helper)" => Ahorn.EntityPlacement(
                CustomCrystalHeart,
                "point",
                Dict{String,Any}(),
                function (entity)
                    entity.data["nodes"] = [
                        (Int(entity.data["x"]) - 20, Int(entity.data["y"]) - 20),
                        (Int(entity.data["x"]), Int(entity.data["y"]) - 20),
                        (Int(entity.data["x"]) + 20, Int(entity.data["y"]) - 20),
                    ]
                end,
            ),
        )
    else
        @warn "AdventureHelper not detected: CommunalHelper+AdventureHelper plugins not loaded."
        return Ahorn.PlacementDict()
    end
end

placements = getPlacements()

Ahorn.nodeLimits(entity::CustomCrystalHeart) = 1, -1

const shardSprites = Tuple{String,String}[(
        "collectables/CommunalHelper/heartGemShard/shard_outline0$i",
        "collectables/CommunalHelper/heartGemShard/shard0$i",
    ) for i in 0:2
]

function Ahorn.selection(entity::CustomCrystalHeart)
    x, y = Ahorn.position(entity)

    nodes = get(entity.data, "nodes", ())

    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle("collectables/heartGem/3/00", x, y)]

    for i in 1:length(nodes)
        nx, ny = nodes[i]

        push!(res, Ahorn.getSpriteRectangle(shardSprites[mod1(i, 3)][1], nx, ny))
    end

    return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::CustomCrystalHeart)
    x, y = Ahorn.position(entity)

    nodes = get(entity.data, "nodes", ())

    for i in 1:length(nodes)
        nx, ny = nodes[i]

        Ahorn.drawLines(
            ctx,
            Tuple{Number,Number}[(x, y), (nx, ny)],
            Ahorn.colors.selection_selected_fc,
        )
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CustomCrystalHeart, room::Maple.Room)
    x, y = Ahorn.position(entity)
    path = get(entity.data, "path", "")
    tint = CommunalHelper.hexToRGBA(get(entity.data, "color", "663931"))
    sprite = String

    if path == "heartgem0"
        sprite = "collectables/heartGem/0/00"
    elseif path == "heartgem1"
        sprite = "collectables/heartGem/1/00"
    elseif path == "heartgem2"
        sprite = "collectables/heartGem/2/00"
    elseif path == "heartgem3"
        sprite = "collectables/heartGem/3/00"
    elseif path == ""
        sprite = "collectables/AdventureHelper/RecolorHeart_Outline/00"
        tint = CommunalHelper.hexToRGBA(get(entity.data, "color", "663931"))
        Ahorn.drawSprite(
            ctx,
            "collectables/AdventureHelper/RecolorHeart/00",
            x,
            y,
            tint=tint,
        )
    else
        sprite = "collectables/heartGem/3/00"
    end

    nodes = get(entity.data, "nodes", ())
    for i in 1:length(nodes)
        nx, ny = nodes[i]
        sprIdx = mod1(i, 3)
        Ahorn.drawSprite(ctx, shardSprites[sprIdx][1], nx, ny)
        Ahorn.drawSprite(ctx, shardSprites[sprIdx][2], nx, ny, tint=tint)
    end

    Ahorn.drawSprite(ctx, sprite, x, y)
end

end
