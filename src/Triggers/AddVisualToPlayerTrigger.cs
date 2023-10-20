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

    public static void Load()
    {
        knownModifiers = new Dictionary<string, PlayerVisualModifier>();
        Everest.Content.OnUpdate += Content_OnUpdate;
        On.Celeste.Player.UpdateSprite += Player_UpdateSprite;
        On.Monocle.Sprite.Play += Sprite_Play;
        On.Celeste.Player.Render += Player_Render;
        On.Celeste.PlayerHair.Render += PlayerHair_Render;
    }

    private static void Sprite_Play(On.Monocle.Sprite.orig_Play orig, Sprite self, string id, bool restart, bool randomizeFrame)
    {
        if(!(ModifySpritePlay && CommunalHelperModule.Session.visualAddition is { } va && self is PlayerSprite))
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
        if (!(CommunalHelperModule.Session.visualAddition is { } va))
        {
            orig(self);
            return;
        }
        Vector2 v = va.modifiersByAnim.TryGetValue(self.Sprite.CurrentAnimationID, out var pam) && pam.playerOffset.HasValue ? pam.playerOffset.Value : va.defaultPlayerOffset;
        {
            self.Sprite.RenderPosition += v;
        }
        orig(self);
        self.Sprite.RenderPosition -= v;
        if (va.image == null) return;
        v = va.modifiersByAnim.TryGetValue(self.Sprite.CurrentAnimationID, out pam) && pam.imageOffset.HasValue ? pam.imageOffset.Value : va.defaultImageOffset;
        v.X *= (int) self.Facing;
        va.image.Texture.Draw(self.Sprite.RenderPosition + v,  va.image.Origin, va.image.Color, new Vector2((int)self.Facing, 1));
    }

    static void PlayerHair_Render(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self)
    {
        if (!(CommunalHelperModule.Session.visualAddition is { } va))
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

    public static void Unload()
    {
        Everest.Content.OnUpdate -= Content_OnUpdate;
        On.Celeste.Player.UpdateSprite -= Player_UpdateSprite;
        On.Monocle.Sprite.Play -= Sprite_Play;
        On.Celeste.Player.Render -= Player_Render;
        On.Celeste.PlayerHair.Render -= PlayerHair_Render;
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
            PrintPVM(filePath, modifier);
            return true;
        }
        return false;
        
    }

    private static void PrintPVM(string filePath, PlayerVisualModifier modifier)
    {
        Console.WriteLine("PlayerVisualModifier @ " + filePath);
        Console.WriteLine("playerOffset: " + modifier.defaultPlayerOffset);
        Console.Write("image: ");
        if (modifier.image == null) Console.WriteLine("null");
        else if (modifier.image is Sprite sprite) Console.WriteLine("Sprite with path " + sprite.Path ?? "null");
        else Console.WriteLine("Image with texture asset path " + modifier.image.Texture.Metadata.PathVirtual);
        Console.Write("Overrides:");
        if (modifier.modifiersByAnim == null) Console.WriteLine(" none");
        else
        {
            Console.WriteLine();
            foreach (var kvp in modifier.modifiersByAnim)
            {
                Console.WriteLine("Anim: " + kvp.Key);
                if (kvp.Value.@override != null) Console.WriteLine("  ReplaceWith: " + kvp.Value.@override);
                if (kvp.Value.imagePlay != null) Console.WriteLine("  ImagePlays: " + kvp.Value.imagePlay);
                Console.WriteLine("  PlayerOffset: " + kvp.Value.playerOffset?.ToString() ?? "none");
                if (modifier.image != null) Console.WriteLine("  ImageOffset: " + kvp.Value.playerOffset?.ToString() ?? "none");
            }
        }
    }

    private static Vector2 getVectorFromXML(string text)
    {
        string[] pos = text.Split(',');
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
                Console.WriteLine(a.Name + " : " + a.NodeType.ToString());
                XmlElement el = a as XmlElement; // This is pretty naive, but it's fiiine
                switch (a.Name)
                {
                    case "PlayerOffset": modifier.defaultPlayerOffset = getVectorFromXML(el.InnerText); break;
                    case "Image":
                        Console.WriteLine("textureSource: " + el.InnerText);
                        modifier.image = new Image(GFX.Game[el.InnerText]);
                        if(el.Attributes.GetNamedItem("offset") is XmlAttribute x)
                            modifier.defaultImageOffset = getVectorFromXML(x.Value);
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
                            Console.WriteLine(el.Name);
                            sd.Add(el);
                            CommunalHelperGFX.SpriteBank.SpriteData[SPRITEBANKPREF + name] = sd;
                            modifier.image = CommunalHelperGFX.SpriteBank.Create(SPRITEBANKPREF + name);
                            // We're going to add to this SpriteBank because no other mod can mess with it without cursed shit anyways.
                        }
                        if (el.Attributes.GetNamedItem("offset") is XmlAttribute y)
                            modifier.defaultImageOffset = getVectorFromXML(y.Value);
                        break;
                    case "Override":
                        if (modifier.modifiersByAnim == null) modifier.modifiersByAnim = new Dictionary<string, PlayerAnimMod>();
                        PlayerAnimMod m = new PlayerAnimMod();
                        foreach (XmlNode n in a.ChildNodes)
                        {
                            Console.WriteLine(n.Name + ": " + n.InnerText);
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
    private Vector2 defaultPlayerOffset, defaultImageOffset;
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
        string s = data.Attr("modifier");
        if(!string.IsNullOrWhiteSpace(s))
            PlayerVisualModifier.TryGetModifier(data.Attr("modifier"), out pvm);
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        if (RevertOnLeave) prev = CommunalHelperModule.Session.visualAddition;
        CommunalHelperModule.Session.visualAddition = pvm;
    }
    
    public override void OnLeave(Player player)
    {
        base.OnLeave(player);
        if (RevertOnLeave) CommunalHelperModule.Session.visualAddition = prev;
    }
}
