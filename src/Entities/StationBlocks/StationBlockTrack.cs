using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/StationBlockTrack")]
[Tracked(false)]
public class StationBlockTrack : Entity
{
    public class Node
    {
        public Node(int x, int y)
        {
            Position = new Vector2(x, y);
            Center = Position + new Vector2(4, 4);
            Hitbox = new Rectangle(x, y, 8, 8);
        }

        public Vector2 Position, Center;
        public Rectangle Hitbox;

        public Node NodeUp, NodeDown, NodeLeft, NodeRight;
        public StationBlockTrack TrackUp, TrackDown, TrackLeft, TrackRight;

        public Vector2 PushForce;
        public Color IndicatorColor;
        public Color IndicatorIncomingColor;
        public bool HasIndicator;
        public float ColorLerp;

        public float Percent = 0f;
    }

    public enum TrackSwitchState
    {
        None, On, Off
    }
    private TrackSwitchState switchState;
    private readonly TrackSwitchState initialSwitchState;

    public bool CanBeUsed => switchState != TrackSwitchState.Off;

    private enum MoveMode
    {
        None,
        ForwardOneWay, BackwardOneWay,
        ForwardForce, BackwardForce,
    }

    public bool HasGroup { get; private set; }
    public bool MasterOfGroup { get; private set; }
    public StationBlockTrack master;

    private Rectangle nodeRect1, nodeRect2, trackRect;
    private readonly Node initialNodeData1, initialNodeData2;

    private List<Node> Track;
    private List<StationBlockTrack> Group;
    private readonly bool multiBlockTrack = false;

    public Vector2? OneWayDir;

    private MTexture trackSprite, disabledTrackSprite;
    private List<MTexture> nodeSprite;

    private readonly float sparkDirFromA, sparkDirFromB, sparkDirToA, sparkDirToB, length;
    public float Percent = 0f;
    private Vector2 from, to, sparkAdd;

    public bool Horizontal;

    private bool trackConstantLooping;
    private float trackStatePercent;
    public float TrackOffset = 0f;

    private static readonly string TracksPath = "objects/CommunalHelper/stationBlock/tracks/";

    private static MTexture forceArrow;

