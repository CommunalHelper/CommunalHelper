using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities {
    [CustomEntity("CommunalHelper/NoOverlayLookout")]
    public class NoOverlayLookout : Entity {
        private TalkComponent talk;
        private Sprite sprite;
        private Tween lightTween;

        private bool interacting;

        private bool onlyY;

        private List<Vector2> nodes;
        private int node;
        private float nodePercent;

        private bool summit;

        private string animPrefix = "";

        public NoOverlayLookout(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Depth = Depths.Above;

            Add(talk = new TalkComponent(new Rectangle(-24, -8, 48, 8), new Vector2(-0.5f, -20f), Interact) {
                PlayerMustBeFacing = false,
            });

            summit = data.Bool("summit");
            onlyY = data.Bool("onlyY");

            Collider = new Hitbox(4f, 4f, -2f, -4f);

            VertexLight vertexLight = new VertexLight(new Vector2(-1f, -11f), Color.White, 0.8f, 16, 24);
            Add(vertexLight);

            lightTween = vertexLight.CreatePulseTween();
            Add(lightTween);

            Add(sprite = GFX.SpriteBank.Create("lookout"));
            sprite.OnFrameChange = delegate (string s) {
                switch (s) {
                    case "idle":
                    case "badeline_idle":
                    case "nobackpack_idle":
                        if (sprite.CurrentAnimationFrame == sprite.CurrentAnimationTotalFrames - 1)
                            lightTween.Start();
                        break;
                }
            };

            Vector2[] array = data.NodesOffset(offset);
            if (array != null && array.Length != 0)
                nodes = new List<Vector2>(array);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);

            if (interacting) {
                Player player = scene.Tracker.GetEntity<Player>();
                if (player != null)
                    player.StateMachine.State = Player.StNormal;
            }
        }

        private void Interact(Player player) {
            if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineAsBadeline || SaveData.Instance.Assists.PlayAsBadeline)
                animPrefix = "badeline_";
            else if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineNoBackpack)
                animPrefix = "nobackpack_";
            else
                animPrefix = "";

            Add(new Coroutine(LookRoutine(player)) {
                RemoveOnComplete = true
            });

            interacting = true;
        }

        public void StopInteracting() {
            interacting = false;
            sprite.Play(animPrefix + "idle");
        }

        public override void Update() {
            if (talk.UI != null)
                talk.UI.Visible = !CollideCheck<Solid>();

            base.Update();

            Player player = Scene.Tracker.GetEntity<Player>();
            if (player != null) {
                sprite.Active = interacting || player.StateMachine.State != Player.StDummy;
                if (!sprite.Active)
                    sprite.SetAnimationFrame(0);
            }
        }

        private IEnumerator LookRoutine(Player player) {
            Level level = SceneAs<Level>();
            SandwichLava sandwichLava = Scene.Entities.FindFirst<SandwichLava>();

            if (sandwichLava != null)
                sandwichLava.Waiting = true;

            if (player.Holding != null)
                player.Drop();

            player.StateMachine.State = Player.StDummy;
            yield return player.DummyWalkToExact((int) X, walkBackwards: false, 1f, cancelOnFall: true);

            if (Math.Abs(X - player.X) > 4f || player.Dead || !player.OnGround()) {
                if (!player.Dead)
                    player.StateMachine.State = Player.StNormal;
                yield break;
            }

            Audio.Play("event:/game/general/lookout_use", Position);

            if (player.Facing == Facings.Right)
                sprite.Play(animPrefix + "lookRight");
            else
                sprite.Play(animPrefix + "lookLeft");

            player.Sprite.Visible = player.Hair.Visible = false;
            yield return 0.2f;

            nodePercent = 0f;
            node = 0;

            Audio.Play("event:/ui/game/lookout_on");

            float accel = 800f;
            float maxSpeed = 240f;

            Vector2 cam = level.Camera.Position;
            Vector2 speed = Vector2.Zero;
            Vector2 lastDir = Vector2.Zero;
            Vector2 camStart = level.Camera.Position;
            Vector2 camStartCenter = camStart + new Vector2(160f, 90f);

            while (!Input.MenuCancel.Pressed && !Input.MenuConfirm.Pressed && !Input.Dash.Pressed && !Input.Jump.Pressed && interacting) {
                Vector2 value = Input.Aim.Value;
                if (onlyY)
                    value.X = 0f;

                if (Math.Sign(value.X) != Math.Sign(lastDir.X) || Math.Sign(value.Y) != Math.Sign(lastDir.Y))
                    Audio.Play("event:/game/general/lookout_move", Position);

                lastDir = value;

                if (sprite.CurrentAnimationID != "lookLeft" && sprite.CurrentAnimationID != "lookRight") {
                    if (value.X == 0f) {
                        if (value.Y == 0f)
                            sprite.Play(animPrefix + "looking");
                        else if (value.Y > 0f)
                            sprite.Play(animPrefix + "lookingDown");
                        else
                            sprite.Play(animPrefix + "lookingUp");
                    } else if (value.X > 0f) {
                        if (value.Y == 0f)
                            sprite.Play(animPrefix + "lookingRight");
                        else if (value.Y > 0f)
                            sprite.Play(animPrefix + "lookingDownRight");
                        else
                            sprite.Play(animPrefix + "lookingUpRight");
                    } else if (value.X < 0f) {
                        if (value.Y == 0f)
                            sprite.Play(animPrefix + "lookingLeft");
                        else if (value.Y > 0f)
                            sprite.Play(animPrefix + "lookingDownLeft");
                        else
                            sprite.Play(animPrefix + "lookingUpLeft");
                    }
                }

                if (nodes == null) {
                    speed += accel * value * Engine.DeltaTime;
                    if (value.X == 0f)
                        speed.X = Calc.Approach(speed.X, 0f, accel * 2f * Engine.DeltaTime);
                    if (value.Y == 0f)
                        speed.Y = Calc.Approach(speed.Y, 0f, accel * 2f * Engine.DeltaTime);
                    if (speed.Length() > maxSpeed)
                        speed = speed.SafeNormalize(maxSpeed);

                    List<Entity> lookoutBlockers = Scene.Tracker.GetEntities<LookoutBlocker>();

                    Vector2 vector = cam;

                    cam.X += speed.X * Engine.DeltaTime;
                    if (cam.X < level.Bounds.Left || cam.X + 320f > level.Bounds.Right)
                        speed.X = 0f;
                    cam.X = Calc.Clamp(cam.X, level.Bounds.Left, level.Bounds.Right - 320);

                    foreach (Entity item in lookoutBlockers) {
                        if (cam.X + 320f > item.Left && cam.Y + 180f > item.Top && cam.X < item.Right && cam.Y < item.Bottom) {
                            cam.X = vector.X;
                            speed.X = 0f;
                        }
                    }

                    cam.Y += speed.Y * Engine.DeltaTime;
                    if (cam.Y < level.Bounds.Top || cam.Y + 180f > level.Bounds.Bottom)
                        speed.Y = 0f;
                    cam.Y = Calc.Clamp(cam.Y, level.Bounds.Top, level.Bounds.Bottom - 180);

                    foreach (Entity item2 in lookoutBlockers) {
                        if (cam.X + 320f > item2.Left && cam.Y + 180f > item2.Top && cam.X < item2.Right && cam.Y < item2.Bottom) {
                            cam.Y = vector.Y;
                            speed.Y = 0f;
                        }
                    }

                    level.Camera.Position = cam;
                } else {
                    Vector2 from = (node <= 0) ? camStartCenter : nodes[node - 1];
                    Vector2 to = nodes[node];

                    float d = (from - to).Length();

                    if (nodePercent < 0.25f && node > 0) {
                        Vector2 begin = Vector2.Lerp((node <= 1) ? camStartCenter : nodes[node - 2], from, 0.75f);
                        Vector2 end = Vector2.Lerp(from, to, 0.25f);

                        SimpleCurve simpleCurve = new SimpleCurve(begin, end, from);
                        level.Camera.Position = simpleCurve.GetPoint(0.5f + nodePercent / 0.25f * 0.5f);
                    } else if (nodePercent > 0.75f && node < nodes.Count - 1) {
                        Vector2 nodeVec = nodes[node + 1];
                        Vector2 begin = Vector2.Lerp(from, to, 0.75f);
                        Vector2 end = Vector2.Lerp(to, nodeVec, 0.25f);

                        SimpleCurve simpleCurve = new SimpleCurve(begin, end, to);
                        level.Camera.Position = simpleCurve.GetPoint((nodePercent - 0.75f) / 0.25f * 0.5f);
                    } else
                        level.Camera.Position = Vector2.Lerp(from, to, nodePercent);

                    level.Camera.Position += new Vector2(-160f, -90f);

                    nodePercent -= value.Y * (maxSpeed / d) * Engine.DeltaTime;
                    if (nodePercent < 0f) {
                        if (node > 0) {
                            node--;
                            nodePercent = 1f;
                        } else {
                            nodePercent = 0f;
                        }
                    } else if (nodePercent > 1f) {
                        if (node < nodes.Count - 1) {
                            node++;
                            nodePercent = 0f;
                        } else {
                            nodePercent = 1f;
                            if (summit) {
                                break;
                            }
                        }
                    }

                    float num2 = 0f;
                    float num3 = 0f;

                    for (int i = 0; i < nodes.Count; i++) {
                        float num4 = (((i == 0) ? camStartCenter : nodes[i - 1]) - nodes[i]).Length();
                        num3 += num4;

                        if (i < node)
                            num2 += num4;
                        else if (i == node)
                            num2 += num4 * nodePercent;
                    }
                }

                yield return null;
            }

            player.Sprite.Visible = (player.Hair.Visible = true);
            sprite.Play(animPrefix + "idle");
            Audio.Play("event:/ui/game/lookout_off");

            bool atSummitTop = summit && node >= nodes.Count - 1 && nodePercent >= 0.95f;
            if (atSummitTop) {
                yield return 0.5f;

                float duration = 3f;
                float approach = 0f;

                Coroutine component = new Coroutine(level.ZoomTo(new Vector2(160f, 90f), 2f, duration));
                Add(component);

                while (!Input.MenuCancel.Pressed && !Input.MenuConfirm.Pressed && !Input.Dash.Pressed && !Input.Jump.Pressed && interacting) {
                    approach = Calc.Approach(approach, 1f, Engine.DeltaTime / duration);
                    Audio.SetMusicParam("escape", approach);
                    yield return null;
                }
            }

            if ((camStart - level.Camera.Position).Length() > 600f) {
                Vector2 was = level.Camera.Position;
                Vector2 direction = (was - camStart).SafeNormalize();

                float approach = atSummitTop ? 1f : 0.5f;
                new FadeWipe(Scene, wipeIn: false).Duration = approach;
                for (float t = 0f; t < 1f; t += Engine.DeltaTime / approach) {
                    level.Camera.Position = was - direction * MathHelper.Lerp(0f, 64f, Ease.CubeIn(t));
                    yield return null;
                }
                level.Camera.Position = camStart + direction * 32f;
                new FadeWipe(Scene, wipeIn: true);
            }

            Audio.SetMusicParam("escape", 0f);

            level.ZoomSnap(Vector2.Zero, 1f);
            interacting = false;
            yield return .1f;
            player.StateMachine.State = Player.StNormal;
        }

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);

            if (interacting) {
                Player entity = scene.Tracker.GetEntity<Player>();
                if (entity != null) {
                    entity.StateMachine.State = Player.StNormal;
                    entity.Sprite.Visible = (entity.Hair.Visible = true);
                }
            }
        }
    }
}
