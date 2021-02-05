using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/CustomSummitGemManager")]
    public class CustomSummitGemManager : Entity {

        public static readonly string[] UnlockEventLookup;

        private List<Gem> gems;
        private Vector2? heartOffset;
        private int[] melody;

        static CustomSummitGemManager() {
            UnlockEventLookup = new string[] {
                "1", "2", "3", "4", "5",
                CustomSFX.game_usableSummitGems_gem_unlock_la,
                CustomSFX.game_usableSummitGems_gem_unlock_ti,
                SFX.game_07_gem_unlock_6
            };
            for (int i = 0; i < 5; i++)
                UnlockEventLookup[i] = "event:/game/07_summit/gem_unlock_" + UnlockEventLookup[i];
        }

        public CustomSummitGemManager(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Depth = -10010;

            // Hopefully this always works
            string mapId = AreaData.Get((Engine.Scene as Level)?.Session ?? (Engine.Scene as LevelLoader).Level.Session).SID;

            // big ol' one liner
            heartOffset = Array.ConvertAll(data.Attr("heartOffset").Split(','), str => float.Parse(str)).ToVector2();
            /*
            if (data.Attr("melody").Contains(','))
                melody = Array.ConvertAll(data.Attr("melody").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries), str => float.Parse(str));
            */
            melody = Array.ConvertAll(data.Attr("melody").ToCharArray(), chr => (chr - '0'));

            gems = new List<Gem>();
            string[] ids = data.Attr("gemIds").Split(',');
            if (ids.Length < data.Nodes.Length)
                throw new IndexOutOfRangeException("The number of supplied SummitGemManager IDs needs to match the number of nodes!");
            int idx = 0;
            foreach (Vector2 position in data.NodesOffset(offset)) {
                if (ids[idx].Count(s => s == '/') < 2)
                    ids[idx] = mapId + '/' + ids[idx];
                Gem item = new Gem(ids[idx], position);
                gems.Add(item); 
                idx++;
            }
            Add(new Coroutine(Routine(), true));
        }

        public override void Awake(Scene scene) {
            foreach (Gem entity in gems) {
                scene.Add(entity);
            }
            base.Awake(scene);
        }

        private IEnumerator Routine() {
            Level level = Scene as Level;
            if (level.Session.HeartGem) {
                foreach (Gem gem in gems) {
                    gem.Sprite.RemoveSelf();
                }
                gems.Clear();
                yield break;
            }

            Player entity = Scene.Tracker.GetEntity<Player>();
            while (entity == null || !((entity.Position - Position).Length() < 64f)) {
                yield return null;
            }
            yield return 0.5f;

            bool alreadyHasHeart = level.Session.OldStats.Modes[0].HeartGem;
            int broken = 0;
            int index = 0;
            foreach (Gem gem in gems) {
                bool flag = CommunalHelperModule.Session.SummitGems.Contains(gem.ID);
                if (!alreadyHasHeart) {
                    flag |= (CommunalHelperModule.SaveData.SummitGems != null && CommunalHelperModule.SaveData.SummitGems.Contains(gem.ID));
                }
                if (flag) {
                    Audio.Play(UnlockEventLookup[(melody != null && melody.Length > index) ? melody[index] : gem.Index], gem.Position);
                    /*
                     * TODO: implement this I guess
                    float note = melody[index];
                    double remainder = note - Math.Truncate(note);
                    if (remainder != 0) {
                        Logger.Log("CommunalHelper", "Begin logging for experimental summitgem tones...");
                        Console.WriteLine("Retrieving ChannelGroup...");
                        RESULT result = instance.getChannelGroup(out ChannelGroup group);
                        while (result != RESULT.OK) {
                            yield return null;
                            result = instance.getChannelGroup(out group);
                        }

                        Console.Write("Getting Number of Groups...\t");
                        Console.WriteLine(group.getNumGroups(out int num));
                        Console.WriteLine("Number of Groups: " + num);
                        Console.Write("Getting Inner Group...\t");
                        Console.WriteLine(group.getGroup(0, out group));

                        Console.Write("Getting Number of Channels...\t");
                        Console.WriteLine(group.getNumChannels(out int numChannels));
                        Console.WriteLine("Number of Channels: " + numChannels);
                        Console.Write("Getting Channel...\t");
                        Console.WriteLine(group.getChannel(0, out Channel channel));

                        Console.Write("Getting Frequency...\t");
                        Console.WriteLine(channel.getFrequency(out float freq));
                        Console.WriteLine("Channel Frequency: " + freq);
                        Console.WriteLine("Setting Frequency to " + freq * (float) Math.Pow(2, remainder * 100f / 1200f));
                        Console.WriteLine(channel.setFrequency(freq * (float) Math.Pow(2, remainder * 100f / 1200f)));
                        Logger.Log("CommunalHelper", "End logging for experimental summitgem tones.");
                    }
                    */
                    gem.Sprite.Play("spin");
                    while (gem.Sprite.CurrentAnimationID == "spin") {
                        gem.Bloom.Alpha = Calc.Approach(gem.Bloom.Alpha, 1f, Engine.DeltaTime * 3f);
                        if (gem.Bloom.Alpha > 0.5f) {

                            gem.Shake = Calc.Random.ShakeVector();
                        }
                        gem.Sprite.Y -= Engine.DeltaTime * 8f;
                        gem.Sprite.Scale = Vector2.One * (1f + gem.Bloom.Alpha * 0.1f);
                        yield return null;
                    }

                    yield return 0.2f;

                    level.Shake();
                    Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
                    for (int i = 0; i < 20; i++) {
                        level.ParticlesFG.Emit(SummitGem.P_Shatter, gem.Position + new Vector2(Calc.Random.Range(-8, 8), Calc.Random.Range(-8, 8)), gem.ParticleColor, Calc.Random.NextFloat((float) Math.PI * 2f));
                    }

                    broken++;
                    gem.Bloom.RemoveSelf();
                    gem.Sprite.RemoveSelf();
                    yield return 0.25f;
                }
                index++;
            }

            if (broken < index)
                yield break;

            Entity heart = Scene.Entities.FindFirst<HeartGem>() ?? Scene.Entities.FindFirst_MiniHeart();
            if (heart != null) {
                Audio.Play(SFX.game_07_gem_unlock_complete, heart.Position);
                yield return 0.1f;
            } else
                yield break;

            Vector2 from = heart.Position;
            Vector2 offset = heartOffset ?? Vector2.Zero;
            float p = 0f;
            while (p < 1f && heart.Scene != null) {
                heart.Position = Vector2.Lerp(from, Position + offset, Ease.CubeOut(p));
                yield return null;

                p += Engine.DeltaTime;
            }
        }

        public override void DebugRender(Camera camera) {
            Draw.Circle(Position, 64f, Color.BlueViolet, 16);
            Draw.Line(Position + new Vector2(2f, 0), Position + new Vector2(64f, 0), Color.BlueViolet);
            Draw.HollowRect(Position.X - 2f, Position.Y - 2f, 4f, 4f, Color.BlueViolet);
        }

        private class Gem : Entity {
            public string ID;
            public int Index;
            public Color ParticleColor;
            public Vector2 Shake;
            public Sprite Sprite;
            public Image Bg;
            public BloomPoint Bloom;

            public Gem(string id, Vector2 position) 
                : base(position) {
                ID = id;
                Index = Calc.Clamp(int.Parse(id.Substring(id.LastIndexOf('/') + 1)), 0, 7);
                Depth = -10010;

                // Will probably be implemented as Decals
                //Add(Bg = new Image(GFX.Game["collectables/summitgems/" + id + "/bg"]));
                //Bg.CenterOrigin();

                if (GFX.Game.Has("collectables/summitgems/" + id + "/gem00")) {
                    Add(Sprite = new Sprite(GFX.Game, "collectables/summitgems/" + id + "/gem"));
                } else {
                    Add(Sprite = new Sprite(GFX.Game, "collectables/summitgems/" + Index + "/gem"));
                }
                Sprite.AddLoop("idle", "", 0.05f, 1);
                Sprite.Add("spin", "", 0.05f, "idle");
                Sprite.Play("idle");
                Sprite.CenterOrigin();

                Add(Bloom = new BloomPoint(0f, 20f));

                if (Everest.Content.TryGet(GFX.Game.RelativeDataPath + "collectables/summitgems/" + id + "/gem.meta", out ModAsset asset) &&
                    asset.TryDeserialize(out CustomSummitGem.ColorMeta meta)) {
                    ParticleColor = Calc.HexToColor(meta.Color);
                } else
                    ParticleColor = CustomSummitGem.GemColors[Index];
            }

            public override void Update() {
                Bloom.Position = Sprite.Position;
                base.Update();
            }

            public override void Render() {
                Vector2 position = Sprite.Position;
                Sprite.Position += Shake;
                base.Render();
                Sprite.Position = position;
            }
        }
    }
}
