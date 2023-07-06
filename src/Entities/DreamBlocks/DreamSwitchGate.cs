using Celeste.Mod.CommunalHelper.Entities;
using System.Collections;

namespace Celeste.Mod.CommunalHelper;

/*
 * Lots of stuff taken from Maddie's Helping Hand Flag Switch Gate entity
 * https://github.com/max4805/MaxHelpingHand/blob/master/Entities/FlagSwitchGate.cs
 */

[CustomEntity("CommunalHelper/DreamSwitchGate",
    "CommunalHelper/MaxHelpingHand/DreamFlagSwitchGate = DreamFlagSwitchGate")]
public class DreamSwitchGate : CustomDreamBlock
{
    private static ParticleType[] P_BehindDreamParticles;

    private readonly ParticleType P_RecoloredFire;
    private readonly ParticleType P_RecoloredFireBack;

    private readonly bool permanent;

    private readonly Sprite icon;
    private Vector2 iconOffset;
    private readonly Wiggler wiggler;

    private Vector2 node;

    private readonly SoundSource openSfx;

    public int ID { get; private set; }
    public string Flag { get; private set; }

    public bool Triggered { get; private set; }

    private Color inactiveColor = Calc.HexToColor("5fcde4");
    private Color activeColor = Color.White;
    private Color finishColor = Calc.HexToColor("f141df");

    private readonly float shakeTime;
    private readonly float moveTime;
    private readonly bool moveEased;

    private readonly string moveSound;
    private readonly string finishedSound;

    private readonly bool allowReturn;

    private bool isFlagSwitchGate;

    public static DreamSwitchGate DreamFlagSwitchGate(Level level, LevelData levelData, Vector2 offset, EntityData entityData)
    {
        entityData.Values["permanent"] = entityData.Bool("persistent");
        return new DreamSwitchGate(entityData, offset) { isFlagSwitchGate = true };
    }

    public DreamSwitchGate(EntityData data, Vector2 offset)
        : base(data, offset)
    {

        permanent = data.Bool("permanent");
        node = data.Nodes[0] + offset;

        isFlagSwitchGate = data.Bool("isFlagSwitchGate");

        ID = data.ID;
        Flag = data.Attr("flag");

        inactiveColor = Calc.HexToColor(data.Attr("inactiveColor", "5FCDE4"));
        activeColor = Calc.HexToColor(data.Attr("activeColor", "FFFFFF"));
        finishColor = Calc.HexToColor(data.Attr("finishColor", "F141DF"));

        shakeTime = data.Float("shakeTime", 0.5f);
        moveTime = data.Float("moveTime", 1.8f);
        moveEased = data.Bool("moveEased", true);

        moveSound = data.Attr("moveSound", SFX.game_gen_touchswitch_gate_open);
        finishedSound = data.Attr("finishedSound", SFX.game_gen_touchswitch_gate_finish);

        allowReturn = data.Bool("allowReturn");

        P_RecoloredFire = new ParticleType(TouchSwitch.P_Fire)
        {
            Color = finishColor
        };
        P_RecoloredFireBack = new ParticleType(TouchSwitch.P_Fire)
        {
            Color = inactiveColor
        };

        string iconAttribute = data.Attr("icon", "vanilla");
        icon = new Sprite(GFX.Game, iconAttribute == "vanilla" ? "objects/switchgate/icon" : $"objects/MaxHelpingHand/flagSwitchGate/{iconAttribute}/icon");
        icon.Add("spin", "", 0.1f, "spin");
        icon.Play("spin");
        icon.Rate = 0f;
        icon.Color = inactiveColor;
        icon.CenterOrigin();
        iconOffset = new Vector2(Width, Height) / 2f;
        Add(wiggler = Wiggler.Create(0.5f, 4f, scale =>
        {
            icon.Scale = Vector2.One * (1f + scale);
        }));

        Add(openSfx = new SoundSource());
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);

        bool check = isFlagSwitchGate ?
            (SceneAs<Level>().Session.GetFlag(Flag + "_gate" + ID) && !allowReturn) || SceneAs<Level>().Session.GetFlag(Flag) :
            Switch.CheckLevelFlag(SceneAs<Level>());

