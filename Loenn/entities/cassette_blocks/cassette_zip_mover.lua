local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local communalHelper = require("mods").requireFromPlugin("libraries.communal_helper")
local utils = require("utils")

local cassetteZipMover = {}

local ropeColors = {
    {110 / 255, 189 / 255, 245 / 255, 1.0},
    {194 / 255, 116 / 255, 171 / 255, 1.0},
    {227 / 255, 214 / 255, 148 / 255, 1.0},
    {128 / 255, 224 / 255, 141 / 255, 1.0},
}

cassetteZipMover.name = "CommunalHelper/CassetteZipMover"
cassetteZipMover.minimumSize = {16, 16}
cassetteZipMover.fieldInformation = {
    index = {
        fieldType = "integer",
    }
}
cassetteZipMover.nodeVisibility = "never"
cassetteZipMover.nodeLimits = {1, -1}

cassetteZipMover.placements = {}
for i = 1, 4 do
    cassetteZipMover.placements[i] = {
        name = string.format("cassette_block_%s", i - 1),
        data = {
            index = i - 1,
            tempo = 1.0,
            width = 16,
            height = 16,
            permanent = false,
            waiting = false,
            ticking = false,
            noReturn = false,
            customColor = "",
        }
    }
end

local function addNodeSprites(sprites, entity, cogColor, ropeColor, cogTexture, centerX, centerY, centerNodeX, centerNodeY)
    local nodeCogSprite = drawableSprite.fromTexture(cogTexture, entity)
    nodeCogSprite:setColor(cogColor)

    nodeCogSprite:setPosition(centerNodeX, centerNodeY)
    nodeCogSprite:setJustification(0.5, 0.5)

    local points = {centerX, centerY, centerNodeX, centerNodeY}
    local leftLine = drawableLine.fromPoints(points, ropeColor, 1)
    local rightLine = drawableLine.fromPoints(points, ropeColor, 1)

    leftLine:setOffset(0, 4.5)
    rightLine:setOffset(0, -4.5)

    leftLine.depth = 5000
    rightLine.depth = 5000

    for _, sprite in ipairs(leftLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    for _, sprite in ipairs(rightLine:getDrawableSprite()) do
        table.insert(sprites, sprite)
    end

    table.insert(sprites, nodeCogSprite)
end

function cassetteZipMover.sprite(room, entity)
    local sprites = communalHelper.getCustomCassetteBlockSprites(room, entity)

    local x, y = entity.x or 0, entity.y or 0
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local centerX, centerY = x + halfWidth, y + halfHeight

    local color = communalHelper.getCustomCassetteBlockColor(entity)

    local i = entity.index or 0
    local ropeColor = (entity.customColor ~= "" and color) or ropeColors[i + 1] or ropeColors[1]

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local cx, cy = centerX, centerY
    for _, node in ipairs(nodes) do
        local centerNodeX, centerNodeY = node.x + halfWidth, node.y + halfHeight

        addNodeSprites(sprites, entity, color, ropeColor, "objects/CommunalHelper/cassetteZipMover/cog", cx, cy, centerNodeX, centerNodeY)
    
        cx, cy = centerNodeX, centerNodeY
    end
    
    if entity.noReturn then
        local cross = drawableSprite.fromTexture("objects/CommunalHelper/cassetteMoveBlock/x")
        cross:setPosition(centerX, centerY)
        cross:setColor(color)
        cross.depth = -11

        table.insert(sprites, cross)
    end

    return sprites
end

function cassetteZipMover.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local mainRectangle = utils.rectangle(x, y, width, height)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeRectangles = {}
    for _, node in ipairs(nodes) do
        local centerNodeX, centerNodeY = node.x + halfWidth, node.y + halfHeight

        table.insert(nodeRectangles, utils.rectangle(centerNodeX - 5, centerNodeY - 5, 10, 10))
    end

    return mainRectangle, nodeRectangles
end

return cassetteZipMover