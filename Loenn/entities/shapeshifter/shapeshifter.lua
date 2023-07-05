local utils = require "utils"
local fakeTilesHelper = require "helpers.fake_tiles"
local mods = require "mods"
local voxel = mods.requireFromPlugin "libraries.communal_helper_voxel"

local shapeshifter = {}

shapeshifter.name = "CommunalHelper/Shapeshifter"

shapeshifter.fieldInformation = {
    voxelWidth = {
        fieldType = "integer",
        minimumValue = 1,
    },
    voxelHeight = {
        fieldType = "integer",
        minimumValue = 1,
    },
    voxelDepth = {
        fieldType = "integer",
        minimumValue = 1,
    },
    startShake = {
        fieldType = "number",
        minimumValue = 0.0,
    },
    finishShake = {
        fieldType = "number",
        minimumValue = 0.0,
    },
    quakeTime = {
        fieldType = "number",
        minimumValue = 0.0,
    },
    rainbowMix = {
        fieldType = "number",
        minimumValue = 0.0,
        maximumValue = 1.0
    }
}

shapeshifter.placements = {
    {
        name = "shapeshifter",
        data = {
            voxelWidth = 1,
            voxelHeight = 1,
            voxelDepth = 1,
            startSound = "event:/new_content/game/10_farewell/quake_rockbreak",
            finishSound = "event:/game/general/touchswitch_gate_finish",
            startShake = 0.2,
            finishShake = 0.2,
            quakeTime = 0.5,
            rainbowMix = 0.2,
            model = "",
            atlas = "",
        }
    }
}

local function addTilesFromVoxel(sprites, vox, room, x, y, tx, ty)
    local _, _, sz = vox:size()
    for z = sz, 1, -1 do
        local fakeTiles = fakeTilesHelper.generateFakeTiles(room, tx, ty, vox[z], "tilesFg", false)
        local tiles = fakeTilesHelper.generateFakeTilesSprites(room, tx, ty, fakeTiles, "tilesFg", x, y)
        for _, sprite in ipairs(tiles) do
            table.insert(sprites, sprite)
        end
    end
end

function shapeshifter.sprite(room, entity)
    local sx, sy, sz = entity.voxelWidth or 1, entity.voxelHeight or 1, entity.voxelDepth or 1

    local model = entity.model or ""
    local vox = voxel.fromStringRepresentation(model, sx, sy, sz, "0")

    local x, y = entity.x or 0, entity.y or 0
    local tx, ty = math.floor(x / 8) + 1, math.floor(y / 8) + 1

    x = x - sx * 4
    y = y - sy * 4

    local sprites = {}
    addTilesFromVoxel(sprites, vox, room, x, y, tx, ty)

    return sprites
end

function shapeshifter.selection(room, entity)
    local sx, sy = entity.voxelWidth or 1, entity.voxelHeight or 1
    local x, y = entity.x or 0, entity.y or 0
    return utils.rectangle(x - sx * 4, y - sy * 4, sx * 8, sy * 8)
end

return shapeshifter
