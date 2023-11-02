# CommunalHelper

CommunalHelper is a general-purpose code mod with many new and remixed entities.

A full list of entities and triggers can be found on the [Custom Entity List](https://max480-random-stuff.appspot.com/celeste/custom-entity-catalog#communal-helper).

Features that require additional information or usage guides will be explained here.

:warning: This wiki is updated by a GitHub Action on every release. CHANGES MADE DIRECTLY ON THE WIKI WILL BE OVERWRITTEN. Update the files in the `docs/` directory of the main repo instead.

## Table of Contents
* [Custom Summit Gems](#custom-summit-gems)
* [Player Visual Modifier](#player-visual-modifier)

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

## Player Visual Modifier
The Player Visual Modifier is an abstraction that allows you to append images or modify Sprite animations on the Player, and was originally a port of the Skateboard Visual within the [Strawberry Jam](https://gamebanana.com/mods/424541) map "Forest Rush" by mmm.

It allows you to do any of the following:
- [Add a positional offset to the player visuals](#add-a-positional-offset)
- Add an image or sprite to render on top of the player with an offset (with facing angle preserved)
- Override the behavior of any animation/loop on the player
  - Replace an animation with another animation, e.g. "runCarry" on player could actually play "idle" on Player
  - Change the positional offset to the player visuals for that animation
  - Change the added image/sprite offset for that animation
  - Play an animation from the added sprite, if one exists.

To use it, an XML must first be added to `Graphics/CommunalHelper/PlayerVisualModifiers/`.
The XML should have a unique path, so it's recommended you also specify the modname as a folder, e.g. `Graphics/CommunalHelper/PlayerVisualModifiers/$ModNameHere/$XMLNameHere.xml`.
You can only have one Player Visual Modifier per XML file, and each file must start and end with a PlayerVisualModifier tag, e.g. `<PlayerVisualModifier> ... </PlayerVisualModifier>`. In addition, you do not need an Image or Sprite for the modifications to work.

There is a single documentation file you can read from [here](https://github.com/CommunalHelper/CommunalHelper/blob/dev/docs/PlayerVisualModifierDocumentation.xml) and a working example of the Forest Rush Skateboard [here](https://github.com/CommunalHelper/CommunalHelper/blob/dev/Graphics/CommunalHelper/PlayerVisualModifiers/Skateboard.xml)

### Add a Positional Offset
To add a default positional offset to the player visual, you need to add a PlayerOffset tag.
```xml
<PlayerOffset>0,-3</PlayerOffset>
```
The Inner Content of the Player Offset tag is how many pixels away from the default the player actually renders, and must be in the form `[integer],[integer]`. Note that negative Y means going upward, and positive Y means going downward.

### Add an Image
To add an Image to the player, you need to add an Image tag of the form:
```xml
<Image offset="3,0" justify="0.5,1">[pathFromGameplayAtlas]</Image>
```
The Inner Content of the Image tag should be a string describing the path to an image placed in `Graphics/Atlases/Gameplay/`, e.g. `objects/CommunalHelper/strawberryJam/skateboard`.

The `offset` attribute refers to the distance relative to the bottom middle of the player the Image will be drawn. If you don't add an offset parameter, it will default to the bottom middle of the player. Please note that both of the numbers in offset *must* be integers.

The `justify` attribute refers to the justification of the image, where `0,0` means the topleft of the image is drawn at `offset`, `0.5,0.5` means that the center of the image is drawn there, and `1,1` means that the bottomright is drawn there. For Images specifically, justification is "0,0" by default, and for Sprites, it defaults to the justification of the Sprite.

### Add a Sprite
To add a Sprite, you need to add a Sprite tag. However, there are two different methods to adding a sprite, each of which have its pros and cons.

If you already have a Sprite in `Graphics/Sprites.xml` and you want to use that for the Player Visual Modifier, simply add it with this format:
```xml
<Sprite name="[TagNameFromSprites.xml]", offset="3,0", justify="0.5,1"/>
```
The `name` attribute should be exactly the same as the tag added in `Graphics/Sprites.xml`.
The `offset` and `justify` attributes work the same as adding an Image.

However, if you want to save the visual clutter of having to work with two XML sheets, you can also implement the Sprite directly into this XML, with the following format:
```xml
<Sprite name="MyCustomGranny" path="characters/oldlady/" start="idle" offset="2,2">
    <Justify x=".5" y="1" />

    <Loop id="idle" path="idle" delay="0.15" />
    <Loop id="walk" path="walk" delay="0.1" />
    <Loop id="laugh" path="laugh" delay="0.1" />
    <Anim id="airQuotes" path="quotes" delay="0.16" frames="0-11" goto="pointCane"/>
    <Loop id="pointCane" path="quotes" delay="0.16" frames="12-28" />
</Sprite>
```
This format is identical to the [format for Sprites.xml](https://github.com/EverestAPI/Resources/wiki/Reskinning-Entities#reskinning-entities-through-spritesxml), with the only changes being:
- Instead of naming the tag the Identifier, you use the form `<Sprite name=<IDENTIFIER> ...>`
- An `offset` attribute can be added to the Sprite tag.
In order to use this, you **must** have a unique `name` to both any other existing PlayerVisualModifier **and** `Graphics/Sprites.xml` tag names (that means it is mod-sensitive, so it's ideal you have it in the form of `name="YOURMODNAME_[the name you would have put in normally]"`)

### Overrides
Overrides are the more specific use cases for Player Visual Modifiers, and enable the user to override any animation(s) within the player or various attributes about that animation(s).
Overrides are formed with the Override Tag. You can add multiple Overrides by simply adding multiple Override tags to the same PlayerVisualModifier.
```xml
<Override anim="runSlow,runFast">

</Override>
```
The `anim` attribute should contain a comma-separated list of Animations (or just the Animation if only 1) that are going to be impacted by this Override. These will henceforth be referred to as the "main animation," for ease of reading.

#### Replace an Animation with Another
```xml
<AnimReplace>[replacingAnimation]</AnimReplace>
```
This tag tells the game: `If the Player is about to play the main animation, instead play this replacing animation.` Note that replacement occurs only once per animation call, so if you had a scenario like:
```xml
<Override anim="runFast">
    <AnimReplace>runSlow</AnimReplace>
</Override>
<Override anim="runSlow">
    <AnimReplace>idle</AnimReplace>
</Override>
```
and then `runFast` tried to play, it would be replaced with `runSlow`, *not* `idle`.

#### Change Player Offset
```xml
<PlayerOffset>0,4</PlayerOffset>
```
This tag changes the Visual Offset of the Player for *only* the main animation. Note that the Player Offset will take priority over the Animation Replace, so in the following scenario, the player will be visually offset 0 pixels to the right and 3 pixels upward.
```xml
<Override anim="runFast">
    <AnimReplace>runSlow</AnimReplace>
    <PlayerOffset>0,-3</PlayerOffset>
</Override>
<Override anim="runSlow">
    <PlayerOffset>2,4</PlayerOffset>
</Override>
```

#### Change Image Offset
```xml
<ImageOffset>0,4</ImageOffset>
```
This tag changes the Visual Offset of the for *only* the main animation. Note that the Image Offset will take priority over the Animation Replace, [See here](#change-player-offset) for an example of this scenario.

#### Animate your Sprite
```xml
<AnimSprite>[spriteAnimation]</AnimSprite>
```
If there is a sprite attached to the Player Visual Modifier, the animation `[spriteAnimation]` will play as the main animation plays. In addition, in the event you want the animation to play the same animation ID as the player, you can use `mirror` in the Inner Content to do so.

AnimSprite will take priority over Animation Replace, with the exception of `mirror`, which will mirror the replacing animation instead of the main animation. [See here](#change-player-offset) for an example of this scenario.