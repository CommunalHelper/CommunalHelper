local drawableRectangle = require "structs.drawable_rectangle"
local utils = require "utils"
local fakeTilesHelper = require "helpers.fake_tiles"
local mods = require "mods"
local voxel = mods.requireFromPlugin "libraries.communal_helper_voxel"
local enums = require "consts.celeste_enums"

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
    rainbowMix = {
        fieldType = "number",
        minimumValue = 0.0,
        maximumValue = 1.0
    },
    surfaceSoundIndex = {
        editable = true,
        options = enums.tileset_sound_ids,
        fieldType = "integer",
    },
    defaultTile = {
        validator = function(input)
            return #input == 1
        end
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
            rainbowMix = 0.2,
            surfaceSoundIndex = 1,
            model = "",
            defaultTile = "0",
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

    local defaultTile = entity.defaultTile or "0"

    local model = entity.model or ""
    local vox = voxel.fromStringRepresentation(model, esx, esy, esz, defaultTile)

    local sprites = {}

    local paths = {}
    for _, other in ipairs(room.entities) do
        if other._name == "CommunalHelper/ShapeshifterPath" then
            table.insert(paths, {path = other, borrowed = false})
        end
    end

    local function spread(x, y, yaw, pitch, roll)
        local yawWrap = yaw % 4
        local pitchWrap = pitch % 4
        local rollWrap = roll % 4

        local rotatedVox = vox

        if rollWrap == 1 then rotatedVox = voxel.counterclockwiseRotationAboutZ(rotatedVox, "0")
        elseif rollWrap == 2 then rotatedVox = voxel.mirrorAboutZ(rotatedVox, "0")
        elseif rollWrap == 3 then rotatedVox = voxel.clockwiseRotationAboutZ(rotatedVox, "0")
        end

        if pitchWrap == 1 then rotatedVox = voxel.counterclockwiseRotationAboutX(rotatedVox, "0")
        elseif pitchWrap == 2 then rotatedVox = voxel.mirrorAboutX(rotatedVox, "0")
        elseif pitchWrap == 3 then rotatedVox = voxel.clockwiseRotationAboutX(rotatedVox, "0")
        end

        if yawWrap == 1 then rotatedVox = voxel.counterclockwiseRotationAboutY(rotatedVox, "0")
        elseif yawWrap == 2 then rotatedVox = voxel.mirrorAboutY(rotatedVox, "0")
        elseif yawWrap == 3 then rotatedVox = voxel.clockwiseRotationAboutY(rotatedVox, "0")
        end

        local sx, sy, _ = rotatedVox:size()

        table.insert(sprites, drawableRectangle.fromRectangle("bordered", x - sx * 4, y - sy * 4, sx * 8, sy * 8, {1.0, 1.0, 1.0, 0.25}, {1.0, 1.0, 1.0, 0.5}))

        addTilesFromVoxel(sprites, rotatedVox, room, x - sx * 4, y - sy * 4)

        for _, current in ipairs(paths) do
            if not current.borrowed then
                local tarx = current.path.nodes[3].x + x - current.path.x
                local tary = current.path.nodes[3].y + y - current.path.y

                local pathBounds = utils.rectangle(current.path.x - 2, current.path.y - 2, 4, 4)
                local blockBounds = utils.rectangle(x - sx * 4, y - sy * 4, sx * 8, sy * 8)

                if utils.intersection(pathBounds, blockBounds) then
                    local pathyaw = current.path.rotateYaw
                    local pathpitch = current.path.rotatePitch
                    local pathroll = current.path.rotateRoll

                    current.borrowed = true
                    spread(tarx, tary, yaw + pathyaw, pitch + pathpitch, roll + pathroll)
                end
            end
        end
    end

    spread(ex, ey, 0, 0, 0)

    return sprites
end

function shapeshifter.selection(room, entity)
    local sx, sy = entity.voxelWidth or 1, entity.voxelHeight or 1
    local x, y = entity.x or 0, entity.y or 0
    return utils.rectangle(x - sx * 4, y - sy * 4, sx * 8, sy * 8)
end

return shapeshifter
