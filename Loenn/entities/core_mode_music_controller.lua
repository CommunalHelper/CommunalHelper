local coreModeMusicController = {}

coreModeMusicController.name = "CommunalHelper/CoreModeMusicController"
coreModeMusicController.depth = -1000000

function coreModeMusicController.texture(room, entity)
    return entity.disable and "objects/CommunalHelper/coreModeMusicController/iconDisable" or "objects/CommunalHelper/coreModeMusicController/iconEnable"
end

coreModeMusicController.placements = {
    {
        name = "enable",
        data = {
            params = "",
            hot = 1.0,
            cold = 0.0,
            none = 0.0,
            disable = false
        }
    },
    {
        name = "disable",
        data = {
            params = "",
            hot = 1.0,
            cold = 0.0,
            none = 0.0,
            disable = true
        }
    }
}

return coreModeMusicController
