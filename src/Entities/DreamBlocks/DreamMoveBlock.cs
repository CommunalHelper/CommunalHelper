using Celeste.Mod.CommunalHelper.Components;
using FMOD.Studio;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/DreamMoveBlock")]
public class DreamMoveBlock : CustomDreamBlock
{
    public enum MovementState
    {
        Idling,
        Moving,
        Breaking
    }

    public static ParticleType P_Activate;
    public static ParticleType P_Break;
    public static ParticleType[] dreamParticles;
    private int moveParticleIndex = 0;
    private int breakParticleIndex = 0;
    private int activateParticleIndex = 0;

    private const float Accel = 300f;
    private const float MoveSpeed = 60f;
    private const float FastMoveSpeed = 75f;

    private const float SteerSpeed = (float) Math.PI * 16f;
    private const float MaxAngle = (float) Math.PI / 4f;
    private const float NoSteerTime = 0.2f;

    private const float CrashResetTime = 0.1f;
    private const float CrashStartShakingTime = 0.15f;
    private readonly float crashTime;
    private readonly float regenTime;
    private readonly bool shakeOnCollision;

    private readonly float moveSpeed;

    public MoveBlock.Directions direction;
    private readonly float homeAngle;
    private readonly int angleSteerSign;
    internal Vector2 startPosition;
    public MovementState state = MovementState.Idling;

    private bool leftPressed;

    private bool rightPressed;

    private bool topPressed;

    private float speed;
    private float targetSpeed;

    private float angle;
    private float targetAngle;

    private Player noSquish;

    private List<Image> topButton = new();
    private List<Image> leftButton = new();
    private List<Image> rightButton = new();

    private readonly List<MTexture> arrows = new();

    private List<Image> body = new List<Image>();

    private Color fillColor = Calc.HexToColor("FFFFFF");
    private Color idleButtonsColor = Calc.HexToColor("FFFFFF");
    private Color idleArrowColor = Calc.HexToColor("FFFFFF");
    private Color idleWobbleLinesColor = Calc.HexToColor("FFFFFF");
    private Color movingButtonsColor = Calc.HexToColor("FFFFFF");
    private Color movingArrowColor = Calc.HexToColor("FFFFFF");
    private Color movingWobbleLinesColor = Calc.HexToColor("FFFFFF");
    private Color breakingWobbleLinesColor = Calc.HexToColor("FFFFFF");
    private Color breakingCrossColor = Calc.HexToColor("FFFFFF");

    private Color wobbleLineColor = Calc.HexToColor("FFFFFF");

    private float flash;
    private readonly SoundSource moveSfx;

    internal bool triggered;
    private float particleRemainder;

    private readonly Coroutine controller;
    private readonly bool noCollide;
    private readonly bool canSteer;

    private bool oneUseBroken;

    internal static void InitializeParticles()
    {
        P_Activate = new ParticleType(MoveBlock.P_Activate)
        {
            Color = Color.White
        };
        P_Break = new ParticleType(MoveBlock.P_Break)
        {
            Color = Color.White
        };

        ParticleType particle = new(MoveBlock.P_Move)
        {
            ColorMode = ParticleType.ColorModes.Choose
        };
        dreamParticles = new ParticleType[4];
        for (int i = 0; i < 4; i++)
        {
            dreamParticles[i] = new ParticleType(particle);
        }
    }

