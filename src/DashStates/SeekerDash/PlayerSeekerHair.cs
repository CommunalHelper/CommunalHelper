using Celeste.Mod.CommunalHelper.Imports;

namespace Celeste.Mod.CommunalHelper.DashStates;

// Not meant to be added on something that isn't Player
public class PlayerSeekerHair : Component
{
    private static MTexture[] seekerHairSegments;
    private static readonly Color[] SeekerHairColors = new[] {
        Calc.HexToColor("2f4c35"),
        Calc.HexToColor("697740"),
    };

    // Couldn't find a better name
    private class Braid
    {
        private static readonly Random random = new();

        private readonly Vector2[] nodes = new Vector2[9];
        private readonly Vector2 direction;

        private float wave = random.NextAngle();
        private float angleSpeed = 1f;

        private readonly float spasmInterval = random.NextFloat(6f) + 4f;

        public Vector2 Scale;

        public Braid(Vector2 direction)
        {
            this.direction = direction;
        }

        public void Simulate(Vector2 from, bool motion)
        {
            nodes[0] = from;

            wave += Engine.DeltaTime * 4f * angleSpeed;
            angleSpeed = Calc.Approach(angleSpeed, 1f, Engine.DeltaTime * 3f);

            if (Engine.Scene.OnInterval(spasmInterval))
                angleSpeed += random.Next(1, 4);

            float angle = (float) Math.Sin(Engine.Scene.TimeActive) * .2f;
            Vector2 dir = new Vector2(direction.X * -Scale.X, direction.Y * Scale.Y).Rotate(angle);
            Vector2 waveDir = dir.Perpendicular() * 1.125f;

            Vector2 target = nodes[0] + dir + (waveDir * (float) Math.Sin(wave));
            Vector2 current = nodes[0];

            const float step = 3;
            const float stepSq = step * step;

            for (int i = 1; i < nodes.Length; i++)
            {
                if (motion)
                {
                    float speed = (1f - ((float) i / nodes.Length * 0.5f)) * 64f;
                    nodes[i] = Calc.Approach(nodes[i], target, speed * Engine.DeltaTime);
                }

                if (Vector2.DistanceSquared(nodes[i], current) > stepSq)
                    nodes[i] = current + ((nodes[i] - current).SafeNormalize() * step);

                target = nodes[i] + dir + (waveDir * (float) Math.Sin(wave + (i * 0.8f)));
                current = nodes[i];
            }
        }

        public void RenderOutline(float threshold)
        {
            threshold *= nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                float segmentScale = Calc.ClampedMap(threshold - i, 0, 1);
                if (segmentScale == 0)
                    continue;
                segmentScale *= GetHairScale(i);
                Vector2 texScale = new(segmentScale, segmentScale * Math.Sign(Scale.Y));

                MTexture hair = seekerHairSegments[i % seekerHairSegments.Length];
                hair.DrawCentered(nodes[i] + Vector2.UnitX, Color.Black, texScale);
                hair.DrawCentered(nodes[i] - Vector2.UnitX, Color.Black, texScale);
                hair.DrawCentered(nodes[i] + Vector2.UnitY, Color.Black, texScale);
                hair.DrawCentered(nodes[i] - Vector2.UnitY, Color.Black, texScale);
            }
        }

        public void Render(float threshold)
        {
            threshold *= nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                float segmentScale = Calc.ClampedMap(threshold - i, 0, 1);
                if (segmentScale == 0)
                    continue;
                segmentScale *= GetHairScale(i);
                Vector2 texScale = new(segmentScale, segmentScale * Math.Sign(Scale.Y));

                float lerp = ((float) i / (nodes.Length - 1)) + (Engine.Scene.TimeActive * 0.75f);
                Color color = Util.ColorArrayLerp(lerp, SeekerHairColors);
                seekerHairSegments[i % seekerHairSegments.Length].DrawCentered(nodes[i], color, texScale);
            }
        }

        private float GetHairScale(int index)
        {
            return 0.25f + ((1f - ((float) index / nodes.Length)) * 0.75f);
        }
    }

    private readonly Braid[] braids = new Braid[] {
        new(Calc.AngleToVector(-.8f, 2f)),
        new(Calc.AngleToVector(-.2f, 2f)),
        new(Calc.AngleToVector( .3f, 2f)),
    };

    private float lerp;

    public PlayerSeekerHair()
        : base(active: true, visible: true) { }

    public override void Update()
    {
        lerp = Calc.Approach(lerp, SeekerDash.HasSeekerDash ? 1 : 0, Engine.DeltaTime * 3f);
        base.Update();
        AfterUpdate();
    }

    // This is required, because we want the hair to update during transitions
    public void AfterUpdate(bool motion = true)
    {
        Player player = Entity as Player;
        PlayerSprite sprite = player.Sprite;

        // GravityHelper only flips the PlayerSprite Y scale during PlayerSprite.Render, so we flip it manually here
        bool inverted = GravityHelper.IsPlayerInverted?.Invoke() ?? false;
        Vector2 scale = new((float) player.Facing, inverted ? -1 : 1);

        Vector2 hairOffset = sprite.HairOffset * scale;
        Vector2 hairPosition = sprite.RenderPosition + new Vector2(0f, -9f * scale.Y * sprite.Scale.Y) + hairOffset;

        foreach (Braid braid in braids)
        {
            braid.Scale = scale;
            braid.Simulate(hairPosition, motion);
        }
    }

    public override void Render()
    {
        foreach (Braid braid in braids)
            braid.RenderOutline(lerp);
        foreach (Braid braid in braids)
            braid.Render(lerp);
    }

    internal static void InitializeTextures()
    {
        seekerHairSegments = GFX.Game.GetAtlasSubtextures("characters/player/CommunalHelper/seekerhair").ToArray();
    }
}
