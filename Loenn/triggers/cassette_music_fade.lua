local enums = require("consts.celeste_enums")

return {
    name = "CommunalHelper/CassetteMusicFadeTrigger",
    fieldInformation = {
        direction = {
            editable = false,
            options = enums.music_fade_trigger_directions
        }
    },
    placements = {
        name = "trigger",
        data = {
            fadeA = 0.0,
            fadeB = 1.0,
            parameter = "",
            direction = "leftToRight"
        }
    }
}
