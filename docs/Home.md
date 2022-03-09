# CommunalHelper

CommunalHelper is a general-purpose code mod with many new and remixed entities.

A full list of entities and triggers can be found on the [Custom Entity List](https://max480-random-stuff.appspot.com/celeste/custom-entity-catalog#communal-helper).

Features that require additional information or usage guides will be explained here.

## Table of Contents
* [Custom Summit Gems](#custom-summit-gems)

## Custom Summit Gems

[[Summit Gem Manager](#custom-summit-gem-manager)] | [[Custom Textures](#custom-summit-gems-textures)]

**Custom Summit Gems** are identified by their unique ID, which is composed of the Map ID, Level Name, and the Gem's `index` attribute.

For example, given a map with the path `MyMod/Maps/MyName/MyMod/MyMap.bin`, a Summit Gem placed in the level `a_1` with an index of `4` will have the ID `MyName/MyMod/MyMap/a_1/4`.


### Custom Summit Gem Manager
The **Summit Gem Manager** takes a list of Summit Gems, checks that they have all been collected<sup><a id="custom-summit-gem-manager-footer-1-ref" href="#custom-summit-gem-manager-footer-1">1</a></sup>, and then reveals a HeartGem<sup><a id="custom-summit-gem-manager-footer-2-ref" href="#custom-summit-gem-manager-footer-2">2</a></sup> that was placed in the room.

Each node added to the Summit Gem Manager is associated with a gem ID, which are provided as a *comma separated list* to the `Gem IDs` attribute.  
The gem IDs supplied can be absolute, using the entire ID, or relative within the current map using only `$Level_Name/$Gem_Index`

During the heart routine, as each Summit Gem breaks a tone is played, which is based on the `index` of the associated gem, from `0-7`. This can be customized by providing a sequence of *non-comma separated* numbers in this range to the `melody` attribute.

<sup>
<a id="custom-summit-gem-manager-footer-1" href="#custom-summit-gem-manager-footer-1-ref">1</a>
Due to Sessions not persisting between maps, any gems referenced from other maps will only ever need to be collected once, as opposed to gems in the current map, which must all be collected in the same session for every completion except the first.
</sup>

<sup>
<a id="custom-summit-gem-manager-footer-2" href="#custom-summit-gem-manager-footer-2-ref">2</a>
Confirmed to support Vanilla and AdventureHelper Hearts, and CollabUtils MiniHearts.
</sup>

<a id="custom-summit-gems-textures"></a>
### Custom Textures
:information_source: CommunalHelper adds two new default textures for summit gems, for indices `6` and `7`.

Custom textures can be supplied for any Summit Gem by putting them in `Graphics/Atlases/Gameplay/collectables/summitgems/$GemID` (reminder that `$GemID` is `$MapID/$Level_Name/$Gem_Index`).  
Recoloring the particles used by the gem can be done by adding a `gem.meta.yaml` file in the same folder with a [hex color code](https://www.color-hex.com/):
```yaml
Color: #c5b5d4
```