    public DreamMoveBlock(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        startPosition = Position;

        // Backwards Compatibility
        moveSpeed = data.Bool("fast") ? FastMoveSpeed : data.Float("moveSpeed", MoveSpeed);
        noCollide = data.Bool("noCollide");

        canSteer = data.Bool("canSteer");

        direction = data.Enum<MoveBlock.Directions>("direction");
        switch (direction)
        {
            case MoveBlock.Directions.Left:
                angleSteerSign = -1;
                break;
            case MoveBlock.Directions.Up:
                angleSteerSign = 1;
                break;
            case MoveBlock.Directions.Down:
                angleSteerSign = -1;
                break;
            default:
                angleSteerSign = 1;
                break;
        }
        homeAngle = targetAngle = angle = direction.Angle();

        crashTime = data.Float("crashTime", 0.15f);
        regenTime = data.Float("regenTime", 3f);
        shakeOnCollision = data.Bool("shakeOnCollision", true);

        if (data.Attr("idleButtonsColor", "FFFFFF") != "FFFFFF")
        {
            this.idleButtonsColor = Calc.HexToColor(data.Attr("idleButtonsColor"));
        }

        if (data.Attr("idleArrowColor", "FFFFFF") != "FFFFFF")
        {
            this.idleArrowColor = Calc.HexToColor(data.Attr("idleArrowColor"));
        }

        if (data.Attr("idleWobbleLinesColor", "FFFFFF") != "FFFFFF")
        {
            this.idleWobbleLinesColor = Calc.HexToColor(data.Attr("idleWobbleLinesColor"));
        }

        if (data.Attr("movingButtonsColor", "FFFFFF") != "FFFFFF")
        {
            this.movingButtonsColor = Calc.HexToColor(data.Attr("movingButtonsColor"));
        }

        if (data.Attr("movingArrowColor", "FFFFFF") != "FFFFFF")
        {
            this.movingArrowColor = Calc.HexToColor(data.Attr("movingArrowColor"));
        }

        if (data.Attr("movingWobbleLinesColor", "FFFFFF") != "FFFFFF")
        {
            this.movingWobbleLinesColor = Calc.HexToColor(data.Attr("movingWobbleLinesColor"));
        }

        if (data.Attr("breakingWobbleLinesColor", "FFFFFF") != "FFFFFF")
        {
            this.breakingWobbleLinesColor = Calc.HexToColor(data.Attr("breakingWobbleLinesColor"));
        }

        if (data.Attr("breakingCrossColor", "FFFFFF") != "FFFFFF")
        {
            this.breakingCrossColor = Calc.HexToColor(data.Attr("breakingCrossColor"));
        }

        arrows = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/dreamMoveBlock/arrow");
        Add(moveSfx = new SoundSource());
        Add(controller = new Coroutine(Controller()));
        Add(new LightOcclude(0.5f));

        Add(new MoveBlockRedirectable(new MonoMod.Utils.DynamicData(this),
            () => false,
            () => direction,
            dir => direction = dir
        ));

        int num = data.Width / 8;
        int num2 = data.Height / 8;
        MTexture mTexture = GFX.Game["objects/CommunalHelper/dreamMoveBlock/base"];
        MTexture mTexture2 = GFX.Game["objects/CommunalHelper/dreamMoveBlock/button"];
        if (canSteer && (direction == MoveBlock.Directions.Left || direction == MoveBlock.Directions.Right))
        {
            for (int i = 0; i < num; i++)
            {
                int num3 = ((i != 0) ? ((i < num - 1) ? 1 : 2) : 0);
                AddImage(mTexture2.GetSubtexture(num3 * 8, 0, 8, 8), new Vector2(i * 8, -4f), 0f, new Vector2(1f, 1f), topButton);
            }
            mTexture = GFX.Game["objects/CommunalHelper/dreamMoveBlock/base_h"];
        }
        else if (canSteer && (direction == MoveBlock.Directions.Up || direction == MoveBlock.Directions.Down))
        {
            for (int j = 0; j < num2; j++)
            {
                int num4 = ((j != 0) ? ((j < num2 - 1) ? 1 : 2) : 0);
                AddImage(mTexture2.GetSubtexture(num4 * 8, 0, 8, 8), new Vector2(-4f, j * 8), (float) Math.PI / 2f, new Vector2(1f, -1f), leftButton);
                AddImage(mTexture2.GetSubtexture(num4 * 8, 0, 8, 8), new Vector2((num - 1) * 8 + 4, j * 8), (float) Math.PI / 2f, new Vector2(1f, 1f), rightButton);
            }
            mTexture = GFX.Game["objects/CommunalHelper/dreamMoveBlock/base_v"];
        }
        for (int k = 0; k < num; k++)
        {
            for (int l = 0; l < num2; l++)
            {
                int num5 = ((k != 0) ? ((k < num - 1) ? 1 : 2) : 0);
                int num6 = ((l != 0) ? ((l < num2 - 1) ? 1 : 2) : 0);
                AddImage(mTexture.GetSubtexture(num5 * 8, num6 * 8, 8, 8), new Vector2(k, l) * 8f, 0f, new Vector2(1f, 1f), body);
            }
        }
        UpdateColors();
    }

