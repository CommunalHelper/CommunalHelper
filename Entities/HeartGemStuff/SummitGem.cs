using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/CustomSummitGem")]
    public class CustomSummitGem : SummitGem {

        public new static readonly Color[] GemColors;

        public string CustomGemSID;

        protected Color? particleColor;
        protected DynData<SummitGem> baseData;

        static CustomSummitGem() {
            GemColors = new Color[8];
            Array.Copy(SummitGem.GemColors, GemColors, 6);
            GemColors[6] = Calc.HexToColor("57FFCD");
            GemColors[7] = Calc.HexToColor("E00047");
        }
        
        public CustomSummitGem(EntityData data, Vector2 offset, EntityID gid) 
            : base(data, offset, gid) {
            baseData = new DynData<SummitGem>(this);

            GemID = data.Int("index");

            // Hopefully this always works
            string mapId = AreaData.Get((Engine.Scene as Level)?.Session ?? (Engine.Scene as LevelLoader).Level.Session).SID;
            
            CustomGemSID = $"{mapId}/{data.Level.Name}/{GemID}";

            Sprite sprite = baseData.Get<Sprite>("sprite");
            Remove(sprite);
            if (GFX.Game.Has("collectables/summitgems/" + CustomGemSID + "/gem00")) {
                sprite = new Sprite(GFX.Game, "collectables/summitgems/" + CustomGemSID + "/gem");
            } else {
                sprite = new Sprite(GFX.Game, "collectables/summitgems/" + GemID + "/gem");
            }
            sprite.AddLoop("idle", "", 0.08f);
            sprite.Play("idle");
            sprite.CenterOrigin();
            Add(sprite);

            Remove(baseData.Get<Wiggler>("scaleWiggler"));
            Wiggler scaleWiggler = Wiggler.Create(0.5f, 4f, f => sprite.Scale = Vector2.One * (1f + f * 0.3f));
            Add(scaleWiggler);

            if (CommunalHelperModule.SaveData.SummitGems != null && CommunalHelperModule.SaveData.SummitGems.Contains(CustomGemSID)) {
                sprite.Color = Color.White * 0.5f;
            }

            if (Everest.Content.TryGet<AssetTypeYaml>(GFX.Game.RelativeDataPath + "collectables/summitgems/" + CustomGemSID + "/gem.meta", out ModAsset asset) && 
                asset.TryDeserialize(out ColorMeta meta)) {
                Console.WriteLine("Found meta file");
                particleColor = Calc.HexToColor(meta.Color);
            }

            baseData["sprite"] = sprite;
        }

        private IEnumerator SmashRoutine(Player player, Level level) {
            Visible = false;
            Collidable = false;
            player.Stamina = 110f;
            SoundEmitter.Play(SFX.game_07_gem_get, this, null);

            Session session = (Scene as Level).Session;
            session.DoNotLoad.Add(GID);
            CommunalHelperModule.Session.SummitGems.Add(CustomGemSID);
            CommunalHelperModule.SaveData.RegisterSummitGem(CustomGemSID);

            level.Shake(0.3f);
            Celeste.Freeze(0.1f);
            P_Shatter.Color = particleColor ?? GemColors[GemID];
            float angle = player.Speed.Angle();
            level.ParticlesFG.Emit(P_Shatter, 5, Position, Vector2.One * 4f, angle - Calc.QuarterCircle);
            level.ParticlesFG.Emit(P_Shatter, 5, Position, Vector2.One * 4f, angle + Calc.QuarterCircle);
            SlashFx.Burst(Position, angle);

            for (int i = 0; i < 10; i++) {
                Scene.Add(new AbsorbOrb(Position, player, null));
            }
            level.Flash(Color.White, true);
            Scene.Add((Entity) Activator.CreateInstance(typeof(SummitGem).GetNestedType("BgFlash", BindingFlags.NonPublic)));

            Engine.TimeRate = 0.5f;
            while (Engine.TimeRate < 1f) {
                Engine.TimeRate += Engine.RawDeltaTime * 0.5f;
                yield return null;
            }

            RemoveSelf();
            yield break;
        }

        [Serializable]
        internal class ColorMeta {
            public string Color { get; set; }
        }

        #region Hooks

        internal static void Load() {
            IL.Celeste.SummitGem.OnPlayer += SummitGem_OnPlayer;
        }

        internal static void Unload() {
            IL.Celeste.SummitGem.OnPlayer -= SummitGem_OnPlayer;
        }

        private static void SummitGem_OnPlayer(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(MoveType.Before, instr => instr.MatchCallvirt<SummitGem>("SmashRoutine"));
                
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Isinst, typeof(CustomSummitGem).GetTypeInfo());
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Callvirt, typeof(CustomSummitGem).GetMethod("SmashRoutine", BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

        }

        #endregion

    }
}
