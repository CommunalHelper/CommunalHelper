local directions = {"leftToRight", "topToBottom"}

return {
    name = "CommunalHelper/CassetteMusicFadeTrigger",
    fieldInformation = {
        direction = {
            editable = false,
            options = directions
        }
    },
    placements = {
        name = "trigger",
        data = {
            fadeA = 0.0,
            fadeB = 1.0,
            param = "",
            direction = "leftToRight"
        }
    }
}