    public StationBlockTrack(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        Depth = Depths.SolidsBelow;

        initialSwitchState = switchState = data.Enum("trackSwitchState", TrackSwitchState.None);
        if (CommunalHelperModule.Session.TrackInitialState == TrackSwitchState.Off && initialSwitchState != TrackSwitchState.None)
            Switch(TrackSwitchState.Off);

        trackStatePercent = switchState is TrackSwitchState.On or TrackSwitchState.None ? 0f : 1f;

        Horizontal = data.Bool("horizontal");
        multiBlockTrack = data.Bool("multiBlockTrack", false);
        Collider = new Hitbox(Horizontal ? data.Width : 8, Horizontal ? 8 : data.Height);

        nodeRect1 = new Rectangle((int) X, (int) Y, 8, 8);
        nodeRect2 = new Rectangle((int) (X + Width - 8), (int) (Y + Height - 8), 8, 8);

        initialNodeData1 = new Node(nodeRect1.X, nodeRect1.Y);
        initialNodeData2 = new Node(nodeRect2.X, nodeRect2.Y);

        Color indicatorColor = data.HexColorNullable("indicatorColor") ?? Color.White;
        Color indicatorIncomingColor = data.HexColorNullable("indicatorIncomingColor") ?? Color.White;
        bool hasIndicator = data.Bool("indicator", true);

        Vector2 dir = Horizontal ? Vector2.UnitX : Vector2.UnitY;
        switch (data.Enum("moveMode", MoveMode.None))
        {
            case MoveMode.ForwardForce:
                initialNodeData1.PushForce = Horizontal ? Vector2.UnitX : Vector2.UnitY;
                initialNodeData1.HasIndicator = hasIndicator;
                if (hasIndicator)
                {
                    initialNodeData1.IndicatorColor = indicatorColor;
                    initialNodeData1.IndicatorIncomingColor = indicatorIncomingColor;
                }
                OneWayDir = dir;
                break;

            case MoveMode.ForwardOneWay:
                OneWayDir = dir;
                break;

            case MoveMode.BackwardForce:
                initialNodeData2.PushForce = Horizontal ? -Vector2.UnitX : -Vector2.UnitY;
                initialNodeData2.HasIndicator = hasIndicator;
                if (hasIndicator)
                {
                    initialNodeData2.IndicatorColor = indicatorColor;
                    initialNodeData2.IndicatorIncomingColor = indicatorIncomingColor;
                }
                OneWayDir = -dir;
                break;

            case MoveMode.BackwardOneWay:
                OneWayDir = -dir;
                break;

            default:
                break;
        }

        if (Horizontal)
        {
            initialNodeData1.NodeRight = initialNodeData2;
            initialNodeData1.TrackRight = this;

            initialNodeData2.NodeLeft = initialNodeData1;
            initialNodeData2.TrackLeft = this;

            trackRect = new Rectangle((int) X + 8, (int) Y, (int) Width - 16, (int) Height);
            length = Width - 8;
        }
        else
        {
            initialNodeData1.NodeDown = initialNodeData2;
            initialNodeData1.TrackDown = this;

            initialNodeData2.NodeUp = initialNodeData1;
            initialNodeData2.TrackUp = this;

            trackRect = new Rectangle((int) X, (int) Y + 8, (int) Width, (int) Height - 16);
            length = Height - 8;
        }

        from = initialNodeData1.Center;
        to = initialNodeData2.Center;
        sparkAdd = (from - to).SafeNormalize(3f).Perpendicular();

        float num = (from - to).Angle();
        sparkDirFromA = num + ((float) Math.PI / 8f);
        sparkDirFromB = num - ((float) Math.PI / 8f);
        sparkDirToA = num + (float) Math.PI - ((float) Math.PI / 8f);
        sparkDirToB = num + (float) Math.PI + ((float) Math.PI / 8f);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (!HasGroup)
        {
            MasterOfGroup = true;
            Track = new List<Node>();
            Group = new List<StationBlockTrack>();
            AddToGroupAndFindChildren(this);

            bool multiBlock = false;
            foreach (StationBlockTrack t in Group)
            {
                if (t.multiBlockTrack)
                {
                    multiBlock = true;
                    break;
                }
            }

            List<Tuple<StationBlock, Node>> toAttach = new();
            bool exit = false;
            foreach (Node node in Track)
            {
                foreach (StationBlock entity in Scene.Tracker.GetEntities<StationBlock>())
                {
                    if (!entity.IsAttachedToTrack &&
                        Math.Abs(node.Center.X - entity.Center.X) <= 4 &&
                        Math.Abs(node.Center.Y - entity.Center.Y) <= 4)
                    {
                        toAttach.Add(new Tuple<StationBlock, Node>(entity, node));
                        if (!multiBlock)
                        {
                            exit = true;
                            break;
                        }
                    }
                }
                if (exit)
                    break;
            }

            if (toAttach.Count == 0)
            {
                SetTrackTheme(StationBlock.Themes.Normal, false);
            }
            else
            {
                bool setTheme = false;
                foreach (Tuple<StationBlock, Node> tuple in toAttach)
                {
                    // Found block(s) to attach.
                    Node node = tuple.Item2;
                    StationBlock block = tuple.Item1;

                    block.Attach(node);
                    if (!setTheme)
                    {
                        OffsetTrack(block.Center - node.Center);
                        SetTrackTheme(block.Theme, block.ReverseControls, block.CustomNode, block.CustomTrackH, block.CustomTrackV);
                        setTheme = true;
                    }
                    block.Position += node.Center - block.Center;
                }
            }
        }
    }

