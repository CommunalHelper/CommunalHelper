local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableRectangle = require("structs.drawable_rectangle")
local utils = require("utils")

local blockTex = "objects/CommunalHelper/pinwheelBlock/block"
local anchorTex = "objects/CommunalHelper/pinwheelBlock/block-anchor"
local nodeTex = "objects/CommunalHelper/pinwheelBlock/pinwheel-anchor"
local armTex = "objects/CommunalHelper/pinwheelBlock/connector00"
local blockNinePatchOptions = {
    borderMode = "repeat"
}

local centerColor = {0, 0, 0}
local ropeColor = {102 / 255, 57 / 255, 49 / 255}

local pinwheelBlock = {}

pinwheelBlock.name = "CommunalHelper/PinwheelBlock"
pinwheelBlock.depth = -9999
pinwheelBlock.nodeVisibility = "always"
pinwheelBlock.nodeTexture = nodeTex
pinwheelBlock.nodeLineRenderType = "none"
pinwheelBlock.nodeLimits = {1, 1}
pinwheelBlock.warnBelowSize = {8, 8}
pinwheelBlock.fieldInformation = {
    group = {
        fieldType = "integer",
    },
	easeType = {
		fieldType = "string",
		editable = false,
		options = {
			{ "Linear", "linear" },
			{ "Cubic", "cubic" },
			{ "Hybrid", "hybrid" },
			{ "Cubic In", "cubicin" },
			{ "Cubic Out", "cubicout" },

		}
	}
}
pinwheelBlock.placements = {
	{
        name = "pinwheelBlock",
        data = {
            width = 24,
            height = 24,
			group = 1,
			moveTime = 0.75,
			rotationDegrees = 180.0,
			easeType = "Hybrid",
			texturesPath = "",
        }
    }
}

local function addBlockSprites(sprites, entity, blockTexture, x, y, width, height)
    local frameNinePatch = drawableNinePatch.fromTexture(blockTexture, blockNinePatchOptions, x, y, width, height)
    local frameSprites = frameNinePatch:getDrawableSprite()
    for _, sprite in ipairs(frameSprites) do
        table.insert(sprites, sprite)
    end
end

local function addArmSprites(sprites, entity, armTexture, x, y, width, height, nodeX, nodeY)
	local armSprite = drawableSprite.fromTexture(armTexture, entity)
	local centerX, centerY = x + width / 2, y + height / 2
	local dx, dy = nodeX - centerX, nodeY - centerY
	local distance = math.sqrt(dx * dx + dy * dy)
	local angle = math.atan2(dy, dx)

	for i = 0, distance, 16 do
		local armSpriteInstance = drawableSprite.fromTexture(armTexture, entity)
		armSpriteInstance:setJustification(0.5, 0.5)
		armSpriteInstance:addPosition((width / 2) + dx * ((i + 0.5) / distance), (height / 2) + dy * ((i + 0.5) / distance))
		armSpriteInstance.rotation = angle
		table.insert(sprites, armSpriteInstance)
	end
end

local function addAnchorSprite(sprites, entity, anchorTexture, x, y, width, height)
	local blockAnchorSprite = drawableSprite.fromTexture(anchorTexture, entity)
	blockAnchorSprite:setJustification(0.5, 0.5)
	blockAnchorSprite:addPosition(width / 2, height / 2)

	table.insert(sprites, blockAnchorSprite)
end

function pinwheelBlock.sprite(room, entity)
    local sprites = {}

    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, entity.height or 16
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y

    local centerX, centerY = x + halfWidth, y + halfHeight
    local centerNodeX, centerNodeY = nodeX + halfWidth, nodeY + halfHeight

    addBlockSprites(sprites, entity, blockTex, x, y, width, height)
    addArmSprites(sprites, entity, armTex, x, y, width, height, nodeX, nodeY)
    addAnchorSprite(sprites, entity, anchorTex, x, y, width, height)

    return sprites
end

function pinwheelBlock.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8
    local halfWidth, halfHeight = math.floor(entity.width / 2), math.floor(entity.height / 2)

    local nodes = entity.nodes or {{x = 0, y = 0}}
    local nodeX, nodeY = nodes[1].x, nodes[1].y

    local nodeSprite = drawableSprite.fromTexture(pinwheelBlock.nodeTexture, entity)
    local nodeWidth, nodeHeight = 16, 16

    local mainRectangle = utils.rectangle(x, y, width, height)
    local nodeRectangle = utils.rectangle(nodeX - 4, nodeY - 4, 8, 8)

    return mainRectangle, {nodeRectangle}
end

return pinwheelBlock