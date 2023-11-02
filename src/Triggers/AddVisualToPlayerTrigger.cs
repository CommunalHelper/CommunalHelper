using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Celeste.Mod.CommunalHelper.Triggers;

// Complete redesign of the "Skateboard Trigger" from Strawberry Jam.
// Instead of hardcoding everything on orig_Update, we set a static variable during orig_Update to tell the game that it's the correct time to modify the Sprite.Play instruction.
// Player Visual Modifier mappings are stored in an XML format, see Example_PlayerVisualModifier.xml for more information.

public class PlayerVisualModifier
{
    private const string STARTPATH = "Graphics/CommunalHelper/PlayerVisualModifiers/";
    private const string SPRITEBANKPREF = "PVI/";

    #region Hooks
    private static bool ModifySpritePlay = false;
    private static Hook SpritePlayHook = null; // This is being added for specifically a future PR for Everest so we dont need to rewrite everything

    public static void Load()
    {
        knownModifiers = new Dictionary<string, PlayerVisualModifier>();
        Everest.Content.OnUpdate += Content_OnUpdate;
        On.Celeste.Player.UpdateSprite += Player_UpdateSprite;
        MethodInfo sprite_Play = typeof(Sprite).GetMethod("orig_Play") ?? typeof(Sprite).GetMethod("Play");
        SpritePlayHook = new Hook(sprite_Play, Sprite_Play);
        On.Celeste.Player.Render += Player_Render;
        On.Celeste.PlayerHair.Render += PlayerHair_Render;
    }

    public static void Unload()
    {
        Everest.Content.OnUpdate -= Content_OnUpdate;
        On.Celeste.Player.UpdateSprite -= Player_UpdateSprite;
        SpritePlayHook?.Dispose();
        SpritePlayHook = null;
        On.Celeste.Player.Render -= Player_Render;
        On.Celeste.PlayerHair.Render -= PlayerHair_Render;
    }

    private static void Sprite_Play(Action<Sprite, string, bool, bool> orig, Sprite self, string id, bool restart, bool randomizeFrame)
    {
        if(!(ModifySpritePlay && CommunalHelperModule.Session.VisualAddition is { } va && self is PlayerSprite))
        {
            orig(self, id, restart, randomizeFrame);
            return;
        }
        if (va.modifiersByAnim.TryGetValue(id, out var pam) && pam.@override != null)
            id = pam.@override;
        orig(self, id, restart, randomizeFrame);
        if (va.image != null && va.image is Sprite sprite && sprite.Has(pam.imagePlay))
        {
            ModifySpritePlay = false;
            sprite.Play(pam.imagePlay == "mirror" ? id : pam.imagePlay, restart, randomizeFrame);
            ModifySpritePlay = true;
        }
    }

    private static void Content_OnUpdate(ModAsset arg1, ModAsset arg2)
    {
        if (arg2 != null && arg2.PathVirtual.StartsWith(STARTPATH))
        {
            if (knownModifiers.ContainsKey(arg2.PathVirtual.Substring(STARTPATH.Length)))
            {
                if (LoadModifier(arg2, out var q))
                {
                    knownModifiers[arg2.PathVirtual.Substring(STARTPATH.Length)] = q;
                }
            }
        }
    }

    private static void Player_UpdateSprite(On.Celeste.Player.orig_UpdateSprite orig, Player self)
    {
        ModifySpritePlay = true;
        orig(self);
        ModifySpritePlay = false;
    }

    private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
    {
        if (!(CommunalHelperModule.Session.VisualAddition is { } va))
        {
            orig(self);
            return;
        }
        Vector2 v = va.modifiersByAnim.TryGetValue(self.Sprite.CurrentAnimationID, out var pam) && pam.playerOffset.HasValue ? pam.playerOffset.Value : va.defaultPlayerOffset;
        self.Sprite.RenderPosition += v;
        orig(self);
        self.Sprite.RenderPosition -= v;
        if (va.image == null) return;
        v = va.modifiersByAnim.TryGetValue(self.Sprite.CurrentAnimationID, out pam) && pam.imageOffset.HasValue ? pam.imageOffset.Value : va.defaultImageOffset;
        v.X *= (int) self.Facing;
        va.image.Texture.Draw(self.Sprite.RenderPosition + v,  va.image.Origin, va.image.Color, new Vector2((int)self.Facing, 1));
    }

