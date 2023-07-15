local matrix = require "utils.matrix"

local voxel = {}

local mt = {}
mt.__index = {}

function mt.__index:get(x, y, z, default)
    return z >= 1 and z <= self._sz and self[z]:get(x, y, default) or default
end

function mt.__index:size()
    return self._sx, self._sy, self._sz
end

function voxel.create(sx, sy, sz)
    local vox = {
        _type = "voxel",
        _sx = sx,
        _sy = sy,
        _sz = sz
    }

    return setmetatable(vox, mt)
end

function voxel.fromStringRepresentation(str, sx, sy, sz, default)
    local vox = voxel.create(sx, sy, sz)

    local len = string.len(str)
    local offset = 0
    local func = function(i)
        local index = offset + i
        return index <= len and string.sub(str, index, index) or default
    end

    local matSize = sx * sy
    for z = 0, sz - 1 do
        offset = z * matSize
        vox[z + 1] = matrix.fromFunction(func, sx, sy)
    end

    return setmetatable(vox, mt)
end

--[[
    this is a little ugly, but basically, a "voxel transformation", as i called it,
    is a table with just two functions:

    * the first one spits out the new sizes of the voxel (3d array) given the sizes of the old one.
    example: the following function
                (sx, sy, sz) -> sy, sx, sz
            swaps the width and height of the array.

    * the second takes in the old array's sizes, and the coordinate of any cell of the new array,
    to determines which coordinates of the old array they should correspond to.
    example: the following function
                (x, y, z, sx, sy, sz) -> sx - x + 1, y, z
            flips the array horizontally (given the new array has the same size as the old one).
            this one
                (x, y, z, sx, sy, sz) -> x, y, z
            does absolutely nothing.

    when the first function is `nil`, the new array will have the same sizes as the old one.
--]]

local function transform(vox, transformation, default)
    local sx, sy, sz = vox:size()
    local nsx, nsy, nsz = sx, sy, sz
    if transformation.sizefunc then
        nsx, nsy, nsz = transformation.sizefunc(sx, sy, sz)
    end

    local new = voxel.create(nsx, nsy, nsz)

    local z = 1
    local f = function(i)
        local x = (i - 1) % nsx + 1
        local y = math.floor((i - 1) / nsx) + 1
        local nx, ny, nz = transformation.coordsfunc(x, y, z, sx, sy, sz)
        return vox:get(nx, ny, nz, default)
    end

    while z <= nsz do
        new[z] = matrix.fromFunction(f, nsx, nsy)
        z = z + 1
    end

    return setmetatable(new, mt)
end

function voxel.mirrorAboutX(vox, default)
    return transform(
        vox,
        {
            coordsfunc = function(x, y, z, sx, sy, sz)
                return x, sy - y + 1, sz - z + 1
            end
        },
        default
    )
end

function voxel.counterclockwiseRotationAboutX(vox, default)
    return transform(
        vox,
        {
            sizefunc = function(sx, sy, sz)
                return sx, sz, sy
            end,
            coordsfunc = function(x, y, z, sx, sy, sz)
                return x, z, sz - y + 1
            end
        },
        default
    )
end

function voxel.clockwiseRotationAboutX(vox, default)
    return transform(
        vox,
        {
            sizefunc = function(sx, sy, sz)
                return sx, sz, sy
            end,
            coordsfunc = function(x, y, z, sx, sy, sz)
                return x, sy - z + 1, y
            end
        },
        default
    )
end

function voxel.mirrorAboutY(vox, default)
    return transform(
        vox,
        {
            coordsfunc = function(x, y, z, sx, sy, sz)
                return sx - x + 1, y, sz - z + 1
            end
        },
        default
    )
end

function voxel.clockwiseRotationAboutY(vox, default)
    return transform(
        vox,
        {
            sizefunc = function(sx, sy, sz)
                return sz, sy, sx
            end,
            coordsfunc = function(x, y, z, sx, sy, sz)
                return sx - z + 1, y, x
            end
        },
        default
    )
end

function voxel.counterclockwiseRotationAboutY(vox, default)
    return transform(
        vox,
        {
            sizefunc = function(sx, sy, sz)
                return sz, sy, sx
            end,
            coordsfunc = function(x, y, z, sx, sy, sz)
                return z, y, sz - x + 1
            end
        },
        default
    )
end

function voxel.mirrorAboutZ(vox, default)
    return transform(
        vox,
        {
            coordsfunc = function(x, y, z, sx, sy, sz)
                return sx - x + 1, sy - y + 1, z
            end
        },
        default
    )
end

function voxel.clockwiseRotationAboutZ(vox, default)
    return transform(
        vox,
        {
            sizefunc = function(sx, sy, sz)
                return sy, sx, sz
            end,
            coordsfunc = function(x, y, z, sx, sy, sz)
                return y, sy - x + 1, z
            end
        },
        default
    )
end

function voxel.counterclockwiseRotationAboutZ(vox, default)
    return transform(
        vox,
        {
            sizefunc = function(sx, sy, sz)
                return sy, sx, sz
            end,
            coordsfunc = function(x, y, z, sx, sy, sz)
                return sx - y + 1, x, z
            end
        },
        default
    )
end

function voxel.log(vox)
    local sx, sy, sz = vox:size()
    local s = ""
    for z = 1, sz do
        for y = 1, sy do
            for x = 1, sx do
                s = s .. vox:get(x, y, z, "0")
            end
            s = s .. "\n"
        end
        s = s .. "\n"
    end
    print(s)
end

return voxel
