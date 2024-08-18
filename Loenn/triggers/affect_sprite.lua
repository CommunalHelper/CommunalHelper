return {
    name = "CommunalHelper/AffectSpriteTrigger",
    fieldInformation = {
        parameter = {
            fieldType = "string",
            options = {"Rate","Rotation","UseRawDeltaTime","Justify","Position","Scale","Color"}
        }
    },
    nodeLimits = {1, 1},
    placements = {
        name = "trigger",
        data = {
            width = 8,
            height = 8,
            player = true,
            parameter = "Rate",
            value = "1"
        }
    }
}