    static void PlayerHair_Render(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self)
    {
        if (!(CommunalHelperModule.Session.VisualAddition is { } va))
        {
            orig(self);
            return;
        }
        Vector2 v = va.modifiersByAnim.TryGetValue(self.Sprite.CurrentAnimationID, out var pam) && pam.playerOffset.HasValue ? pam.playerOffset.Value : va.defaultPlayerOffset;
        for (int i = 0; i < self.Nodes.Count; i++)
        {
            self.Nodes[i] += v;
        }
        orig(self);
        for (int i = 0; i < self.Nodes.Count; i++)
        {
            self.Nodes[i] -= v;
        }
    }
    #endregion

    private static Dictionary<string, PlayerVisualModifier> knownModifiers;
    public static bool TryGetModifier(string filePath, out PlayerVisualModifier modifier)
    {
        modifier = null;
        if (knownModifiers?.TryGetValue(filePath, out modifier) ?? false) return true;
        if (!Everest.Content.TryGet<AssetTypeXml>(STARTPATH + filePath, out ModAsset mod))
        {
            Logger.Log("CommunalHelper", "PlayerVisualModifier not found @ \""+STARTPATH + filePath + "\".");
            return false;
        }
        if (LoadModifier(mod, out modifier))
        {
            knownModifiers[filePath] = modifier;
            return true;
        }
        return false;
        
    }

    // Keeping this in for the sake of, we're definitely going to get requests and it'll be useful to just have this for the future i feel like.
    private static void DebugPVM(string filePath, PlayerVisualModifier modifier)
    {
        Logger.Log(LogLevel.Debug, "CommunalHelper", "PlayerVisualModifier @ " + filePath);
        Logger.Log(LogLevel.Debug, "CommunalHelper", "playerOffset: " + modifier.defaultPlayerOffset);
        if (modifier.image == null) Logger.Log(LogLevel.Debug, "CommunalHelper", "image: null");
        else if (modifier.image is Sprite sprite) Logger.Log(LogLevel.Debug, "CommunalHelper", "image: Sprite with path " + sprite.Path ?? "null");
        else Logger.Log(LogLevel.Debug, "CommunalHelper", "image: Image with texture asset path " + modifier.image.Texture.Metadata.PathVirtual);
        if (modifier.modifiersByAnim == null) Logger.Log(LogLevel.Debug, "CommunalHelper", "Overrides: none");
        else
        {
            Logger.Log(LogLevel.Debug, "CommunalHelper", "Overrides:");
            foreach (var kvp in modifier.modifiersByAnim)
            {
                Logger.Log(LogLevel.Debug, "CommunalHelper", "Anim: " + kvp.Key);
                if (kvp.Value.@override != null) Logger.Log(LogLevel.Debug, "CommunalHelper", "  ReplaceWith: " + kvp.Value.@override);
                if (kvp.Value.imagePlay != null) Logger.Log(LogLevel.Debug, "CommunalHelper", "  ImagePlays: " + kvp.Value.imagePlay);
                Logger.Log(LogLevel.Debug, "CommunalHelper", "  PlayerOffset: " + kvp.Value.playerOffset?.ToString() ?? "none");
                if (modifier.image != null) Logger.Log(LogLevel.Debug, "CommunalHelper", "  ImageOffset: " + kvp.Value.playerOffset?.ToString() ?? "none");
            }
        }
    }

    private static Vector2 getVectorFromXML(string text, bool forceFloat = false)
    {
        string[] pos = text.Split(',');
        if(forceFloat)
            return new Vector2(Convert.ToSingle(pos[0]), Convert.ToSingle(pos[1]));
        else
            return new Vector2(Convert.ToInt32(pos[0]), Convert.ToInt32(pos[1]));
    }

