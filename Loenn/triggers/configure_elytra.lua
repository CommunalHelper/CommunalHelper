return {
    name = "CommunalHelper/ConfigureElytraTrigger",
    ignoredFields = {
        "_name", "_id", "_type", "infinite"
    },
    placements = {
        {
            name = "yes",
            data = {
                allow = true,
                infinite = false,
            }
        },
        {
            name = "yes_infinite",
            data = {
                allow = true,
                infinite = true,
            }
        },
        {
            name = "no",
            data = {
                allow = false,
                infinite = false,
            }
        }
    }
}