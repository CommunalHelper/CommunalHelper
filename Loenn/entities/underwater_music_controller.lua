local underwaterMusicController = {}

underwaterMusicController.name = "CommunalHelper/UnderwaterMusicController"
underwaterMusicController.depth = -1000000
underwaterMusicController.texture = "objects/CommunalHelper/underwaterMusicController/icon"

underwaterMusicController.placements = {
    {
        name = "normal",
        data = {
            enable = false,
            dashSFX = false,
        }
    }
}

return underwaterMusicController