    private static bool LoadModifier(ModAsset mod, out PlayerVisualModifier modifier)
    {
        modifier = new PlayerVisualModifier();
        modifier.source = mod;
        using (Stream inStream = mod.Stream)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(inStream);
            XmlNode c = null;
            foreach (XmlNode b in doc.ChildNodes)
            {
                if (b.Name == "PlayerVisualModifier")
                {
                    c = b;
                    break;
                }
            }
            if (c == null) return false;
            foreach(XmlNode a in c.ChildNodes) { 
                XmlElement el = a as XmlElement; // This is pretty naive, but it's fiiine
                switch (a.Name)
                {
                    case "PlayerOffset": modifier.defaultPlayerOffset = getVectorFromXML(el.InnerText); break;
                    case "Image":
                        modifier.image = new Image(GFX.Game[el.InnerText]);
                        if(el.Attributes.GetNamedItem("offset") is XmlAttribute x)
                            modifier.defaultImageOffset = getVectorFromXML(x.Value);
                        if (el.Attributes.GetNamedItem("justify") is XmlAttribute y)
                            modifier.imageJustify = getVectorFromXML(y.Value, true);
                        break;
                    case "Sprite":
                        string name = el.Attr("name");
                        if (GFX.SpriteBank.Has(name))
                        {
                            modifier.image = GFX.SpriteBank.Create(name);
                        }
                        else
                        {
                            SpriteData sd = new SpriteData(GFX.Game);
                            // the *one* time I should use DynData<T> it's a typecast I can't grab without the compiler bitching.
                            DynamicData d1 = DynamicData.For(el);
                            DynamicData d2 = new DynamicData(d1.Get("name")); 
                            d2.Set("name", el.Attr("name")); // This is setting XmlElement.name.name
                            d2.Dispose(); // We don't need to dispose d1 because we may use it again, even though it's unlikely.
                            el.RemoveAttribute("name");
                            sd.Add(el);
                            CommunalHelperGFX.SpriteBank.SpriteData[SPRITEBANKPREF + name] = sd;
                            modifier.image = CommunalHelperGFX.SpriteBank.Create(SPRITEBANKPREF + name);
                            // We're going to add to this SpriteBank because no other mod can mess with it without cursed shit anyways.
                        }
                        if (el.Attributes.GetNamedItem("offset") is XmlAttribute z)
                            modifier.defaultImageOffset = getVectorFromXML(z.Value);
                        if (el.Attributes.GetNamedItem("justify") is XmlAttribute w)
                            modifier.imageJustify = getVectorFromXML(w.Value, true);
                        break;
                    case "Override":
                        if (modifier.modifiersByAnim == null) modifier.modifiersByAnim = new Dictionary<string, PlayerAnimMod>();
                        PlayerAnimMod m = new PlayerAnimMod();
                        foreach (XmlNode n in a.ChildNodes)
                        {
                            switch (n.Name)
                            {
                                case "AnimReplace":
                                    m.@override = n.InnerText;
                                    break;
                                case "PlayerOffset":
                                    m.playerOffset = getVectorFromXML(n.InnerText);
                                    break;
                                case "AnimSprite":
                                    m.imagePlay = n.InnerText;
                                    break;
                                case "VisualOffset":
                                    m.imageOffset = getVectorFromXML(n.InnerText);
                                    break;
                            }
                        }
                        string anim = el.Attr("anim");
                        if (anim.Contains(','))
                        {
                            foreach(string an in anim.Split(','))
                            {
                                modifier.modifiersByAnim[an.Trim()] = m;
                            }
                        }
                        else modifier.modifiersByAnim[anim] = m;
                        break;
                }
            }
        }
        return true;
    }


    private ModAsset source;
    private Image image;
    private Vector2 defaultPlayerOffset, defaultImageOffset, imageJustify;
    private Dictionary<string, PlayerAnimMod> modifiersByAnim;

    private struct PlayerAnimMod
    {
        public string @override;
        public Vector2? playerOffset;
        public string imagePlay;
        public Vector2? imageOffset;

    }
}

[CustomEntity("CommunalHelper/PlayerVisualModTrigger")]
public class AddVisualToPlayerTrigger : Trigger
{
    private bool RevertOnLeave;
    private PlayerVisualModifier pvm;
    private PlayerVisualModifier prev;

    public AddVisualToPlayerTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        RevertOnLeave = data.Bool("revertOnLeave");
        string modifier = data.Attr("modifier");
        if(!string.IsNullOrWhiteSpace(modifier))
            PlayerVisualModifier.TryGetModifier(modifier, out pvm);
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        if (RevertOnLeave) prev = CommunalHelperModule.Session.VisualAddition;
        CommunalHelperModule.Session.VisualAddition = pvm;
    }
    
    public override void OnLeave(Player player)
    {
        base.OnLeave(player);
        if (RevertOnLeave) CommunalHelperModule.Session.VisualAddition = prev;
    }
}
