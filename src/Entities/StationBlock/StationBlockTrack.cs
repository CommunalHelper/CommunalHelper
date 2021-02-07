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
            public Node nodeUp, nodeDown, nodeLeft, nodeRight;
            public StationBlockTrack trackUp, trackDown, trackLeft, trackRight;
            public float percent = 0f;
        }

        public enum TrackSwitchState {
            None, On, Off
        }
        public TrackSwitchState switchState;
        private TrackSwitchState initialSwitchState;

        public bool HasGroup { get; private set; }
        public bool MasterOfGroup { get; private set; }
        public StationBlockTrack master;

        private Rectangle nodeRect1, nodeRect2, trackRect;
        private Node initialNodeData1, initialNodeData2;

        private List<Node> Track;
        private List<StationBlockTrack> Group;

        private MTexture trackSprite, disabledTrackSprite;
        private List<MTexture> nodeSprite;

        private float sparkDirFromA, sparkDirFromB, sparkDirToA, sparkDirToB, length;
        public float percent = 0f;
        private Vector2 from, to, sparkAdd;

        public bool horizontal;
        private bool trackConstantLooping = false;
        public float trackOffset = 0f;
        private float trackStatePercent;

        private static readonly string TracksPath = "objects/CommunalHelper/stationBlock/tracks/";

        public StationBlockTrack(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Depth = Depths.SolidsBelow;

            initialSwitchState = switchState = data.Enum("trackSwitchState", TrackSwitchState.None);
            if (CommunalHelperModule.Session.TrackInitialState == TrackSwitchState.Off && initialSwitchState != TrackSwitchState.None)
                Switch(TrackSwitchState.Off);

            trackStatePercent = switchState is TrackSwitchState.On or TrackSwitchState.None ? 0f : 1f;

            horizontal = data.Bool("horizontal");
            Collider = new Hitbox(horizontal ? data.Width : 8, horizontal ? 8 : data.Height);
            
            nodeRect1 = new Rectangle((int) X, (int) Y, 8, 8);
            nodeRect2 = new Rectangle((int) (X + Width - 8), (int) (Y + Height - 8), 8, 8);

            initialNodeData1 = new Node(nodeRect1.X, nodeRect1.Y);
            initialNodeData2 = new Node(nodeRect2.X, nodeRect2.Y);
            if (horizontal) {
                initialNodeData1.nodeRight = initialNodeData2;
                initialNodeData1.trackRight = this;
                initialNodeData2.nodeLeft = initialNodeData1;
                initialNodeData2.trackLeft = this;
                trackRect = new Rectangle((int) X + 8, (int) Y, (int) Width - 16, (int) Height);
                length = Width - 8;
            } else {
                initialNodeData1.nodeDown = initialNodeData2;
                initialNodeData1.trackDown = this;
                initialNodeData2.nodeUp = initialNodeData1;
                initialNodeData2.trackUp = this;
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

                StationBlock block = null;
                foreach (Node node in Track) {
                    foreach (StationBlock entity in Scene.Tracker.GetEntities<StationBlock>()) {
                        if (!entity.IsAttachedToTrack &&
                            Math.Abs(node.Center.X - entity.Center.X) <= 4 &&
                            Math.Abs(node.Center.Y - entity.Center.Y) <= 4) {
                            block = entity;
                            break;
                        }
                    }

                    if (block == null)
                        continue;

                    // Found block to attach.
                    block.Attach(node);
                    OffsetTrack(block.Center - node.Center);
                    break;
                }

                if (block == null) {
                    SetTrackTheme(StationBlock.Theme.Normal, false);
                } else {
                    SetTrackTheme(block.theme, block.reverseControls, block.CustomNode, block.CustomTrackH, block.CustomTrackV);
                }
            }
        }

        private void SetTrackTheme(StationBlock.Theme theme, bool reversedControls, MTexture customNode = null, MTexture customTrackH = null, MTexture customTrackV = null) {
            if (customNode == null && customTrackH == null && customTrackV == null) {
                string node, trackV, trackH;
                bool constantLooping;
                switch (theme) {
                    default:
                    case StationBlock.Theme.Normal:
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

                    case StationBlock.Theme.Moon:
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
                    track.trackSprite = GFX.Game[TracksPath + (track.horizontal ? trackH : trackV)];
                    track.disabledTrackSprite = GFX.Game[TracksPath + "outline/" + (track.horizontal ? "h" : "v")];
                    track.nodeSprite = GFX.Game.GetAtlasSubtextures(TracksPath + node);
                }
            } else {
                foreach (StationBlockTrack track in Group) {
                    track.trackSprite = track.horizontal ? customTrackH : customTrackV;
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
                if (foundNode1.nodeUp == null && node1.nodeUp != null) {
                    foundNode1.nodeUp = node1.nodeUp;
                    node1.nodeUp.nodeDown = foundNode1;
                    foundNode1.trackUp = track;
                    node1.nodeUp.trackDown = track;
                }
                if (foundNode1.nodeDown == null && node1.nodeDown != null) {
                    foundNode1.nodeDown = node1.nodeDown;
                    node1.nodeDown.nodeUp = foundNode1;
                    foundNode1.trackDown = track;
                    node1.nodeDown.trackUp = track;
                }
                if (foundNode1.nodeLeft == null && node1.nodeLeft != null) {
                    foundNode1.nodeLeft = node1.nodeLeft;
                    node1.nodeLeft.nodeRight = foundNode1;
                    foundNode1.trackLeft = track;
                    node1.nodeLeft.trackRight = track;
                }
                if (foundNode1.nodeRight == null && node1.nodeRight != null) {
                    foundNode1.nodeRight = node1.nodeRight;
                    node1.nodeRight.nodeLeft = foundNode1;
                    foundNode1.trackRight = track;
                    node1.nodeRight.trackLeft = track;
                }
            }

            if (foundNode2 == null) {
                Track.Add(node2);
            } else {
                if (foundNode2.nodeUp == null && node2.nodeUp != null) {
                    foundNode2.nodeUp = node2.nodeUp;
                    node2.nodeUp.nodeDown = foundNode2;
                    foundNode2.trackUp = track;
                    node2.nodeUp.trackDown = track;
                }
                if (foundNode2.nodeDown == null && node2.nodeDown != null) {
                    foundNode2.nodeDown = node2.nodeDown;
                    node2.nodeDown.nodeUp = foundNode2;
                    foundNode2.trackDown = track;
                    node2.nodeDown.trackUp = track;
                }
                if (foundNode2.nodeLeft == null && node2.nodeLeft != null) {
                    foundNode2.nodeLeft = node2.nodeLeft;
                    node2.nodeLeft.nodeRight = foundNode2;
                    foundNode2.trackLeft = track;
                    node2.nodeLeft.trackRight = track;
                }
                if (foundNode2.nodeRight == null && node2.nodeRight != null) {
                    foundNode2.nodeRight = node2.nodeRight;
                    node2.nodeRight.nodeLeft = foundNode2;
                    foundNode2.trackRight = track;
                    node2.nodeRight.trackLeft = track;
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
                    int frame = (int) (node.percent * 8) % nodeSprite.Count; // Allows for somewhat speed control.
                    nodeSprite[frame].DrawCentered(node.Center);
                }
            }
        }

        private void DrawPipe() {
            if (switchState != TrackSwitchState.None)
                for (int i = 0; i <= length; i += 8) {
                    disabledTrackSprite.Draw(Position + new Vector2(horizontal ? i : 0, horizontal ? 0 : i), Vector2.Zero, Color.White * trackStatePercent);
                }

            int trackpercent = (int) (trackStatePercent * (length + 2));

            for (int i = (int) mod(trackConstantLooping ? Scene.TimeActive * 14 : trackOffset, 8) + trackpercent; i <= length; i += 8) {
                trackSprite.Draw(Position + new Vector2(horizontal ? i : 0, horizontal ? 0 : i));
            }

        }

        public void CreateSparks(Vector2 position, ParticleType p) {
            SceneAs<Level>().ParticlesBG.Emit(p, position + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
            SceneAs<Level>().ParticlesBG.Emit(p, position - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
            SceneAs<Level>().ParticlesBG.Emit(p, position + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
            SceneAs<Level>().ParticlesBG.Emit(p, position - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
        }

        private static float mod(float x, float m) {
            return (x % m + m) % m;
        }

        public static void SwitchTracks(Scene scene, TrackSwitchState state) {
            foreach (StationBlockTrack track in scene.Tracker.GetEntities<StationBlockTrack>()) {
                if(track.MasterOfGroup) {
                    foreach(StationBlockTrack child in track.Group) {
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
