using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
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

    private const int SCREEN_WIDTH = 320;
    private const int SCREEN_HEIGHT = 180;
    private static readonly Dictionary<int, Tuple<RenderTarget2D, RenderTarget2D, RenderTarget2D, RenderTarget2D>> mrtBuffers = new();

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
    }

    public static void QueryMRTBuffers(int rendererDepth, out RenderTarget2D albedo, out RenderTarget2D depth, out RenderTarget2D normal, out RenderTarget2D final)
    {
        if (!mrtBuffers.TryGetValue(rendererDepth, out var buffers))
        {
            buffers = Tuple.Create(
                new RenderTarget2D(Engine.Graphics.GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8),
                new RenderTarget2D(Engine.Graphics.GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8),
                new RenderTarget2D(Engine.Graphics.GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8),
                new RenderTarget2D(Engine.Graphics.GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8)
            );
            mrtBuffers.Add(rendererDepth, buffers);
            Logger.Log(LogLevel.Info, nameof(Shape3DRenderer), $"new PCTN-MRT buffer quadruplet created, at depth {rendererDepth}.");
        }

        albedo = buffers.Item1;
        depth = buffers.Item2;
        normal = buffers.Item3;
        final = buffers.Item4;
    }

    private static Effect LoadShader(string id)
        => new(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"CommunalHelper:/Effects/CommunalHelper/{id}.cso").Data);

    private static readonly FieldInfo f_PlayerSprite_FrameMetadata
        = typeof(PlayerSprite).GetField("FrameMetadata", BindingFlags.Static | BindingFlags.NonPublic);

    private static void Mod_PlayerSprite_CreateFramesMetadata(On.Celeste.PlayerSprite.orig_CreateFramesMetadata orig, string sprite)
    {
        orig(sprite);

        Dictionary<string, PlayerAnimMetadata> frameMetadata = (Dictionary<string, PlayerAnimMetadata>) f_PlayerSprite_FrameMetadata.GetValue(null);
        foreach (XmlElement element in CustomPlayerFrameMetadata.GetElementsByTagName("Frames"))
        {
            string path = element.Attr("path", "");
            path = $"characters/{sprite}/{path}";

            string[] hairData = element.Attr("hair").Split('|');
            string[] carryData = element.Attr("carry", "").Split(',');

            for (int i = 0; i < Math.Max(hairData.Length, carryData.Length); i++)
            {
                PlayerAnimMetadata playerAnimMetadata = new();

                string str = path + ((i < 10) ? "0" : string.Empty) + i;
                if (i == 0 && !GFX.Game.Has(str))
                    str = path;

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
