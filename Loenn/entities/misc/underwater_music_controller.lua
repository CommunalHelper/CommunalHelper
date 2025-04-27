local underwaterMusicController = {}

underwaterMusicController.name = "CommunalHelper/UnderwaterMusicController"
underwaterMusicController.depth = -1000000
underwaterMusicController.texture = "objects/CommunalHelper/underwaterMusicController/icon"

underwaterMusicController.placements = {
    {
        name = "controller",
        data = {
            enable = false,
            dashSFX = false,
            flag = "",
        }
    }
}

return underwaterMusicController