    private void SetTrackTheme(StationBlock.Themes theme, bool reversedControls, MTexture customNode = null, MTexture customTrackH = null, MTexture customTrackV = null)
    {
        if (customNode == null && customTrackH == null && customTrackV == null)
        {
            string node, trackV, trackH;
            bool constantLooping;
            switch (theme)
            {
                default:
                case StationBlock.Themes.Normal:
                    constantLooping = false;
                    if (reversedControls)
                    {
                        node = "altTrack/ball";
                        trackV = "altTrack/pipeV";
                        trackH = "altTrack/pipeH";
                    }
                    else
                    {
                        node = "track/ball";
                        trackV = "track/pipeV";
                        trackH = "track/pipeH";
                    }
                    break;

                case StationBlock.Themes.Moon:
                    constantLooping = true;
                    if (reversedControls)
                    {
                        node = "altMoonTrack/node";
                        trackV = "altMoonTrack/trackV";
                        trackH = "altMoonTrack/trackH";
                    }
                    else
                    {
                        node = "moonTrack/node";
                        trackV = "moonTrack/trackV";
                        trackH = "moonTrack/trackH";
                    }
                    break;
            }

            foreach (StationBlockTrack track in Group)
            {
                track.trackConstantLooping = constantLooping;
                track.trackSprite = GFX.Game[TracksPath + (track.Horizontal ? trackH : trackV)];
                track.disabledTrackSprite = GFX.Game[TracksPath + "outline/" + (track.Horizontal ? "h" : "v")];
                track.nodeSprite = GFX.Game.GetAtlasSubtextures(TracksPath + node);
            }
        }
        else
        {
            foreach (StationBlockTrack track in Group)
            {
                track.trackSprite = track.Horizontal ? customTrackH : customTrackV;
                track.nodeSprite = new List<MTexture>() { customNode };
            }
        }
    }

    private void OffsetTrack(Vector2 amount)
    {
        foreach (Node node in Track)
        {
            node.Position += amount;
            node.Center += amount;
        }
        foreach (StationBlockTrack track in Group)
        {
            track.Position += amount;
        }
    }

    private void AddToGroupAndFindChildren(StationBlockTrack from)
    {
        from.HasGroup = true;
        from.master = this;
        Group.Add(from);

        AddTrackSegmentToTrack(from.initialNodeData1, from.initialNodeData2, from);

        foreach (StationBlockTrack track in Scene.Tracker.GetEntities<StationBlockTrack>())
        {
            if (!track.HasGroup && !from.trackRect.Intersects(track.trackRect))
            {
                if (from.nodeRect1.Intersects(track.nodeRect1) || from.nodeRect1.Intersects(track.nodeRect2) ||
                    from.nodeRect2.Intersects(track.nodeRect1) || from.nodeRect2.Intersects(track.nodeRect2))
                {
                    AddToGroupAndFindChildren(track);
                }
            }
        }
    }

    private static Node GetNodeAt(List<Node> lookAt, Vector2 pos)
    {
        foreach (Node node in lookAt)
        {
            if (node.Position == pos)
            {
                return node;
            }
        }
        return null;
    }

    private void AddNodeToTrack(Node node, StationBlockTrack track)
    {
        Node foundNode = GetNodeAt(Track, node.Position);

        if (foundNode == null)
        {
            Track.Add(node);
        }
        else
        {
            if (foundNode.NodeUp == null && node.NodeUp != null)
            {
                foundNode.NodeUp = node.NodeUp;
                node.NodeUp.NodeDown = foundNode;
                foundNode.TrackUp = track;
                node.NodeUp.TrackDown = track;
            }
            if (foundNode.NodeDown == null && node.NodeDown != null)
            {
                foundNode.NodeDown = node.NodeDown;
                node.NodeDown.NodeUp = foundNode;
                foundNode.TrackDown = track;
                node.NodeDown.TrackUp = track;
            }
            if (foundNode.NodeLeft == null && node.NodeLeft != null)
            {
                foundNode.NodeLeft = node.NodeLeft;
                node.NodeLeft.NodeRight = foundNode;
                foundNode.TrackLeft = track;
                node.NodeLeft.TrackRight = track;
            }
            if (foundNode.NodeRight == null && node.NodeRight != null)
            {
                foundNode.NodeRight = node.NodeRight;
                node.NodeRight.NodeLeft = foundNode;
                foundNode.TrackRight = track;
                node.NodeRight.TrackLeft = track;
            }
            if (foundNode.PushForce == Vector2.Zero && node.PushForce != Vector2.Zero)
            {
                foundNode.PushForce = node.PushForce;
                foundNode.HasIndicator = node.HasIndicator;
                foundNode.IndicatorColor = node.IndicatorColor;
                foundNode.IndicatorIncomingColor = node.IndicatorIncomingColor;
            }
        }
    }

