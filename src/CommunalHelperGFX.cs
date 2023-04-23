using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.CommunalHelper;

public static class CommunalHelperGFX
{
    public static SpriteBank SpriteBank { get; set; }

    public static Atlas CloudscapeAtlas { get; private set; }
    public static Effect CloudscapeShader { get; private set; }


    internal static void LoadContent()
    {
        SpriteBank = new SpriteBank(GFX.Game, "Graphics/CommunalHelper/Sprites.xml");

        CloudscapeAtlas = Extensions.LoadAtlasFromMod("CommunalHelper:/Graphics/Atlases/CommunalHelper/Cloudscape/atlas", Atlas.AtlasDataFormat.CrunchXml);
        CloudscapeShader = LoadShader("cloudscape");
    }

    internal static void Load()
    {

    }

    internal static void Unload()
    {
        CloudscapeAtlas.Dispose();
        CloudscapeShader.Dispose();
    }

    private static Effect LoadShader(string id)
        => new(Engine.Graphics.GraphicsDevice, Everest.Content.Get($"CommunalHelper:/Effects/CommunalHelper/{id}.cso").Data);
}
