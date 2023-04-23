using FMOD.Studio;
using System.Linq;
using System.Security.Cryptography;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/Chain")]
public class Chain : Entity
{
    private struct ChainNode
    {
        public Vector2 Position, Velocity, Acceleration;

        public void UpdateStep()
        {
            Velocity += Acceleration * Engine.DeltaTime;
            Position += Velocity * Engine.DeltaTime;
            Velocity *= 1 - Engine.DeltaTime;
            Acceleration = Vector2.Zero;
        }

        public void ConstraintTo(Vector2 to, float distance, bool cancelAcceleration)
        {
            if (Vector2.Distance(to, Position) > distance)
            {
                Vector2 from = Position;
                Vector2 dir = from - to;
                dir.Normalize();
                Position = to + (dir * distance);
                if (!cancelAcceleration)
                {
                    Vector2 accel = Position - from;
                    accel.X = Calc.Clamp(accel.X, -2f, 2f);
                    accel.Y = Calc.Clamp(accel.Y, -2f, 2f);
                    Acceleration += accel * 210f;
                }
            }
        }
    }

    public static MTexture DefaultChain { get; private set; }
    public const string DEFAULT_CHAIN_PATH = "objects/CommunalHelper/chains/chain";

    private readonly bool outline;

    private readonly ChainNode[] nodes;
    private Func<Vector2> attachedStartGetter, attachedEndGetter;

    private readonly float distanceConstraint;

    public bool Tight;

    private readonly EventInstance sfx;
    private Vector2 sfxPos;

    private readonly MTexture texture;
    private readonly MTexture segment;

    public Chain(EntityData data, Vector2 offset)
        : this(
            data.Bool("outline", true),
            (int) ((Vector2.Distance(data.Position + offset, data.NodesOffset(offset)[0]) / 8) + 1 + data.Int("extraJoints")),
            8,
            () => data.Position + offset,
            () => data.Nodes[0] + offset,
            GFX.Game.GetOrDefault(data.Attr("texture", DEFAULT_CHAIN_PATH), DefaultChain)
        ) { }

    public Chain(bool outline, int nodeCount, float distanceConstraint, Func<Vector2> attachedStartGetter, Func<Vector2> attachedEndGetter, MTexture texture)
        : base(attachedStartGetter())
    {
        this.texture = texture;
        segment = texture.GetSubtexture(0, 8, 8, 8);

        nodes = new ChainNode[nodeCount];
        this.attachedStartGetter = attachedStartGetter;
        this.attachedEndGetter = attachedEndGetter;
        this.distanceConstraint = distanceConstraint;

        this.outline = outline;

        Vector2 from = attachedStartGetter != null ? attachedStartGetter() : Position;
        Vector2 to = attachedEndGetter != null ? attachedEndGetter() : Position;
        for (int i = 0; i < nodes.Length; i++)
        {
            Vector2 newPos = from + ((to - from) * i / (nodes.Length - 1));
            nodes[i].Position = newPos;
        }

        UpdateChain();

        sfx = Audio.Play(CustomSFX.game_chain_move);
    }

    private void AttachedEndsToSolids(Scene scene)
    {
        Vector2 start = nodes[0].Position;
        Vector2 end = nodes[nodes.Length - 1].Position;

        Solid startSolid = scene.CollideFirst<Solid>(new Rectangle((int) start.X - 2, (int) start.Y - 2, 4, 4));
        Solid endSolid = scene.CollideFirst<Solid>(new Rectangle((int) end.X - 2, (int) end.Y - 2, 4, 4));

        if (startSolid != null)
        {
            Vector2 offset = start - startSolid.Position;
            attachedStartGetter = () => startSolid.Position + offset;
        }
        else
            attachedStartGetter = null;

        if (endSolid != null)
        {
            Vector2 offset = end - endSolid.Position;
            attachedEndGetter = () => endSolid.Position + offset;
        }
        else
            attachedEndGetter = null;

        if (attachedStartGetter == null && attachedEndGetter == null)
            RemoveSelf();
    }