    private IEnumerator Controller()
    {
        while (true)
        {
            triggered = false;
            state = MovementState.Idling;
            while (!triggered && !HasPlayerRider())
            {
                yield return null;
            }


            Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamMoveBlock_dream_move_block_activate : SFX.game_04_arrowblock_activate, Position);
            state = MovementState.Moving;
            StartShaking(0.2f);
            ActivateParticles();
            yield return 0.2f;


            targetSpeed = moveSpeed;
            moveSfx.Play(CustomSFX.game_redirectMoveBlock_arrowblock_move);
            moveSfx.Param("arrow_stop", 0f);
            StopPlayerRunIntoAnimation = false;

            float crashTimer = crashTime;
            float crashResetTimer = CrashResetTime;
            float crashStartShakingTimer = CrashStartShakingTime;
            float noSteerTimer = 0.2f;
            while (true)
            {
                if (canSteer)
                {
                    targetAngle = homeAngle;
                    bool flag = ((direction != MoveBlock.Directions.Right && direction != 0) ? HasPlayerClimbing() : HasPlayerOnTop());
                    if (flag && noSteerTimer > 0f)
                    {
                        noSteerTimer -= Engine.DeltaTime;
                    }
                    if (flag)
                    {
                        if (noSteerTimer <= 0f)
                        {
                            if (direction == MoveBlock.Directions.Right || direction == MoveBlock.Directions.Left)
                            {
                                targetAngle = homeAngle + (float) Math.PI / 4f * (float) angleSteerSign * (float) Input.MoveY.Value;
                            }
                            else
                            {
                                targetAngle = homeAngle + (float) Math.PI / 4f * (float) angleSteerSign * (float) Input.MoveX.Value;
                            }
                        }
                    }
                    else
                    {
                        noSteerTimer = 0.2f;
                    }
                }

                if (Scene.OnInterval(0.02f) && Collidable)
                {
                    MoveParticles();
                }
                speed = Calc.Approach(speed, targetSpeed, Accel * Engine.DeltaTime);
                angle = Calc.Approach(angle, targetAngle, SteerSpeed * Engine.DeltaTime);
                Vector2 move = Calc.AngleToVector(angle, speed) * Engine.DeltaTime;
                bool hit;
                if (direction is MoveBlock.Directions.Right or MoveBlock.Directions.Left)
                {
                    hit = MoveCheck(move.XComp());
                    noSquish = Scene.Tracker.GetEntity<Player>();
                    MoveVCollideSolids(move.Y, thruDashBlocks: false);
                    noSquish = null;
                    if (Scene.OnInterval(0.03f))
                    {
                        if (move.Y > 0f)
                        {
                            ScrapeParticles(Vector2.UnitY);
                        }
                        else if (move.Y < 0f)
                        {
                            ScrapeParticles(-Vector2.UnitY);
                        }
                    }
                }
                else
                {
                    hit = MoveCheck(move.YComp());
                    noSquish = Scene.Tracker.GetEntity<Player>();
                    MoveHCollideSolids(move.X, thruDashBlocks: false);
                    noSquish = null;
                    if (Scene.OnInterval(0.03f))
                    {
                        if (move.X > 0f)
                        {
                            ScrapeParticles(Vector2.UnitX);
                        }
                        else if (move.X < 0f)
                        {
                            ScrapeParticles(-Vector2.UnitX);
                        }
                    }
                    if (direction == MoveBlock.Directions.Down && Top > SceneAs<Level>().Bounds.Bottom + 32)
                    {
                        hit = true;
                    }
                }
                if (hit)
                {
                    moveSfx.Param("arrow_stop", crashTimer > 0.15f ? 0.5f : 1f);
                    crashResetTimer = CrashResetTime;
                    if (crashStartShakingTimer < 0f && shakeOnCollision)
                        StartShaking();
                    if (!(crashTimer > 0f) && !shattering)
                    {
                        break;
                    }
                    crashTimer -= Engine.DeltaTime;
                    crashStartShakingTimer -= Engine.DeltaTime;
                }
                else
                {
                    moveSfx.Param("arrow_stop", 0f);
                    if (crashResetTimer > 0f)
                    {
                        crashResetTimer -= Engine.DeltaTime;
                    }
                    else
                    {
                        StopShaking();
                        crashTimer = crashTime;
                        crashStartShakingTimer = CrashStartShakingTime;
                    }
                }
                Level level = Scene as Level;
                if (Left < level.Bounds.Left || Top < level.Bounds.Top || Right > level.Bounds.Right)
                {
                    break;
                }
                yield return null;
            }


            Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamMoveBlock_dream_move_block_break : SFX.game_04_arrowblock_break, Position);
            moveSfx.Stop();
            state = MovementState.Breaking;
            speed = targetSpeed = 0f;
            angle = targetAngle = homeAngle;
            StartShaking(0.2f);
            StopPlayerRunIntoAnimation = true;
            yield return 0.2f;


