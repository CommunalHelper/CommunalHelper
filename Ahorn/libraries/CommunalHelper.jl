module CommunalHelper

using ..Ahorn, Maple
using Cairo

# General
export hexToRGBA
# Dream Blocks
export renderDreamBlock
# Cassette Blocks
export renderCassetteBlock, cassetteColorNames, getCassetteColor
# Connected Blocks
export getExtensionRectangles, notAdjacent
# Depths
export depths

"Celeste.Depths"
const depths = Dict{String, Integer}(
        "BGTerrain (10000)" => 10000,
        "BGDecals (9000)" => 9000,
        "BGParticles (8000)" => 8000,
        "Below (2000)" => 2000,
        "NPCs (1000)" => 1000,
        "Player (0)" => 0,
        "Dust (-50)" => -50,
        "Pickups (-100)" => -100,
        "Particles (-8000)" => -8000,
        "Above Particles (-8500)" => -8500,
        "Solids (-9000)" => -9000,
        "FGTerrain (-10000)" => -10000,
        "FGDecals (-10500)" => -10500,
        "DreamBlocks (-11000)" => -11000,
        "CrystalSpinners (-11500)" => -11500,
        "Chaser (-12500)" => -12500,
        "Fake Walls (-13000)" => -13000,
        "FGParticles (-50000)" => -50000
)

"Celeste.SurfaceSound"
const surfaceSounds = Dict{String, Integer}(
   "Asphalt" => 1,
   "Car" => 2,
   "Dirt" => 3,
   "Snow" => 4,
   "Wood" => 5,
   "StoneBridge" => 6,
   "Girder" => 7,
   "Brick" => 8,
   "ZipMover" => 9,
   "DreamBlockInactive" => 11,
   "DreamBlockActive" => 12,
   "ResortWood" => 13,
   "ResortRoof" => 14,
   "ResortSinkingPlatforms" => 15,
   "ResortBasementTile" => 16,
   "ResortLinens" => 17,
   "ResortBoxes" => 18,
   "ResortBooks" => 19,
   "ClutterDoor" => 20,
   "ClutterSwitch" => 21,
   "ResortMagicButton" => 21,
   "ResortElevator" => 22,
   "CliffsideSnow" => 23,
   "CliffsideGrass" => 25,
   "CliffsideWhiteBlock" => 27,
   "Gondola" => 28,
   "AuroraGlass" => 32,
   "Grass" => 33,
   "CassetteBlock" => 35,
   "CoreIce" => 36,
   "CoreMoltenRock" => 37,
   "Glitch" => 40,
   "MoonCafe" => 42,
   "DreamClouds" => 43,
   "Moon" => 44,
)

"""
	 hexToRGBA(hex)

Convert a hex color code to an RGBA tuple with value from 0.0-1.0

# Examples:
    hexToRGBA("ff00ff")
"""
hexToRGBA(hex) = tuple(Ahorn.argb32ToRGBATuple(parse(Int, hex; base=16))[1:3] ./ 255..., 1.0)

"""
	 renderDreamBlock(ctx, x, y, width, height, data)
	
Draw a custom dreamblock based on the attributes present in `data`.

# Supported Attributes:
	`featherMode`::Bool
	`oneUse`::Bool
	`noCollide`::Bool
	`doubleRefill`::Bool OR `refillCount`::Int
"""
function renderDreamBlock(ctx::CairoContext, x::Number, y::Number, width::Number, height::Number, data::Dict{String, Any})
	save(ctx)

	set_antialias(ctx, 1)
	set_line_width(ctx, 1)

	fillColor = get(data, "featherMode", false) ? (0.31, 0.69, 1.0, 0.4) : (0.0, 0.0, 0.0, 0.4)
	#get(data, "noCollide", false)

	lineColor = (1.0, 1.0, 1.0, 1.0)
	if get(data, "doubleRefill", false)
		lineColor = (1.0, 0.43, 0.94, 1.0)
	else
		refillCount = Int(get(data, "refillCount", -1))
		if refillCount != -1
			lineColor = get(hairColors, refillCount + 1, (1.0, 0.43, 0.94, 1.0))
		end
	end

	if (get(data, "oneUse", false))
		set_dash(ctx, [0.6, 0.2])
	end

	Ahorn.drawRectangle(ctx, x, y, width, height, fillColor, lineColor)

	restore(ctx)
end