    private void UpdateSfx(Vector2[] oldPositions)
    {
        if (sfx == null)
            return;

        float intensity = 0f;
        if (nodes.Length > 0 && nodes.Length == oldPositions.Length && Util.TryGetPlayer(out Player player))
        {
            float minDistSqr = float.MaxValue;

            Vector2 averageOffset = Vector2.Zero;
            int eff = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                float lengthSqr = (player.Center - nodes[i].Position).LengthSquared();
                if (lengthSqr < minDistSqr)
                {
                    minDistSqr = lengthSqr;
                    sfxPos = nodes[i].Position;
                }

                Vector2 offset = nodes[i].Position - oldPositions[i];
                if (offset.LengthSquared() < 0.025f && eff > 1)
                    eff--;
                averageOffset += offset;
            }
            averageOffset /= eff;

            intensity = Tight ? 0f : Calc.ClampedMap(averageOffset.Length(), 0f, 2f);
        }
        Audio.SetParameter(sfx, "intensity", intensity);
        Audio.Position(sfx, sfxPos);
    }

    private void RemoveSfx()
    {
        Audio.Stop(sfx);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        RemoveSfx();
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        AttachedEndsToSolids(scene);
    }

    public override void Update()
    {
        base.Update();

        Vector2[] oldPositions = nodes.Select(node => node.Position).ToArray();
        UpdateChain();
        UpdateSfx(oldPositions);

        if (Vector2.Distance(nodes[0].Position, nodes[nodes.Length - 1].Position) > (nodes.Length + 1) * distanceConstraint)
            BreakInHalf();
    }

    private void BreakInHalf()
    {
        RemoveSelf();
        Vector2 middleNode = nodes[nodes.Length / 2].Position;

        Chain a, b;
        Scene.Add(a = new Chain(outline, nodes.Length / 2, 8, () => middleNode, attachedStartGetter, texture));
        a.AttachedEndsToSolids(Scene);
        a.ShakeImpulse();

        Scene.Add(b = new Chain(outline, nodes.Length / 2, 8, () => middleNode, attachedEndGetter, texture));
        b.AttachedEndsToSolids(Scene);
        b.ShakeImpulse();

        Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_block, middleNode);

        Level level = SceneAs<Level>();
        for (int i = 0; i < 10; i++)
            level.ParticlesFG.Emit(ZipMover.P_Sparks, middleNode, Calc.Random.NextAngle());
    }

    private void UpdateChain()
    {
        bool startAttached = attachedStartGetter != null;
        bool endAttached = attachedEndGetter != null;
        if (startAttached)
        {
            nodes[0].Position = attachedStartGetter();
            nodes[0].Velocity = Vector2.Zero;
        }
        if (endAttached)
        {
            nodes[nodes.Length - 1].Position = attachedEndGetter();
            nodes[nodes.Length - 1].Velocity = Vector2.Zero;
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].Acceleration += Vector2.UnitY * 220f;
            if (Scene is not null && !Tight)
                nodes[i].Acceleration += SceneAs<Level>().Wind;
            nodes[i].UpdateStep();
        }

        if (!startAttached && !endAttached)
        {
            for (int i = 1; i < nodes.Length; i++)
                nodes[i].ConstraintTo(nodes[i - 1].Position, distanceConstraint, Tight);
            for (int i = nodes.Length - 2; i >= 0; i--)
                nodes[i].ConstraintTo(nodes[i + 1].Position, distanceConstraint, Tight);
        }
        else
        {
            if (startAttached)
            {
                for (int i = 1; i < nodes.Length - (endAttached ? 1 : 0); i++)
                    nodes[i].ConstraintTo(nodes[i - 1].Position, distanceConstraint, Tight);
            }
            if (endAttached)
            {
                for (int i = nodes.Length - 2; i >= (startAttached ? 1 : 0); i--)
                    nodes[i].ConstraintTo(nodes[i + 1].Position, distanceConstraint, Tight);
            }
        }
    }

    private void ShakeImpulse()
    {
        for (int i = attachedStartGetter != null ? 1 : 0; i < nodes.Length - (attachedEndGetter != null ? 1 : 0); i++)
        {
            nodes[i].Acceleration += Util.RandomDir(10000f);
        }
    }

    public override void Render()
    {
        base.Render();

        if (outline)
        {
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                if (Calc.Round(nodes[i].Position) == Calc.Round(nodes[i + 1].Position))
                    continue;

                float yScale = Vector2.Distance(nodes[i].Position, nodes[i + 1].Position) / distanceConstraint;
                Vector2 mid = (nodes[i].Position + nodes[i + 1].Position) * 0.5f;
                float angle = Calc.Angle(nodes[i].Position, nodes[i + 1].Position) - MathHelper.PiOver2;
                segment.DrawOutlineOnlyCentered(mid, new Vector2(1f, yScale), angle);
            }
        }

        for (int i = 0; i < nodes.Length - 1; i++)
        {
            if (Calc.Round(nodes[i].Position) == Calc.Round(nodes[i + 1].Position))
                continue;

            float yScale = Vector2.Distance(nodes[i].Position, nodes[i + 1].Position) / distanceConstraint;
            Vector2 mid = (nodes[i].Position + nodes[i + 1].Position) * 0.5f;
            float angle = Calc.Angle(nodes[i].Position, nodes[i + 1].Position) - MathHelper.PiOver2;
            segment.DrawCentered(mid, Color.White, new Vector2(1f, yScale), angle);
        }
    }

    public static void DrawChainLine(Vector2 from, Vector2 to, MTexture texture, bool outline)
    {
        Vector2 dir = Vector2.Normalize(to - from);
        float angle = dir.Angle() - MathHelper.PiOver2;
        float d = Vector2.Distance(from, to);

        MTexture tip = texture.GetSubtexture(0, 0, 8, 8),
                 mid = texture.GetSubtexture(0, 8, 8, 8);

        if (outline)
        {
            for (float t = d; t >= 0f; t -= 8f)
                mid.DrawOutlineOnlyCentered(Vector2.Lerp(from, to, t / d), Vector2.One, angle);
            tip.DrawOutlineOnlyCentered(from + (dir * 4f), Vector2.One, angle);
        }

        for (float t = d; t >= 0f; t -= 8f)
            mid.DrawCentered(Vector2.Lerp(from, to, t / d), Color.White, 1f, angle);
        tip.DrawCentered(from + (dir * 4f), Color.White, 1f, angle);
    }

    public static void InitializeTextures()
    {
        DefaultChain = GFX.Game[DEFAULT_CHAIN_PATH];
    }
}
