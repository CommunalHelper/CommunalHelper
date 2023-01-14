namespace Celeste.Mod.CommunalHelper;

public static class CommunalHelperGFX
{
    public static SpriteBank SpriteBank { get; set; }

    public static Atlas CloudscapeAtlas { get; private set; }

    internal static void LoadContent()
    {
        SpriteBank = new SpriteBank(GFX.Game, "Graphics/CommunalHelper/Sprites.xml");

        CloudscapeAtlas = Extensions.LoadAtlasFromMod("CommunalHelper:/Graphics/Atlases/CommunalHelper/Cloudscape/atlas", Atlas.AtlasDataFormat.CrunchXml);
    }

    internal static void Load()
    {

    }

    internal static void Unload()
    {
        CloudscapeAtlas.Dispose();
    }
}
