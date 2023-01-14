local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")

local playerSeekerBarrier = {}

playerSeekerBarrier.name = "CommunalHelper/PlayerSeekerBarrier"
playerSeekerBarrier.depth = 0
playerSeekerBarrier.minimumSize = {8, 8}

playerSeekerBarrier.placements = {
    {
        name = "normal",
        data = {
            width = 8,
            height = 8,
            spikeUp = false,
            spikeDown = false,
            spikeLeft = false,
            spikeRight = false
        }
    },
    {
        name = "spike_up",
        data = {
            width = 8,
            height = 8,
            spikeUp = true,
            spikeDown = false,
            spikeLeft = false,
            spikeRight = false
        }
    },
    {
        name = "spike_down",
        data = {
            width = 8,
            height = 8,
            spikeUp = false,
            spikeDown = true,
            spikeLeft = false,
            spikeRight = false
        }
    },
    {
        name = "spike_left",
        data = {
            width = 8,
            height = 8,
            spikeUp = false,
            spikeDown = false,
            spikeLeft = true,
            spikeRight = false
        }
    },
    {
        name = "spike_right",
        data = {
            width = 8,
            height = 8,
            spikeUp = false,
            spikeDown = false,
            spikeLeft = false,
            spikeRight = true
        }
    },
    {
        name = "spike_all",
        data = {
            width = 8,
            height = 8,
            spikeUp = true,
            spikeDown = true,
            spikeLeft = true,
            spikeRight = true
        }
    }
}

local color = {0.36, 0.36, 0.36, 0.8}
local spikeTexture = "objects/CommunalHelper/playerSeekerBarrier/spike"

local function bounce(x, p)
    return 1 - (2 * math.abs((x % 1) - 0.5) ^ p)
end

function playerSeekerBarrier.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 8, entity.height or 8

    local sprites = {
        drawableRectangle.fromRectangle("fill", x, y, width, height, color)
    }

    -- deprecated "spiky" setting, might still be set on older maps
    -- it used to add spikes on all edges of the block
    local allSpikes = entity.spiky
    local up = entity.spikeUp or allSpikes
    local down = entity.spikeDown or allSpikes
    local left = entity.spikeLeft or allSpikes
    local right = entity.spikeRight or allSpikes

    if up or down then
        local w = math.floor(width / 8)
        for i = 0, w - 1 do
            local scale = w == 1 and 1 or bounce(i / (w - 1), 2)
            if up then
                local sprite = drawableSprite.fromTexture(spikeTexture, entity)
                sprite:addPosition(i * 8, 0)
                sprite:setJustification(0, 1)
                sprite:setScale(1, scale)
                sprite.color = color
                table.insert(sprites, sprite)
            end
            if down then
                local sprite = drawableSprite.fromTexture(spikeTexture, entity)
                sprite:addPosition(i * 8, height)
                sprite:setJustification(1, 1)
                sprite:setScale(1, scale)
                sprite.rotation = math.pi
                sprite.color = color
                table.insert(sprites, sprite)
            end
        end
    end

    if left or right then
        local h = math.floor(height / 8)
        for i = 0, h - 1 do
            local scale = h == 1 and 1 or bounce(i / (h - 1), 2.25)
            if left then
                local sprite = drawableSprite.fromTexture(spikeTexture, entity)
                sprite:addPosition(0, i * 8)
                sprite:setJustification(1, 1)
                sprite:setScale(1, scale)
                sprite.rotation = -math.pi / 2
                sprite.color = color
                table.insert(sprites, sprite)
            end
            if right then
                local sprite = drawableSprite.fromTexture(spikeTexture, entity)
                sprite:addPosition(width, i * 8)
                sprite:setJustification(0, 1)
                sprite:setScale(1, scale)
                sprite.rotation = math.pi / 2
                sprite.color = color
                table.insert(sprites, sprite)
            end
        end
    end

    return sprites
end

return playerSeekerBarrier
