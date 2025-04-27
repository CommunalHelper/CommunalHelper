using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace Celeste.Mod.CommunalHelper;

public static class CommunalHelperGFX
{
    public static SpriteBank SpriteBank { get; set; }

    public static Atlas CloudscapeAtlas { get; private set; }
    public static Effect CloudscapeShader { get; private set; }

    public static XmlElement CustomPlayerFrameMetadata { get; private set; }

    public static Texture2D Blank { get; private set; }

    public static Effect PCTN_MRT { get; private set; }
    public static Effect PCTN_COMPOSE { get; private set; }

    public static int GameplayBufferWidth => GameplayBuffers.Gameplay?.Width ?? 320;
    public static int GameplayBufferHeight => GameplayBuffers.Gameplay?.Height ?? 180;

    private static readonly Dictionary<int, Tuple<RenderTarget2D, RenderTarget2D, RenderTarget2D, RenderTarget2D>> mrtBuffers = new();
    private static readonly Dictionary<int, RenderTarget2D> dreamSpriteBuffers = new();

    internal static void LoadContent()
    {
        SpriteBank = new SpriteBank(GFX.Game, "Graphics/CommunalHelper/Sprites.xml");

        CloudscapeAtlas = Extensions.LoadAtlasFromMod("CommunalHelper:/Graphics/Atlases/CommunalHelper/Cloudscape/atlas", Atlas.AtlasDataFormat.CrunchXml);
        CloudscapeShader = LoadShader("cloudscape");

        CustomPlayerFrameMetadata = Everest.Content.Map["CommunalHelper:/Graphics/CommunalHelper/CustomPlayerFrameMetadata"].LoadXML()["Metadata"];

        Blank = new Texture2D(Engine.Graphics.GraphicsDevice, 1, 1);
        Blank.SetData(new Color[] { Color.White });

        PCTN_MRT = LoadShader("3d_pctn_mrt");
        PCTN_COMPOSE = LoadShader("3d_pctn_compose");
    }

    internal static void Load()
    {
        On.Celeste.PlayerSprite.CreateFramesMetadata += Mod_PlayerSprite_CreateFramesMetadata;
    }

    internal static void Unload()
    {
        On.Celeste.PlayerSprite.CreateFramesMetadata -= Mod_PlayerSprite_CreateFramesMetadata;

        CloudscapeAtlas.Dispose();
        CloudscapeShader.Dispose();

        CustomPlayerFrameMetadata = null;

        foreach (var buffers in mrtBuffers.Values)
        {
            buffers.Item1.Dispose();
            buffers.Item2.Dispose();
            buffers.Item3.Dispose();
            buffers.Item4.Dispose();
        }
        Util.Log(LogLevel.Info, "destroyed all PCTN-MRT buffer quadruplets.");

        foreach (var buffer in dreamSpriteBuffers.Values)
        {
            buffer.Dispose();
        }
        Util.Log(LogLevel.Info, "destroyed all dream sprite buffers.");
    }

    public static void QueryMRTBuffers(int rendererDepth, out RenderTarget2D albedo, out RenderTarget2D depth, out RenderTarget2D normal, out RenderTarget2D final)
    {
        int gameplayWidth = GameplayBufferWidth, gameplayHeight = GameplayBufferHeight;
        // create the buffers if either none exist for the requested depth, or if the dimensions of the existing buffers no longer match the gameplay buffer
        if (!mrtBuffers.TryGetValue(rendererDepth, out var buffers) || buffers.Item1.Width != gameplayWidth || buffers.Item1.Height != gameplayHeight)
        {
            // make sure any previous buffers are disposed if they exist
            if (buffers is not null)
            {
                buffers.Item1.Dispose();
                buffers.Item2.Dispose();
                buffers.Item3.Dispose();
                buffers.Item4.Dispose();
            }

            buffers = Tuple.Create(
                new RenderTarget2D(Engine.Graphics.GraphicsDevice, gameplayWidth, gameplayHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8),
                new RenderTarget2D(Engine.Graphics.GraphicsDevice, gameplayWidth, gameplayHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8),
                new RenderTarget2D(Engine.Graphics.GraphicsDevice, gameplayWidth, gameplayHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8),
                new RenderTarget2D(Engine.Graphics.GraphicsDevice, gameplayWidth, gameplayHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8)
            );
            mrtBuffers[rendererDepth] = buffers;
            Logger.Log(LogLevel.Info, nameof(Shape3DRenderer), $"new PCTN-MRT buffer quadruplet created, at depth {rendererDepth}.");
        }

        albedo = buffers.Item1;
        depth = buffers.Item2;
        normal = buffers.Item3;
        final = buffers.Item4;
    }