# Translated from Celeste.UserIO.GetSavePath
function getSavesDir()
	if Sys.islinux() || Sys.isfreebsd() || Sys.isopenbsd() || Sys.isnetbsd()
		envVar = get(ENV, "XDG_DATA_HOME", nothing)
		if !isnothing(envVar) && !isempty(envVar)
			return joinpath(envVar, "Celeste", "Saves")
		end
		envVar = get(ENV, "HOME", nothing)
		if !isnothing(envVar) && !isempty(envVar)
			return joinpath(envVar, ".local", "share", "Celeste", "Saves")
		end
	elseif Sys.isapple()
		envVar = get(ENV, "HOME", nothing)
		if !isnothing(envVar) && !isempty(envVar)
			return joinpath(envVar, "Library", "Application Support", "Celeste", "Saves")
		end
	elseif Sys.iswindows()
		return joinpath(Ahorn.config["celeste_dir"], "Saves")
	end
	throw(ErrorException("Unsupported operating system. (how did you even get celeste working?)"))
end

function getPlayerHairColors()
	colors = [
		(0.27, 0.7, 1.0, 1.0),
		(0.67, 0.2, 0.2, 1.0),
		(1.0, 0.43, 0.94, 1.0)
	]
	try
		path = joinpath(getSavesDir(), "modsettings-MoreDasheline.celeste")
		if isfile(path)
			# can't hecking use YAML.jl because it can't read the hex values properly
			moreDasheline = Dict{String, String}(
				strip(key) => strip(value) for (key, value) in split.(readlines(path), ":")
			) 

			colors = append!(colors, [
				hexToRGBA(moreDasheline["ThreeDashColor"]),
				hexToRGBA(moreDasheline["FourDashColor"]),
				hexToRGBA(moreDasheline["FiveDashColor"])
			])
			println("Using MoreDasheline hair colors for CommunalHelper DreamBlocks.")
			return colors
		end
	catch err
		@warn "Error loading MoreDasheline hair colors:\n$err"
	end
	
	println("MoreDasheline hair colors not loaded, using default hair colors for CommunalHelper DreamBlocks.")

	return colors
end

const hairColors = getPlayerHairColors()


const cassetteBlock = "objects/cassetteblock/solid"
const cassetteColors = Dict{Int, Ahorn.colorTupleType}(
   1 => (240, 73, 190, 255) ./ 255,
	2 => (252, 220, 58, 255) ./ 255,
	3 => (56, 224, 78, 255) ./ 255
)
const defaultCassetteColor = (73, 170, 240, 255) ./ 255

const cassetteColorNames = Dict{String, Int}(
    "Blue" => 0,
    "Rose" => 1,
    "Bright Sun" => 2,
    "Malachite" => 3
)

getCassetteColor(index::Int) = get(cassetteColors, index, defaultCassetteColor)

function renderCassetteBlock(ctx::CairoContext, x, y, width, height, index)
   tileWidth = ceil(Int, width / 8)
   tileHeight = ceil(Int, height / 8)

   color = get(cassetteColors, index, defaultCassetteColor)

   for i in 1:tileWidth, j in 1:tileHeight
		tx = (i == 1) ? 0 : ((i == tileWidth) ? 16 : 8)
      ty = (j == 1) ? 0 : ((j == tileHeight) ? 16 : 8)

      Ahorn.drawImage(ctx, cassetteBlock, x + (i - 1) * 8, y + (j - 1) * 8, tx, ty, 8, 8, tint=color)
    end
end

# Get Rectangles from SolidExtensions present in the room.
function getExtensionRectangles(room::Room)
	entities = filter(e -> e.name == "CommunalHelper/SolidExtension", room.entities)
	rects = []

	for e in entities
		 push!(rects, Ahorn.Rectangle(
			  Int(get(e.data, "x", 0)),
			  Int(get(e.data, "y", 0)),
			  Int(get(e.data, "width", 8)),
			  Int(get(e.data, "height", 8))
		 ))
	end
		 
	return rects
end

"Check for collision with an array of rectangles at specified tile position"
function notAdjacent(x, y, ox, oy, rects)
	rect = Ahorn.Rectangle(x + ox + 4, y + oy + 4, 1, 1)

	for r in rects
		 if Ahorn.checkCollision(r, rect)
			  return false
		 end
	end

	return true
end
notAdjacent(entity::Entity, ox, oy, rects) = notAdjacent(Ahorn.position(entity)..., ox, oy, rects)

function detectMod(mod)
	any(s -> occursin(mod, lowercase(s)), Ahorn.getCelesteModZips()) ||
		any(s -> occursin(mod, lowercase(s)), Ahorn.getCelesteModDirs())
end

end