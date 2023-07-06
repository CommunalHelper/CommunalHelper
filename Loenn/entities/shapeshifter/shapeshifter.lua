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
    fakeoutTime = {
        fieldType = "number",
        minimumValue = 0.0,
    },
    fakeoutDistance = {
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
            fakeoutTime = 0.75,
            fakeoutDistance = 32.0,
            rainbowMix = 0.2,
            model = "",
            atlas = "",
        }
    }
}

local function addTilesFromVoxel(sprites, vox, room, x, y)
    local tx, ty = math.floor(x / 8) + 1, math.floor(y / 8) + 1
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
    local ex, ey = entity.x or 0, entity.y or 0
    local esx, esy, esz = entity.voxelWidth or 1, entity.voxelHeight or 1, entity.voxelDepth or 1

    local model = entity.model or ""
    local evox = voxel.fromStringRepresentation(model, esx, esy, esz, "0")

    local sprites = {}

    local paths = {}
    for _, other in ipairs(room.entities) do
        if other._name == "CommunalHelper/ShapeshifterPath" then
            table.insert(paths, {path = other, borrowed = false})
        end
    end

    local function spread(x, y, vox)
        local sx, sy, _ = vox:size()
        addTilesFromVoxel(sprites, vox, room, x - sx * 4, y - sy * 4)
        for _, current in ipairs(paths) do
            if not current.borrowed then
                local tarx = current.path.nodes[3].x + x - current.path.x
                local tary = current.path.nodes[3].y + y - current.path.y

                local pathBounds = utils.rectangle(current.path.x - 2, current.path.y - 2, 4, 4)
                local blockBounds = utils.rectangle(x - sx * 4, y - sy * 4, sx * 8, sy * 8)

                if utils.intersection(pathBounds, blockBounds) then
                    local yaw = current.path.rotateYaw % 4
                    local pitch = current.path.rotatePitch % 4
                    local roll = current.path.rotateRoll % 4

                    local next_vox = vox

                    if roll == 1 then next_vox = voxel.counterclockwiseRotationAboutZ(next_vox, "0")
                    elseif roll == 2 then next_vox = voxel.mirrorAboutZ(next_vox, "0")
                    elseif roll == 3 then next_vox = voxel.clockwiseRotationAboutZ(next_vox, "0")
                    end

                    if pitch == 1 then next_vox = voxel.counterclockwiseRotationAboutX(next_vox, "0")
                    elseif pitch == 2 then next_vox = voxel.mirrorAboutX(next_vox, "0")
                    elseif pitch == 3 then next_vox = voxel.clockwiseRotationAboutX(next_vox, "0")
                    end

                    if yaw == 1 then next_vox = voxel.counterclockwiseRotationAboutY(next_vox, "0")
                    elseif yaw == 2 then next_vox = voxel.mirrorAboutY(next_vox, "0")
                    elseif yaw == 3 then next_vox = voxel.clockwiseRotationAboutY(next_vox, "0")
                    end

                    current.borrowed = true
                    spread(tarx, tary, next_vox)
                end
            end
        end
    end

    spread(ex, ey, evox)

    return sprites
end

function shapeshifter.selection(room, entity)
    local sx, sy = entity.voxelWidth or 1, entity.voxelHeight or 1
    local x, y = entity.x or 0, entity.y or 0
    return utils.rectangle(x - sx * 4, y - sy * 4, sx * 8, sy * 8)
end

return shapeshifter
