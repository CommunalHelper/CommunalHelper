using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/StationBlockTrack")]
    [Tracked(false)]
    public class StationBlockTrack : Entity {
        public class Node {
            public Node(int x, int y) {
                Position = new Vector2(x, y);
                Center = Position + new Vector2(4, 4);
                Hitbox = new Rectangle(x, y, 8, 8);
            }

            public Vector2 Position, Center;
            public Rectangle Hitbox;

            public Node NodeUp, NodeDown, NodeLeft, NodeRight;
            public StationBlockTrack TrackUp, TrackDown, TrackLeft, TrackRight;

            public float Percent = 0f;
        }

        public enum TrackSwitchState {
            None, On, Off
        }
        private TrackSwitchState switchState;
        private TrackSwitchState initialSwitchState;

        public bool CanBeUsed => switchState != TrackSwitchState.Off;

        private enum MoveMode {
            None, 
            ForwardOneWay, BackwardOneWay,
            ForwardForce, BackwardForce,
        }

        public bool HasGroup { get; private set; }
        public bool MasterOfGroup { get; private set; }
        public StationBlockTrack master;

        private Rectangle nodeRect1, nodeRect2, trackRect;
        private Node initialNodeData1, initialNodeData2;

        private List<Node> Track;
        private List<StationBlockTrack> Group;
        private bool multiBlockTrack = false;

        public Vector2? OneWayDir;
        public bool ForceMovement;
        
        private MTexture trackSprite, disabledTrackSprite;
        private List<MTexture> nodeSprite;

        private float sparkDirFromA, sparkDirFromB, sparkDirToA, sparkDirToB, length;
        public float Percent = 0f;
        private Vector2 from, to, sparkAdd;

        public bool Horizontal;

        private bool trackConstantLooping;
        private float trackStatePercent;
        public float TrackOffset = 0f;

        private static readonly string TracksPath = "objects/CommunalHelper/stationBlock/tracks/";

        public StationBlockTrack(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Depth = Depths.SolidsBelow;

            initialSwitchState = switchState = data.Enum("trackSwitchState", TrackSwitchState.None);
            if (CommunalHelperModule.Session.TrackInitialState == TrackSwitchState.Off && initialSwitchState != TrackSwitchState.None)
                Switch(TrackSwitchState.Off);

            trackStatePercent = switchState is TrackSwitchState.On or TrackSwitchState.None ? 0f : 1f;

            Horizontal = data.Bool("horizontal");
            multiBlockTrack = data.Bool("multiBlockTrack", false);
            Collider = new Hitbox(Horizontal ? data.Width : 8, Horizontal ? 8 : data.Height);

            Vector2 dir = Horizontal ? Vector2.UnitX : Vector2.UnitY;
            switch (data.Enum("moveMode", MoveMode.None)) {
                case MoveMode.ForwardForce:
                    ForceMovement = true;
                    OneWayDir = dir;
                    break;

                case MoveMode.ForwardOneWay:
                    OneWayDir = dir;
                    break;

                case MoveMode.BackwardForce:
                    ForceMovement = true;
                    OneWayDir = -dir;
                    break;

                case MoveMode.BackwardOneWay:
                    OneWayDir = -dir;
                    break;

                default:
                    break;
            }

            nodeRect1 = new Rectangle((int) X, (int) Y, 8, 8);
            nodeRect2 = new Rectangle((int) (X + Width - 8), (int) (Y + Height - 8), 8, 8);

            initialNodeData1 = new Node(nodeRect1.X, nodeRect1.Y);
            initialNodeData2 = new Node(nodeRect2.X, nodeRect2.Y);

            if (Horizontal) {
                initialNodeData1.NodeRight = initialNodeData2;
                initialNodeData1.TrackRight = this;

                initialNodeData2.NodeLeft = initialNodeData1;
                initialNodeData2.TrackLeft = this;

                trackRect = new Rectangle((int) X + 8, (int) Y, (int) Width - 16, (int) Height);
                length = Width - 8;
            } else {
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
            sparkDirFromA = num + (float) Math.PI / 8f;
            sparkDirFromB = num - (float) Math.PI / 8f;
            sparkDirToA = num + (float) Math.PI - (float) Math.PI / 8f;
            sparkDirToB = num + (float) Math.PI + (float) Math.PI / 8f;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (!HasGroup) {
                MasterOfGroup = true;
                Track = new List<Node>();
                Group = new List<StationBlockTrack>();
                AddToGroupAndFindChildren(this);

                bool multiBlock = false;
                foreach (StationBlockTrack t in Group) {
                    if (t.multiBlockTrack) {
                        multiBlock = true;
                        break;
                    }
                }

                List<Tuple<StationBlock, Node>> toAttach = new List<Tuple<StationBlock, Node>>();
                bool exit = false;
                foreach (Node node in Track) {
                    foreach (StationBlock entity in Scene.Tracker.GetEntities<StationBlock>()) {
                        if (!entity.IsAttachedToTrack &&
                            Math.Abs(node.Center.X - entity.Center.X) <= 4 &&
                            Math.Abs(node.Center.Y - entity.Center.Y) <= 4) {
                            toAttach.Add(new Tuple<StationBlock, Node>(entity, node));
                            if (!multiBlock) {
                                exit = true;
                                break;
                            }
                        }
                    }
                    if (exit)
                        break;
                }

                if (toAttach.Count == 0) {
                    SetTrackTheme(StationBlock.Themes.Normal, false);
                } else {
                    bool setTheme = false;
                    foreach (Tuple<StationBlock, Node> tuple in toAttach) {
                        // Found block(s) to attach.
                        Node node = tuple.Item2;
                        StationBlock block = tuple.Item1;

                        block.Attach(node);
                        if (!setTheme) {
                            OffsetTrack(block.Center - node.Center);
                            SetTrackTheme(block.Theme, block.reverseControls, block.CustomNode, block.CustomTrackH, block.CustomTrackV);
                            setTheme = true;
                        }
                        block.Position += node.Center - block.Center;
                    }
                }
            }
        }

        private void SetTrackTheme(StationBlock.Themes theme, bool reversedControls, MTexture customNode = null, MTexture customTrackH = null, MTexture customTrackV = null) {
            if (customNode == null && customTrackH == null && customTrackV == null) {
                string node, trackV, trackH;
                bool constantLooping;
                switch (theme) {
                    default:
                    case StationBlock.Themes.Normal:
                        constantLooping = false;
                        if (reversedControls) {
                            node = "altTrack/ball";
                            trackV = "altTrack/pipeV";
                            trackH = "altTrack/pipeH";
                        } else {
                            node = "track/ball";
                            trackV = "track/pipeV";
                            trackH = "track/pipeH";
                        }
                        break;

                    case StationBlock.Themes.Moon:
                        constantLooping = true;
                        if (reversedControls) {
                            node = "altMoonTrack/node";
                            trackV = "altMoonTrack/trackV";
                            trackH = "altMoonTrack/trackH";
                        } else {
                            node = "moonTrack/node";
                            trackV = "moonTrack/trackV";
                            trackH = "moonTrack/trackH";
                        }
                        break;
                }

                foreach (StationBlockTrack track in Group) {
                    track.trackConstantLooping = constantLooping;
                    track.trackSprite = GFX.Game[TracksPath + (track.Horizontal ? trackH : trackV)];
                    track.disabledTrackSprite = GFX.Game[TracksPath + "outline/" + (track.Horizontal ? "h" : "v")];
                    track.nodeSprite = GFX.Game.GetAtlasSubtextures(TracksPath + node);
                }
            } else {
                foreach (StationBlockTrack track in Group) {
                    track.trackSprite = track.Horizontal ? customTrackH : customTrackV;
                    track.nodeSprite = new List<MTexture>() { customNode };
                }
            }
        }

        private void OffsetTrack(Vector2 amount) {
            foreach (Node node in Track) {
                node.Position += amount;
                node.Center += amount;
            }
            foreach (StationBlockTrack track in Group) {
                track.Position += amount;
            }
        }

        private void AddToGroupAndFindChildren(StationBlockTrack from) {
            from.HasGroup = true;
            from.master = this;
            Group.Add(from);

            AddTrackSegmentToTrack(from.initialNodeData1, from.initialNodeData2, from);

            foreach (StationBlockTrack track in Scene.Tracker.GetEntities<StationBlockTrack>()) {
                if (!track.HasGroup && !from.trackRect.Intersects(track.trackRect)) {
                    if (from.nodeRect1.Intersects(track.nodeRect1) || from.nodeRect1.Intersects(track.nodeRect2) ||
                        from.nodeRect2.Intersects(track.nodeRect1) || from.nodeRect2.Intersects(track.nodeRect2)) {
                        AddToGroupAndFindChildren(track);
                    }
                }
            }
        }

        private static Node GetNodeAt(List<Node> lookAt, Vector2 pos) {
            foreach (Node node in lookAt) {
                if (node.Position == pos) {
                    return node;
                }
            }
            return null;
        }

        private void AddTrackSegmentToTrack(Node node1, Node node2, StationBlockTrack track) {
            Node foundNode1 = GetNodeAt(Track, node1.Position);
            Node foundNode2 = GetNodeAt(Track, node2.Position);

            if (foundNode1 == null) {
                Track.Add(node1);
            } else {
                if (foundNode1.NodeUp == null && node1.NodeUp != null) {
                    foundNode1.NodeUp = node1.NodeUp;
                    node1.NodeUp.NodeDown = foundNode1;
                    foundNode1.TrackUp = track;
                    node1.NodeUp.TrackDown = track;
                }
                if (foundNode1.NodeDown == null && node1.NodeDown != null) {
                    foundNode1.NodeDown = node1.NodeDown;
                    node1.NodeDown.NodeUp = foundNode1;
                    foundNode1.TrackDown = track;
                    node1.NodeDown.TrackUp = track;
                }
                if (foundNode1.NodeLeft == null && node1.NodeLeft != null) {
                    foundNode1.NodeLeft = node1.NodeLeft;
                    node1.NodeLeft.NodeRight = foundNode1;
                    foundNode1.TrackLeft = track;
                    node1.NodeLeft.TrackRight = track;
                }
                if (foundNode1.NodeRight == null && node1.NodeRight != null) {
                    foundNode1.NodeRight = node1.NodeRight;
                    node1.NodeRight.NodeLeft = foundNode1;
                    foundNode1.TrackRight = track;
                    node1.NodeRight.TrackLeft = track;
                }
            }

            if (foundNode2 == null) {
                Track.Add(node2);
            } else {
                if (foundNode2.NodeUp == null && node2.NodeUp != null) {
                    foundNode2.NodeUp = node2.NodeUp;
                    node2.NodeUp.NodeDown = foundNode2;
                    foundNode2.TrackUp = track;
                    node2.NodeUp.TrackDown = track;
                }
                if (foundNode2.NodeDown == null && node2.NodeDown != null) {
                    foundNode2.NodeDown = node2.NodeDown;
                    node2.NodeDown.NodeUp = foundNode2;
                    foundNode2.TrackDown = track;
                    node2.NodeDown.TrackUp = track;
                }
                if (foundNode2.NodeLeft == null && node2.NodeLeft != null) {
                    foundNode2.NodeLeft = node2.NodeLeft;
                    node2.NodeLeft.NodeRight = foundNode2;
                    foundNode2.TrackLeft = track;
                    node2.NodeLeft.TrackRight = track;
                }
                if (foundNode2.NodeRight == null && node2.NodeRight != null) {
                    foundNode2.NodeRight = node2.NodeRight;
                    node2.NodeRight.NodeLeft = foundNode2;
                    foundNode2.TrackRight = track;
                    node2.NodeRight.TrackLeft = track;
                }
            }
        }

        public override void Update() {
            base.Update();
            //trackStatePercent = Calc.Approach(trackStatePercent, switchState == TrackSwitchState.On ? 1f : 0f, Engine.DeltaTime);
            trackStatePercent += ((switchState is TrackSwitchState.On or TrackSwitchState.None ? 0f : 1f) - trackStatePercent) / 4 * Engine.DeltaTime * 25;
        }

        public override void Render() {
            base.Render();

            if (MasterOfGroup) {
                foreach (StationBlockTrack track in Group) {
                    track.DrawPipe();
                }

                foreach (Node node in Track) {
                    int frame = (int) (node.Percent * 8) % nodeSprite.Count; // Allows for somewhat speed control.
                    nodeSprite[frame].DrawCentered(node.Center);
                }
            }
        }

        private void DrawPipe() {
            if (switchState != TrackSwitchState.None) {
                for (int i = 0; i <= length; i += 8) {
                    disabledTrackSprite.Draw(Position + new Vector2(Horizontal ? i : 0, Horizontal ? 0 : i), Vector2.Zero, Color.White * trackStatePercent);
                }
            }

            int trackpercent = (int) (trackStatePercent * (length + 2));

            for (int i = (int) Util.Mod(trackConstantLooping ? Scene.TimeActive * 14 : TrackOffset, 8) + trackpercent; i <= length; i += 8) {
                trackSprite.Draw(Position + new Vector2(Horizontal ? i : 0, Horizontal ? 0 : i));
            }
        }

        public void CreateSparks(Vector2 position, ParticleType p) {
            SceneAs<Level>().ParticlesBG.Emit(p, position + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
            SceneAs<Level>().ParticlesBG.Emit(p, position - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
            SceneAs<Level>().ParticlesBG.Emit(p, position + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
            SceneAs<Level>().ParticlesBG.Emit(p, position - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
        }

        public static void SwitchTracks(Scene scene, TrackSwitchState state) {
            foreach (StationBlockTrack track in scene.Tracker.GetEntities<StationBlockTrack>()) {
                if (track.MasterOfGroup) {
                    foreach (StationBlockTrack child in track.Group) {
                        child.Switch(state);
                    }
                }
            }
        }

        private void Switch(TrackSwitchState state) {
            if (initialSwitchState == TrackSwitchState.None)
                return;
            switchState = initialSwitchState == state ? TrackSwitchState.On : TrackSwitchState.Off;
        }
    }
}