        if (check)
        {
            if (allowReturn && isFlagSwitchGate)
            {
                Add(new Coroutine(MoveBackAndForthSequence(Position, node, startAtNode: true)));
            }

            MoveTo(node);
            icon.Rate = 0f;
            icon.SetAnimationFrame(0);
            icon.Color = finishColor;
        }
        else
        {
            if (isFlagSwitchGate)
            {
                if (allowReturn)
                {
                    // go back and forth as needed.
                    Add(new Coroutine(MoveBackAndForthSequence(Position, node, startAtNode: false)));
                }
                else
                {
                    // we are only going to the node, then stopping.
                    Add(new Coroutine(MaxHelpingHandSequence(node, goingBack: false)));
                }
            }
            else
            {
                Add(new Coroutine(CommunalHelperSequence(node)));
            }
        }
    }

    public override void Render()
    {
        Vector2 position = Position;
        Position += Shake;
        base.Render();
        icon.Position = Center;
        icon.DrawOutline();
        icon.Render();

        // Redraw whiteFill over icon
        float whiteFill = baseData.Get<float>("whiteFill");
        if (whiteFill > 0)
            Draw.Rect(Position, Width, Height * baseData.Get<float>("whiteHeight"), Color.White * whiteFill);

        Position = position;
    }

    private IEnumerator MoveBackAndForthSequence(Vector2 position, Vector2 node, bool startAtNode)
    {
        while (true)
        {
            if (!startAtNode)
            {
                // go forth
                yield return new SwapImmediately(MaxHelpingHandSequence(node, goingBack: false));
            }

            // go back
            yield return new SwapImmediately(MaxHelpingHandSequence(position, goingBack: true));
            startAtNode = false;
        }
    }

    private IEnumerator CommunalHelperSequence(Vector2 node)
    {
        this.node = node;

        Vector2 start = Position;
        while (!Switch.Check(Scene))
        {
            yield return null;
        }

        if (permanent)
        {
            Switch.SetLevelFlag(SceneAs<Level>());
        }
        yield return 0.1f;

        openSfx.Play(SFX.game_gen_touchswitch_gate_open);
        StartShaking(0.5f);
        while (icon.Rate < 1f)
        {
            icon.Color = Color.Lerp(inactiveColor, activeColor, icon.Rate);
            icon.Rate += Engine.DeltaTime * 2f;
            yield return null;
        }

        yield return 0.1f;


        int particleAt = 0;
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 2f, start: true);
        tween.OnUpdate = t =>
        {
            MoveTo(Vector2.Lerp(start, node, t.Eased));
            if (Scene.OnInterval(0.1f))
            {
                particleAt++;
                particleAt %= 2;
                for (int n = 0; n < Width / 8f; n++)
                {
                    for (int num2 = 0; num2 < Height / 8f;
                    num2++)
                    {
                        if ((n + num2) % 2 == particleAt)
                        {
                            ParticleType pType = Calc.Random.Choose(P_BehindDreamParticles);
                            SceneAs<Level>().ParticlesBG.Emit(pType, Position + new Vector2(n * 8, num2 * 8) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
                        }
                    }
                }
            }
        };
        Add(tween);
        yield return 1.8f;

        bool collidable = Collidable;
        Collidable = false;
        if (node.X <= start.X)
        {
            Vector2 value = new(0f, 2f);
            for (int i = 0; i < Height / 8f; i++)
            {
                Vector2 vector = new(Left - 1f, Top + 4f + (i * 8));
                Vector2 point = vector + Vector2.UnitX;
                if (Scene.CollideCheck<Solid>(vector) && !Scene.CollideCheck<Solid>(point))
                {
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector + value, (float) Math.PI);
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector - value, (float) Math.PI);
                }
            }
        }
        if (node.X >= start.X)
        {
            Vector2 value = new(0f, 2f);
            for (int j = 0; j < Height / 8f; j++)
            {
                Vector2 vector = new(Right + 1f, Top + 4f + (j * 8));
                Vector2 point = vector - (Vector2.UnitX * 2f);
                if (Scene.CollideCheck<Solid>(vector) && !Scene.CollideCheck<Solid>(point))
                {
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector + value, 0f);
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector - value, 0f);
                }
            }
        }
        if (node.Y <= start.Y)
        {
            Vector2 value = new(2f, 0f);
            for (int k = 0; k < Width / 8f; k++)
            {
                Vector2 vectpr = new(Left + 4f + (k * 8), Top - 1f);
                Vector2 point = vectpr + Vector2.UnitY;
                if (Scene.CollideCheck<Solid>(vectpr) && !Scene.CollideCheck<Solid>(point))
                {
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vectpr + value, -(float) Math.PI / 2f);
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vectpr - value, -(float) Math.PI / 2f);
                }
            }
        }
        if (node.Y >= start.Y)
        {
            Vector2 value = new(2f, 0f);
            for (int l = 0; l < Width / 8f; l++)
            {
                Vector2 vector = new(Left + 4f + (l * 8), Bottom + 1f);
                Vector2 point = vector - (Vector2.UnitY * 2f);
                if (Scene.CollideCheck<Solid>(vector) && !Scene.CollideCheck<Solid>(point))
                {
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector + value, (float) Math.PI / 2f);
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, vector - value, (float) Math.PI / 2f);
                }
            }
        }
        Collidable = collidable;
        Audio.Play(SFX.game_gen_touchswitch_gate_finish, Position);
        StartShaking(0.2f);
        while (icon.Rate > 0f)
        {
            icon.Color = Color.Lerp(activeColor, finishColor, 1f - icon.Rate);
            icon.Rate -= Engine.DeltaTime * 4f;
            yield return null;
        }

        icon.Rate = 0f;
        icon.SetAnimationFrame(0);
        wiggler.Start();
        bool collidable2 = Collidable;
        Collidable = false;
        if (!Scene.CollideCheck<Solid>(Center))
        {
            for (int m = 0; m < 32; m++)
            {
                float num = Calc.Random.NextFloat((float) Math.PI * 2f);
                SceneAs<Level>().ParticlesFG.Emit(TouchSwitch.P_Fire, Center + Calc.AngleToVector(num, 4f), num);
            }
        }
        Collidable = collidable2;
    }

    private IEnumerator MaxHelpingHandSequence(Vector2 node, bool goingBack)
    {
        Vector2 start = Position;

        Color fromColor, toColor;

        if (!goingBack)
        {
            fromColor = inactiveColor;
            toColor = finishColor;
            while ((!Triggered || allowReturn) && !SceneAs<Level>().Session.GetFlag(Flag))
            {
                yield return null;
            }
        }
        else
        {
            fromColor = finishColor;
            toColor = inactiveColor;
            while (SceneAs<Level>().Session.GetFlag(Flag))
            {
                yield return null;
            }
        }

        yield return 0.1f;
        if (ShouldCancelMove(goingBack))
            yield break;

        // animate the icon
        openSfx.Play(moveSound);
        if (shakeTime > 0f)
        {
            StartShaking(shakeTime);
            while (icon.Rate < 1f)
            {
                icon.Color = Color.Lerp(fromColor, activeColor, icon.Rate);
                icon.Rate += Engine.DeltaTime / shakeTime;
                yield return null;
                if (ShouldCancelMove(goingBack))
                    yield break;
            }
        }
        else
        {
            icon.Rate = 1f;
        }

        yield return 0.1f;
        if (ShouldCancelMove(goingBack))
            yield break;

        // move the switch gate, emitting particles along the way
        int particleAt = 0;
        Tween tween = Tween.Create(Tween.TweenMode.Oneshot, moveEased ? Ease.CubeOut : null, moveTime + (moveEased ? 0.2f : 0f), start: true);
        tween.OnUpdate = tweenArg =>
        {
            MoveTo(Vector2.Lerp(start, node, tweenArg.Eased));
            if (Scene.OnInterval(0.1f))
            {
                particleAt++;
                particleAt %= 2;
                for (int tileX = 0; tileX < Width / 8f; tileX++)
                {
                    for (int tileY = 0; tileY < Height / 8f; tileY++)
                    {
                        if ((tileX + tileY) % 2 == particleAt)
                        {
                            SceneAs<Level>().ParticlesBG.Emit(SwitchGate.P_Behind,
                                Position + new Vector2(tileX * 8, tileY * 8) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
                        }
                    }
                }
            }
        };
        Add(tween);

        float moveTimeLeft = moveTime;
        while (moveTimeLeft > 0f)
        {
            yield return null;
            moveTimeLeft -= Engine.DeltaTime;
            if (ShouldCancelMove(goingBack, tween))
                yield break;
        }

        bool collidableBackup = Collidable;
        Collidable = false;

        // collide dust particles on the left
        if (node.X <= start.X)
        {
            Vector2 add = new(0f, 2f);
            for (int tileY = 0; tileY < Height / 8f; tileY++)
            {
                Vector2 collideAt = new(Left - 1f, Top + 4f + (tileY * 8));
                Vector2 noCollideAt = collideAt + Vector2.UnitX;
                if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                {
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, (float) Math.PI);
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, (float) Math.PI);
                }
            }
        }

        // collide dust particles on the rigth
        if (node.X >= start.X)
        {
            Vector2 add = new(0f, 2f);
            for (int tileY = 0; tileY < Height / 8f; tileY++)
            {
                Vector2 collideAt = new(Right + 1f, Top + 4f + (tileY * 8));
                Vector2 noCollideAt = collideAt - (Vector2.UnitX * 2f);
                if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                {
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, 0f);
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, 0f);
                }
            }
        }

        // collide dust particles on the top
        if (node.Y <= start.Y)
        {
            Vector2 add = new(2f, 0f);
            for (int tileX = 0; tileX < Width / 8f; tileX++)
            {
                Vector2 collideAt = new(Left + 4f + (tileX * 8), Top - 1f);
                Vector2 noCollideAt = collideAt + Vector2.UnitY;
                if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                {
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, -(float) Math.PI / 2f);
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, -(float) Math.PI / 2f);
                }
            }
        }

        // collide dust particles on the bottom
        if (node.Y >= start.Y)
        {
            Vector2 add = new(2f, 0f);
            for (int tileX = 0; tileX < Width / 8f; tileX++)
            {
                Vector2 collideAt = new(Left + 4f + (tileX * 8), Bottom + 1f);
                Vector2 noCollideAt = collideAt - (Vector2.UnitY * 2f);
                if (Scene.CollideCheck<Solid>(collideAt) && !Scene.CollideCheck<Solid>(noCollideAt))
                {
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt + add, (float) Math.PI / 2f);
                    SceneAs<Level>().ParticlesFG.Emit(SwitchGate.P_Dust, collideAt - add, (float) Math.PI / 2f);
                }
            }
        }
        Collidable = collidableBackup;

        // moving is over
        Audio.Play(finishedSound, Position);
        StartShaking(0.2f);
        while (icon.Rate > 0f)
        {
            icon.Color = Color.Lerp(activeColor, toColor, 1f - icon.Rate);
            icon.Rate -= Engine.DeltaTime * 4f;
            yield return null;
            if (ShouldCancelMove(goingBack))
                yield break;
        }
        icon.Rate = 0f;
        icon.SetAnimationFrame(0);
        wiggler.Start();

        // emit fire particles if the block is not behind a solid.
        collidableBackup = Collidable;
        Collidable = false;
        if (!Scene.CollideCheck<Solid>(Center))
        {
            for (int i = 0; i < 32; i++)
            {
                float angle = Calc.Random.NextFloat((float) Math.PI * 2f);
                SceneAs<Level>().ParticlesFG.Emit(goingBack ? P_RecoloredFireBack : P_RecoloredFire, Position + iconOffset + Calc.AngleToVector(angle, 4f), angle);
            }
        }
        Collidable = collidableBackup;
    }

    private bool ShouldCancelMove(bool goingBack, Tween tween = null)
    {
        if (allowReturn && SceneAs<Level>().Session.GetFlag(Flag) == goingBack)
        {
            // whoops, the flag changed too fast! we need to backtrack.
            if (tween != null)
            {
                Remove(tween);
            }

            icon.Rate = 0f;
            icon.SetAnimationFrame(0);
            return true;
        }
        return false;
    }

    public static void InitializeParticles()
    {
        P_BehindDreamParticles = new ParticleType[4];
        // Color Codes : FFEF11, FF00D0, 08a310, 5fcde4, 7fb25e, E0564C, 5b6ee1, CC3B3B

        ParticleType particle = new(SwitchGate.P_Behind)
        {
            ColorMode = ParticleType.ColorModes.Choose
        };
        for (int i = 0; i < 4; i++)
        {
            P_BehindDreamParticles[i] = new ParticleType(particle);
        }
    }

    public override void SetupCustomParticles(float canvasWidth, float canvasHeight)
    {
        base.SetupCustomParticles(canvasWidth, canvasHeight);
        if (PlayerHasDreamDash)
        {
            P_BehindDreamParticles[0].Color = Calc.HexToColor("FFEF11");
            P_BehindDreamParticles[0].Color2 = Calc.HexToColor("FF00D0");

            P_BehindDreamParticles[1].Color = Calc.HexToColor("08a310");
            P_BehindDreamParticles[1].Color2 = Calc.HexToColor("5fcde4");

            P_BehindDreamParticles[2].Color = Calc.HexToColor("7fb25e");
            P_BehindDreamParticles[2].Color2 = Calc.HexToColor("E0564C");

            P_BehindDreamParticles[3].Color = Calc.HexToColor("5b6ee1");
            P_BehindDreamParticles[3].Color2 = Calc.HexToColor("CC3B3B");
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                P_BehindDreamParticles[i].Color = Color.LightGray * 0.5f;
                P_BehindDreamParticles[i].Color2 = Color.LightGray * 0.75f;
            }
        }
    }
}