    public static void QueryDreamSpriteBuffers(int rendererDepth, out RenderTarget2D dreamSpriteBuffer)
    {
        int gameplayWidth = GameplayBufferWidth, gameplayHeight = GameplayBufferHeight;
        if (!dreamSpriteBuffers.TryGetValue(rendererDepth, out var buffer) || buffer.Width !=  gameplayWidth || buffer.Height != gameplayHeight)
        {
            buffer?.Dispose();
            buffer = new RenderTarget2D(Engine.Graphics.GraphicsDevice, gameplayWidth, gameplayHeight, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            dreamSpriteBuffers[rendererDepth] = buffer;
            Logger.Log(LogLevel.Info, nameof(DreamSpriteRenderer), $"new dream sprite buffer created, at depth {rendererDepth}.");
        }

        dreamSpriteBuffer = buffer;
    }

    private static Effect LoadShader(string id)
        => new(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"CommunalHelper:/Effects/CommunalHelper/{id}.cso").Data);

    private static readonly FieldInfo f_PlayerSprite_FrameMetadata
        = typeof(PlayerSprite).GetField("FrameMetadata", BindingFlags.Static | BindingFlags.NonPublic);

    private static void Mod_PlayerSprite_CreateFramesMetadata(On.Celeste.PlayerSprite.orig_CreateFramesMetadata orig, string sprite)
    {
        orig(sprite);
        SpriteData data = GFX.SpriteBank.SpriteData[sprite];

        Dictionary<string, PlayerAnimMetadata> frameMetadata = (Dictionary<string, PlayerAnimMetadata>) f_PlayerSprite_FrameMetadata.GetValue(null);
        foreach (XmlElement element in CustomPlayerFrameMetadata.GetElementsByTagName("Frames"))
        {
            string path = !string.IsNullOrEmpty(data.Sources[0].OverridePath) ? data.Sources[0].OverridePath : data.Sources[0].Path;

            if (!(GFX.Game.HasAtlasSubtextures($"{path}{element.Attr("path", "")}")))
                path = "characters/player_no_backpack/";

            path = $"{path}{element.Attr("path", "")}";

            string[] hairData = element.Attr("hair").Split('|');
            string[] carryData = element.Attr("carry", "").Split(',');

            for (int i = 0; i < Math.Max(hairData.Length, carryData.Length); i++)
            {
                string str = path + ((i < 10) ? "0" : string.Empty) + i;
                if (i == 0 && !GFX.Game.Has(str))
                    str = path;

                if (!frameMetadata.ContainsKey(str))
                {
                    PlayerAnimMetadata playerAnimMetadata = new();
                    frameMetadata[str] = playerAnimMetadata;

                    if (i < hairData.Length)
                    {
                        if (hairData[i].Equals("x", StringComparison.OrdinalIgnoreCase) || hairData[i].Length <= 0)
                            playerAnimMetadata.HasHair = false;
                        else
                        {
                            string[] frames = hairData[i].Split(':'); // (:frame)
                            string[] values = frames[0].Split(','); // (x,y)

                            playerAnimMetadata.HasHair = true;
                            playerAnimMetadata.HairOffset = new Vector2(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
                            playerAnimMetadata.Frame = (frames.Length >= 2)
                                ? Convert.ToInt32(frames[1])
                                : 0;
                        }
                    }

                    if (i < carryData.Length && carryData[i].Length > 0)
                        playerAnimMetadata.CarryYOffset = int.Parse(carryData[i]);
                }
            }
        }
    }
}