    private void AddTrackSegmentToTrack(Node node1, Node node2, StationBlockTrack track)
    {
        AddNodeToTrack(node1, track);
        AddNodeToTrack(node2, track);
    }

    public override void Update()
    {
        base.Update();

        if (MasterOfGroup)
        {
            foreach (Node node in Track)
            {
                if (node.HasIndicator && node.ColorLerp != 0f)
                {
                    node.ColorLerp = Calc.Approach(node.ColorLerp, 0f, Engine.DeltaTime);
                }
            }
        }

        trackStatePercent += ((switchState is TrackSwitchState.On or TrackSwitchState.None ? 0f : 1f) - trackStatePercent) / 4 * Engine.DeltaTime * 25;
    }

    public override void Render()
    {
        base.Render();

        if (MasterOfGroup)
        {
            foreach (StationBlockTrack track in Group)
            {
                track.DrawPipe();
            }

            float bounce = (float) Math.Floor(1.5f * (Scene.TimeActive % 1f));

            foreach (Node node in Track)
            {
                int frame = (int) (node.Percent * 8) % nodeSprite.Count; // Allows for somewhat speed control.
                nodeSprite[frame].DrawCentered(node.Center);
                if (node.HasIndicator && node.PushForce != Vector2.Zero)
                {
                    forceArrow.DrawCentered(node.Center + (node.PushForce * (8f + bounce)), Color.Lerp(node.IndicatorColor, node.IndicatorIncomingColor, node.ColorLerp), 1f, node.PushForce.Angle());
                }
            }
        }
    }

    private void DrawPipe()
    {
        if (switchState != TrackSwitchState.None)
        {
            for (int i = 0; i <= length; i += 8)
            {
                disabledTrackSprite.Draw(Position + new Vector2(Horizontal ? i : 0, Horizontal ? 0 : i), Vector2.Zero, Color.White * trackStatePercent);
            }
        }

        int trackpercent = (int) (trackStatePercent * (length + 2));

        for (int i = (int) Util.Mod(trackConstantLooping ? Scene.TimeActive * 14 : TrackOffset, 8) + trackpercent; i <= length; i += 8)
        {
            trackSprite.Draw(Position + new Vector2(Horizontal ? i : 0, Horizontal ? 0 : i));
        }
    }

    public void CreateSparks(Vector2 position, ParticleType p)
    {
        SceneAs<Level>().ParticlesBG.Emit(p, position + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
        SceneAs<Level>().ParticlesBG.Emit(p, position - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
        SceneAs<Level>().ParticlesBG.Emit(p, position + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
        SceneAs<Level>().ParticlesBG.Emit(p, position - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
    }

    public static void SwitchTracks(Scene scene, TrackSwitchState state)
    {
        foreach (StationBlockTrack track in scene.Tracker.GetEntities<StationBlockTrack>())
        {
            if (track.MasterOfGroup)
            {
                foreach (StationBlockTrack child in track.Group)
                {
                    child.Switch(state);
                }
            }
        }
    }

    private void Switch(TrackSwitchState state)
    {
        if (initialSwitchState == TrackSwitchState.None)
            return;
        switchState = initialSwitchState == state ? TrackSwitchState.On : TrackSwitchState.Off;
    }

    internal static void InitializeTextures()
    {
        forceArrow = GFX.Game["objects/CommunalHelper/stationBlock/tracks/forceIndicator"];
    }
}