            BreakParticles();
            ((MoveBlockRedirectable) Get<Redirectable>())?.ResetBlock();
            List<MoveBlockDebris> debris = new();
            for (int x = 0; x < Width; x += 8)
            {
                for (int y = 0; y < Height; y += 8)
                {
                    Vector2 offset = new(x + 4f, y + 4f);
                    MTexture texture = Calc.Random.Choose(GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/dreamMoveBlock/debris"));
                    MTexture altTexture = GFX.Game[texture.AtlasPath.Replace("debris", "disabledDebris")];
                    MoveBlockDebris d = Engine.Pooler.Create<MoveBlockDebris>()
                        .Init(Position + offset, Center, startPosition + offset, spr =>
                        {
                            spr.Texture = PlayerHasDreamDash ? texture : altTexture;
                        });
                    debris.Add(d);
                    Scene.Add(d);
                }
            }
            MoveStaticMovers(startPosition - Position);
            DisableStaticMovers();
            Position = startPosition;
            Visible = Collidable = false;


            float waitTime = Calc.Clamp(regenTime - 0.8f, 0, float.MaxValue);
            float debrisShakeTime = Calc.Clamp(regenTime - 0.6f, 0, 0.2f);
            float debrisMoveTime = Calc.Clamp(regenTime, 0, 0.6f);

            yield return waitTime;

            foreach (MoveBlockDebris d in debris)
            {
                d.StopMoving();
            }
            if (oneUseBroken)
            {
                OneUseDestroy();
                yield break;
            }

            while (CollideCheck<Actor>() || (noCollide ? CollideCheck<DreamBlock>() : CollideCheck<Solid>()))
            {
                yield return null;
            }


            Collidable = true;
            EventInstance sound = Audio.Play(SFX.game_04_arrowblock_reform_begin, debris[0].Position);
            Coroutine soundFollower = new(SoundFollowsDebrisCenter(sound, debris));
            Add(soundFollower);
            foreach (MoveBlockDebris d in debris)
            {
                d.StartShaking();
            }
            yield return debrisShakeTime;


            foreach (MoveBlockDebris d in debris)
            {
                d.ReturnHome(debrisMoveTime + 0.05f);
            }
            yield return debrisMoveTime;


            soundFollower.RemoveSelf();
            foreach (MoveBlockDebris d in debris)
            {
                d.RemoveSelf();
            }
            Audio.Play(PlayerHasDreamDash ? CustomSFX.game_dreamMoveBlock_dream_move_block_reappear : SFX.game_04_arrowblock_reappear, Position);
            Visible = true;
            EnableStaticMovers();
            speed = targetSpeed = 0f;
            angle = targetAngle = homeAngle;
            noSquish = null;
            this.fillColor = this.idleButtonsColor;
            UpdateColors();
            flash = 1f;
        }
    }

    public override void BeginShatter()
    {
        if (state != MovementState.Breaking)
            base.BeginShatter();
        oneUseBroken = true;
    }

    protected override void OneUseDestroy()
    {
        base.OneUseDestroy();
        Remove(controller);
        moveSfx.Stop();
    }

    public override void SetupCustomParticles(float canvasWidth, float canvasHeight)
    {
        base.SetupCustomParticles(canvasWidth, canvasHeight);
        if (PlayerHasDreamDash)
        {
            dreamParticles[0].Color = Calc.HexToColor("FFEF11");
            dreamParticles[0].Color2 = Calc.HexToColor("FF00D0");

            dreamParticles[1].Color = Calc.HexToColor("08a310");
            dreamParticles[1].Color2 = Calc.HexToColor("5fcde4");

            dreamParticles[2].Color = Calc.HexToColor("7fb25e");
            dreamParticles[2].Color2 = Calc.HexToColor("E0564C");

            dreamParticles[3].Color = Calc.HexToColor("5b6ee1");
            dreamParticles[3].Color2 = Calc.HexToColor("CC3B3B");
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                dreamParticles[i].Color = Color.LightGray * 0.5f;
                dreamParticles[i].Color2 = Color.LightGray * 0.75f;
            }
        }
    }

    private IEnumerator SoundFollowsDebrisCenter(EventInstance instance, List<MoveBlockDebris> debris)
    {
        while (true)
        {
            instance.getPlaybackState(out PLAYBACK_STATE state);
            if (state == PLAYBACK_STATE.STOPPED)
            {
                break;
            }
            Vector2 center = Vector2.Zero;
            foreach (MoveBlockDebris d in debris)
            {
                center += d.Position;
            }
            center /= debris.Count;
            Audio.Position(instance, center);
            yield return null;
        }
    }

    public override void Update()
    {
        base.Update();
        if (canSteer)
        {
            bool flag = (direction == MoveBlock.Directions.Up || direction == MoveBlock.Directions.Down) && CollideCheck<Player>(Position + new Vector2(-1f, 0f));
            bool flag2 = (direction == MoveBlock.Directions.Up || direction == MoveBlock.Directions.Down) && CollideCheck<Player>(Position + new Vector2(1f, 0f));
            bool flag3 = (direction == MoveBlock.Directions.Left || direction == MoveBlock.Directions.Right) && CollideCheck<Player>(Position + new Vector2(0f, -1f));
            foreach (Image item in topButton)
            {
                item.Y = (flag3 ? 2 : 0);
            }
            foreach (Image item2 in leftButton)
            {
                item2.X = (flag ? 2 : 0);
            }
            foreach (Image item3 in rightButton)
            {
                item3.X = base.Width + (float) (flag2 ? (-2) : 0);
            }
            if ((flag && !leftPressed) || (flag3 && !topPressed) || (flag2 && !rightPressed))
            {
                Audio.Play("event:/game/04_cliffside/arrowblock_side_depress", Position);
            }
            if ((!flag && leftPressed) || (!flag3 && topPressed) || (!flag2 && rightPressed))
            {
                Audio.Play("event:/game/04_cliffside/arrowblock_side_release", Position);
            }
            leftPressed = flag;
            rightPressed = flag2;
            topPressed = flag3;
        }
        if (moveSfx != null && moveSfx.Playing)
        {
            float num = (Calc.AngleToVector(angle, 1f) * new Vector2(-1f, 1f)).Angle();
            int num2 = (int) Math.Floor(((0f - num + ((float) Math.PI * 2f)) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f) + 0.5f);
            moveSfx.Param("arrow_influence", num2 + 1);
        }
        flash = Calc.Approach(flash, 0f, Engine.DeltaTime * 5f);
        UpdateColors();
    }

    public override void OnStaticMoverTrigger(StaticMover sm)
    {
        triggered = true;
    }

    public override void MoveHExact(int move)
    {
        if (noSquish != null && ((move < 0 && noSquish.X < X) || (move > 0 && noSquish.X > X)))
        {
            while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + (Vector2.UnitX * move)))
            {
                move -= Math.Sign(move);
            }
        }
        base.MoveHExact(move);
    }

    public override void MoveVExact(int move)
    {
        if (noSquish != null && move < 0 && noSquish.Y <= Y)
        {
            while (move != 0 && noSquish.CollideCheck<Solid>(noSquish.Position + (Vector2.UnitY * move)))
            {
                move -= Math.Sign(move);
            }
        }
        base.MoveVExact(move);
    }

    private bool MoveCheck(Vector2 speed)
    {
        if (speed.X != 0f)
        {
            if (!noCollide || CollideCheck<DreamBlock>(Position + speed.XComp()))
            {
                if (MoveHCollideSolids(speed.X, thruDashBlocks: false))
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        for (int num = 1; num >= -1; num -= 2)
                        {
                            Vector2 value = new(Math.Sign(speed.X), i * num);
                            if (!CollideCheck<Solid>(Position + value))
                            {
                                MoveVExact(i * num);
                                MoveHExact(Math.Sign(speed.X));
                                return false;
                            }
                        }
                    }
                    return true;
                }
            }
            else
            {
                MoveH(speed.X);
                return false;
            }
        }
        if (speed.Y != 0f)
        {
            if (!noCollide || CollideCheck<DreamBlock>(Position + speed.YComp()))
            {
                if (MoveVCollideSolids(speed.Y, thruDashBlocks: false))
                {
                    for (int j = 1; j <= 3; j++)
                    {
                        for (int num2 = 1; num2 >= -1; num2 -= 2)
                        {
                            Vector2 value2 = new(j * num2, Math.Sign(speed.Y));
                            if (!CollideCheck<Solid>(Position + value2))
                            {
                                MoveHExact(j * num2);
                                MoveVExact(Math.Sign(speed.Y));
                                return false;
                            }
                        }
                    }
                    return true;
                }
            }
            else
            {
                MoveV(speed.Y);
                return false;
            }
        }
        return false;
    }

    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;

        var f_activeLineColor = typeof(DreamBlock).GetField("activeLineColor", BindingFlags.NonPublic | BindingFlags.Static);
        var oldColor = f_activeLineColor.GetValue(null);
        f_activeLineColor.SetValue(null, wobbleLineColor);

        base.Render();

        Color color = Color.Lerp(ActiveLineColor, Color.Black, ColorLerp);
        if (state != MovementState.Breaking)
        {
            int value = (int) Math.Floor(((0f - angle + ((float) Math.PI * 2f)) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f) + 0.5f);
            MTexture arrow = arrows[Calc.Clamp(value, 0, 7)];
            if(state == MovementState.Moving)
            {
                arrow.DrawCentered(Center + baseData.Get<Vector2>("shake"), this.movingArrowColor);
            }
            else
            {
                arrow.DrawCentered(Center + baseData.Get<Vector2>("shake"), this.idleArrowColor);
            }
            
            if(canSteer)
            {
                foreach (Image item in leftButton)
                {
                    item.Render();
                }
                foreach (Image item2 in rightButton)
                {
                    item2.Render();
                }
                foreach (Image item3 in topButton)
                {
                    item3.Render();
                }

                foreach (Image item4 in body)
                {
                    item4.Render();
                }
            }
        }
        else
        {
            GFX.Game["objects/CommunalHelper/dreamMoveBlock/x"].DrawCentered(Center + baseData.Get<Vector2>("shake"), this.breakingCrossColor);
        }
        float num = flash * 4f;
        Draw.Rect(X - num, Y - num, Width + (num * 2f), Height + (num * 2f), Color.White * flash);
        Position = position;

        f_activeLineColor.SetValue(null, oldColor);
    }

    /// <summary>
    /// This method is called to update the buttons, arrow and body colors
    /// </summary>
    private void UpdateColors()
    {
        if (direction == MoveBlock.Directions.Left || direction == MoveBlock.Directions.Right)
        {
            if (canSteer)
            {
                base.topWobble = false;
            }
        }
        else if (direction == MoveBlock.Directions.Up || direction == MoveBlock.Directions.Down)
        {
            if (canSteer)
            {
                base.leftWobble = false;
                base.rightWobble = false;
            }
        }
        if (state == MovementState.Idling)
        {
            this.fillColor = Color.Lerp(fillColor, this.idleButtonsColor, 10f * Engine.DeltaTime);
            this.wobbleLineColor = this.idleWobbleLinesColor;
        }
        else if (state == MovementState.Moving)
        {
            this.fillColor = Color.Lerp(fillColor, this.movingButtonsColor, 10f * Engine.DeltaTime);
            this.wobbleLineColor = this.movingWobbleLinesColor;
        }
        else if(state == MovementState.Breaking && canSteer)
        {
            base.topWobble = true;
            base.bottomWobble = true;
            base.leftWobble = true;
            base.rightWobble = true;
            this.wobbleLineColor = this.breakingWobbleLinesColor;
        }

        if (canSteer)
        {
            foreach (Image item in topButton)
            {
                item.Color = this.fillColor;
            }
            foreach (Image item2 in leftButton)
            {
                item2.Color = this.fillColor;
            }
            foreach (Image item3 in rightButton)
            {
                item3.Color = this.fillColor;
            }
            foreach (Image item4 in body)
            {
                item4.Color = this.fillColor;
            }
        }
    }

    private void AddImage(MTexture tex, Vector2 position, float rotation, Vector2 scale, List<Image> addTo)
    {
        Image image = new Image(tex);
        image.Position = position + new Vector2(4f, 4f);
        image.CenterOrigin();
        image.Rotation = rotation;
        image.Scale = scale;
        Add(image);
        addTo?.Add(image);
    }

    private void ActivateParticles()
    {
        bool flag = direction == MoveBlock.Directions.Down || direction == MoveBlock.Directions.Up;
        bool left = (!canSteer || !flag) && !CollideCheck<Player>(Position - Vector2.UnitX);
        bool right = (!canSteer || !flag) && !CollideCheck<Player>(Position + Vector2.UnitX);
        bool top = (!canSteer || !flag) && !CollideCheck<Player>(Position - Vector2.UnitY);
        if (left)
        {
            for (int i = 1; i < (Height / 2) - 1; ++i)
            {
                ParticleType particle = dreamParticles[activateParticleIndex];
                Vector2 position = TopLeft + (Vector2.UnitY * i * 2);
                SceneAs<Level>().Particles.Emit(particle, 1, position, Vector2.One, (float) Math.PI);
                ++activateParticleIndex;
                activateParticleIndex %= 4;
            }
        }
        if (right)
        {
            for (int i = 1; i < (Height / 2) - 1; ++i)
            {
                ParticleType particle = dreamParticles[activateParticleIndex];
                Vector2 position = TopRight + (Vector2.UnitY * i * 2);
                SceneAs<Level>().Particles.Emit(particle, 1, position, Vector2.One, 0f);
                ++activateParticleIndex;
                activateParticleIndex %= 4;
            }
        }
        if (top)
        {
            for (int i = 1; i < (Width / 2) - 1; ++i)
            {
                ParticleType particle = dreamParticles[activateParticleIndex];
                Vector2 position = TopLeft + (Vector2.UnitX * i * 2);
                SceneAs<Level>().Particles.Emit(particle, 1, position, Vector2.One, -(float) Math.PI / 2f);
                ++activateParticleIndex;
                activateParticleIndex %= 4;
            }
        }
        for (int i = 1; i < (Width / 2) - 1; ++i)
        {
            ParticleType particle = dreamParticles[activateParticleIndex];
            Vector2 position = BottomLeft + (Vector2.UnitX * i * 2);
            SceneAs<Level>().Particles.Emit(particle, 1, position, Vector2.One, (float) Math.PI / 2f);
            ++activateParticleIndex;
            activateParticleIndex %= 4;
        }
    }

    private void BreakParticles()
    {
        Vector2 center = Center;
        for (int i = 0; i < Width; i += 4)
        {
            for (int j = 0; j < Height; j += 4)
            {
                Vector2 vector = Position + new Vector2(2 + i, 2 + j);
                SceneAs<Level>().Particles.Emit(dreamParticles[breakParticleIndex], 1, vector, Vector2.One * 2f, (vector - center).Angle());
                ++breakParticleIndex;
                breakParticleIndex %= 4;
            }
        }
    }

    private void MoveParticles()
    {
        Vector2 position;
        Vector2 positionRange;
        float dir;
        float num2;
        if (direction == MoveBlock.Directions.Right)
        {
            position = CenterLeft + Vector2.UnitX;
            positionRange = Vector2.UnitY * (Height - 4f);
            dir = (float) Math.PI;
            num2 = Height / 32f;
        }
        else if (direction == MoveBlock.Directions.Left)
        {
            position = CenterRight;
            positionRange = Vector2.UnitY * (Height - 4f);
            dir = 0f;
            num2 = Height / 32f;
        }
        else if (direction == MoveBlock.Directions.Down)
        {
            position = TopCenter + Vector2.UnitY;
            positionRange = Vector2.UnitX * (Width - 4f);
            dir = -(float) Math.PI / 2f;
            num2 = Width / 32f;
        }
        else
        {
            position = BottomCenter;
            positionRange = Vector2.UnitX * (Width - 4f);
            dir = (float) Math.PI / 2f;
            num2 = Width / 32f;
        }

        particleRemainder += num2;
        int amount = (int) particleRemainder;
        particleRemainder -= amount;
        positionRange *= 0.5f;
        if (amount > 0)
        {
            SceneAs<Level>().ParticlesBG.Emit(dreamParticles[moveParticleIndex], amount, position, positionRange, dir);
            ++moveParticleIndex;
            moveParticleIndex %= 4;
        }
    }

    private void ScrapeParticles(Vector2 dir)
    {
        if (noCollide)
            return;

        Collidable = false;
        if (dir.X != 0f)
        {
            float x = (!(dir.X > 0f)) ? (Left - 1f) : Right;
            for (int i = 0; i < Height; i += 8)
            {
                Vector2 vector = new(x, Top + 4f + i);
                if (Scene.CollideCheck<Solid>(vector))
                {
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector);
                }
            }
        }
        else
        {
            float y = (!(dir.Y > 0f)) ? (Top - 1f) : Bottom;
            for (int j = 0; j < Width; j += 8)
            {
                Vector2 vector2 = new(Left + 4f + j, y);
                if (Scene.CollideCheck<Solid>(vector2))
                {
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, vector2);
                }
            }
        }
        Collidable = true;
    }

    #region Hooks

    private static FieldInfo f_this;
    private static List<IDetour> hook_DreamBlock_Routines;

    internal static new void Load()
    {
        hook_DreamBlock_Routines = new List<IDetour>();
        Type[] nestedTypes = typeof(DreamBlock).GetNestedTypes(BindingFlags.NonPublic);
        foreach (string method in new string[] { "FastActivate", "FastDeactivate", "Activate", "Deactivate" })
        {
            Type type = nestedTypes.First(t => t.Name.StartsWith($"<{method}>"));
            f_this = type.GetField("<>4__this");
            hook_DreamBlock_Routines.Add(new ILHook(type.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance), DreamBlock_ActivationParticles));
        }
    }

    internal static new void Unload()
    {
        hook_DreamBlock_Routines.ForEach(hook => hook?.Dispose());
    }

    // Probably a bad idea to leave it like this, but it works
    private static void DreamBlock_ActivationParticles(ILContext il)
    {
        ILCursor cursor = new(il);
        while (cursor.TryGotoNext(instr => instr.MatchCallvirt<ParticleSystem>("Emit")))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_this);
            cursor.EmitDelegate<Func<DreamBlock, bool>>(block => block is DreamMoveBlock moveBlock && !moveBlock.Visible);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            foreach (ParameterDefinition param in ((MethodReference) cursor.Next.Operand).Parameters)
                cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
            cursor.Goto(cursor.Next, MoveType.After);
        }
    }

    #endregion
}
