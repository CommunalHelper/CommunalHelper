using Celeste.Mod.CommunalHelper.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Directions = Celeste.MoveBlock.Directions;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/MoveBlockRedirect")]
public class MoveBlockRedirect : Entity
{
    public enum Operations
    {
        Add, Subtract, Multiply
    }
    public Operations Operation;
    public float Modifier;

    private ParticleType p_Used;

    internal const string MoveBlock_InitialAngle = "communalHelperInitialAngle";
    internal const string MoveBlock_InitialDirection = "communalHelperInitialDirection";

    public static readonly Color Mask = new(200, 180, 190);
    public static readonly Color UsedColor = Calc.HexToColor("474070"); // From MoveBlock
    public static readonly Color DeleteColor = Calc.HexToColor("cc2541");
    public static readonly Color DefaultColor = Calc.HexToColor("fbce36");
    public static readonly Color FasterColor = Calc.HexToColor("29c32f");
    public static readonly Color SlowerColor = Calc.HexToColor("1c5bb3");

    private Color startColor;
    private Color? overrideColor, overrideUsedColor;

    public Directions Direction;
    public bool FastRedirect;
    public bool OneUse, DeleteBlock;

    private readonly float angle;
    private float maskAlpha, alpha = 1f;
    private List<Image> borders;

    private Redirectable lastMoveBlock;

    private readonly string reskinFolder;
    private Icon icon;

    public MoveBlockRedirect(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        Depth = Depths.Above;
        Collider = new Hitbox(data.Width, data.Height);

        FastRedirect = data.Bool("fastRedirect");
        OneUse = data.Bool("oneUse");
        DeleteBlock = data.Bool("deleteBlock") || (Operation == Operations.Multiply && Modifier == 0f);

        Operation = data.Enum("operation", Operations.Add);
        Modifier = Math.Abs(data.Float("modifier"));

        overrideColor = data.HexColorNullable("overrideColor");
        overrideUsedColor = data.HexColorNullable("overrideUsedColor");

        if (float.TryParse(data.Attr("direction"), out float fAngle))
            angle = fAngle;
        else
        {
            Direction = data.Enum<Directions>("direction");
            angle = Direction.Angle();
        }

        string str = data.Attr("reskinFolder").Trim().TrimEnd('/');
        reskinFolder = str == "" ? null : "objects/" + str;

        AddTextures();
    }

    private void AddTextures()
    {
        borders = new List<Image>();

        MTexture defaultTexture = GFX.Game["objects/CommunalHelper/moveBlockRedirect/block"];
        MTexture block;
        if (reskinFolder == null)
        {
            block = defaultTexture;
        }
        else
        {
            GFX.Game.PushFallback(defaultTexture);
            block = GFX.Game[reskinFolder + "/block"];
            GFX.Game.PopFallback();
        }

        int w = (int) (Width / 8f);
        int h = (int) (Height / 8f);
        for (int i = -1; i <= w; i++)
        {
            for (int j = -1; j <= h; j++)
            {
                int tx = (i == -1) ? 0 : ((i == w) ? 16 : 8);
                int ty = (j == -1) ? 0 : ((j == h) ? 16 : 8);
                AddImage(block.GetSubtexture(tx, ty, 8, 8), new Vector2(i, j) * 8, borders);
            }
        }

        // Unused in favor of large arrow
        /*
        int x = 8;
        for (int y = 8; y <= Height; y += 8) {
            for (; x <= Width; x += 16) {
                Image image = new Image(GFX.Game["objects/CommunalHelper/moveBlockRedirect/arrow"]);
                image.Position = new Vector2(x, y);
                image.Color = new Color(100, 80, 120) * 0.5f;
                image.Rotation = angle;
                arrows.Add(image);
                Add(image);
            }
            x = ((y / 8) % 2 == 0) ? 8 : 16;
        }
        */

    }

    private void AddImage(MTexture texture, Vector2 position, List<Image> addTo)
    {
        Image image = new(texture)
        {
            Position = position
        };
        Add(image);
        addTo?.Add(image);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        string iconTexture = "arrow";
        startColor = DefaultColor;

        if (DeleteBlock)
        {
            iconTexture = "x";
            startColor = DeleteColor;
        }
        else
        {
            if ((Operation == Operations.Add && Modifier != 0f) || (Operation == Operations.Multiply && Modifier > 1f))
            {
                iconTexture = "fast";
                startColor = FasterColor;
            }
            else if ((Operation == Operations.Subtract && Modifier != 0f) || (Operation == Operations.Multiply && Modifier < 1f))
            {
                iconTexture = "slow";
                startColor = SlowerColor;
            }
        }
        scene.Add(icon = new Icon(Center, angle, iconTexture, reskinFolder));
        UpdateAppearance();
        p_Used = new ParticleType(SwitchGate.P_Behind)
        {
            Color = icon.Sprite.Color,
            Color2 = Color.Lerp(icon.Sprite.Color, Color.White, .2f),
            DirectionRange = (float) Math.PI * 0.5f,
            SpeedMax = 20f
        };
    }

    private void UpdateAppearance()
    {
        Color currentColor = Color.Lerp(overrideColor ?? startColor, overrideUsedColor ?? UsedColor, maskAlpha) * alpha;
        icon.Sprite.Color = currentColor;
        foreach (Image image in borders)
        {
            image.Color = currentColor * alpha;
        }
    }

    public override void Update()
    {
        base.Update();
        UpdateAppearance();

        if (lastMoveBlock != null && !CollideCheck(lastMoveBlock.Entity))
            lastMoveBlock = null;
        else if ((lastMoveBlock == null || FastRedirect) && maskAlpha != 0f)
        {
            maskAlpha = Calc.Approach(maskAlpha, 0f, (FastRedirect && !DeleteBlock ? 2.5f : 4f) * Engine.DeltaTime);
        }

        Redirectable redirectable = Scene.Tracker.GetComponents<Redirectable>().FirstOrDefault(c => CollideCheck(c.Entity)) as Redirectable;
        Entity block = redirectable?.Entity; // Non-null if redirectible isn't null

        if (Collidable && redirectable != null && redirectable != lastMoveBlock && !redirectable.CanSteer &&
            block.Width == Width && block.Height == Height)
        {

            if (!Collider.Contains(block.Collider, 0.001f))
            {
                Directions dir = redirectable.Direction;
                Vector2 prevPosOffset = -dir.Vector(redirectable.Speed);

                float edgeMin;
                float edgeMax;
                bool wentThrough = false;
                if (dir is Directions.Down or Directions.Up)
                {
                    edgeMin = Math.Min(block.Top, block.Top + prevPosOffset.Y);
                    edgeMax = Math.Max(block.Bottom, block.Bottom + prevPosOffset.Y);
                    wentThrough = X == block.X && edgeMin <= Top && edgeMax >= Bottom;
                }
                else
                {
                    edgeMin = Math.Min(block.Left, block.Left + prevPosOffset.X);
                    edgeMax = Math.Max(block.Right, block.Right + prevPosOffset.X);
                    wentThrough = Y == block.Y && edgeMin <= Left && edgeMax >= Right;
                }

                if (!wentThrough)
                    return;
            }

            lastMoveBlock = redirectable;

            if (DeleteBlock)
            {
                Coroutine routine = block.Get<Coroutine>();
                block.Remove(routine);
                block.Add(new Coroutine(BreakBlock(redirectable, routine, FastRedirect, OneUse)));
            }
            else
            {
                if (FastRedirect)
                {
                    SetBlockData(redirectable);
                    maskAlpha = 1f;
                    if (OneUse)
                        Disappear();
                }
                else
                {
                    Coroutine routine = block.Get<Coroutine>();
                    block.Remove(routine);
                    Add(new Coroutine(RedirectRoutine(redirectable, routine)));
                }
            }
        }
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        icon.RemoveSelf();
    }

    private void SetBlockData(Redirectable redirectable)
    {
        redirectable.InitialAngle ??= redirectable.HomeAngle;
        redirectable.InitialDirection ??= redirectable.Direction;

        redirectable.Angle = redirectable.TargetAngle = redirectable.HomeAngle = angle;
        redirectable.Direction = Direction;

        float newSpeed = Operation.ApplyTo(redirectable.TargetSpeed, Modifier);

        redirectable.TargetSpeed = Math.Max(10f, newSpeed); // could go into negative speeds, yuck
        lastMoveBlock = redirectable;
        redirectable.MoveTo(Position);

        UsedParticles();
    }

    private IEnumerator BreakBlock(Redirectable redirectable, Coroutine orig, bool fast, bool oneUse)
    {
        redirectable.MoveTo(Position);
        redirectable.MoveSfx.Stop();

        //state = MovementState.Breaking;
        redirectable.Speed = redirectable.TargetSpeed = 0f;
        redirectable.Angle = redirectable.TargetAngle = redirectable.HomeAngle;

        redirectable.StartShaking(0.2f);
        //redirectable.Entity.StopPlayerRunIntoAnimation = true; // Unused in Vanilla so we ignore it

        if (fast)
        {
            maskAlpha = 1f;
            Audio.Play(CustomSFX.game_redirectMoveBlock_arrowblock_break_fast, redirectable.Entity.Position);
        }
        else
        {
            float duration = 0.15f;
            float timer = 0f;
            yield return .2f;
            Audio.Play(SFX.game_04_arrowblock_break, redirectable.Entity.Position);
            while (timer < duration)
            {
                timer += Engine.DeltaTime;
                maskAlpha = Ease.SineIn(timer / duration);
                yield return null;
            }
        }

        UsedParticles();

        // Absolutely cursed beyond belief

        redirectable.OnBreak(orig);
        redirectable.Entity.Add(orig);

        yield return null;

        if (oneUse)
            Disappear();
    }

    private IEnumerator RedirectRoutine(Redirectable redirectable, Coroutine orig)
    {
        Entity block = redirectable.Entity;
        float duration = 1f;

        redirectable.MoveTo(Position);

        SoundSource moveSfx = redirectable.MoveSfx;
        moveSfx.Param("redirect_slowdown", 1f);

        redirectable.StartShaking(0.2f);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Engine.DeltaTime;
            maskAlpha = Ease.BounceIn(timer / duration);
            yield return null;
        }

        SetBlockData(redirectable);

        while (timer > 0.2f)
        {
            timer -= Engine.DeltaTime;
            float percent = timer / duration;
            maskAlpha = Ease.BounceIn(percent);
            yield return null;
        }

        redirectable.StartShaking(0.18f);
        moveSfx.Param("redirect_slowdown", 0f);

        while (timer > 0)
        {
            timer -= Engine.DeltaTime;
            maskAlpha = Ease.BounceIn(timer / duration);
            yield return null;
        }

        // Absolutely cursed, starts the Controller routine after a certain number of yields
        redirectable.OnResume(orig);
        redirectable.Entity.Add(orig);

        // Wait for the moveblock to continue before resetting
        if (OneUse)
            Disappear();
    }

    private void Disappear()
    {
        Collidable = false;
        Add(new Coroutine(DisappearRoutine()));
    }

    private IEnumerator DisappearRoutine()
    {
        float timer = 1f;
        while (timer > 0f)
        {
            timer -= Engine.DeltaTime * 2f;
            alpha = timer;
            yield return null;
        }
        RemoveSelf();
    }

    private void UsedParticles()
    {
        Level level = SceneAs<Level>();

        for (int i = 0; i < Height / 8; i++)
        {
            level.Particles.Emit(p_Used, new Vector2(Left + 1f, Calc.Random.Range(Top + 3f, Bottom - 3f)), (float) Math.PI);
            level.Particles.Emit(p_Used, new Vector2(Right - 1f, Calc.Random.Range(Top + 3f, Bottom - 3f)), 0f);
        }
        for (int i = 0; i < Width / 8; i++)
        {
            level.Particles.Emit(p_Used, new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Top + 1f), -(float) Math.PI / 2f);
            level.Particles.Emit(p_Used, new Vector2(Calc.Random.Range(Left + 3f, Right - 3f), Bottom - 1f), (float) Math.PI / 2f);
        }
    }

    public override void Render()
    {
        Draw.Rect(X - 1, Y - 1, Width + 2, Height + 2, Mask * maskAlpha);
        base.Render();
    }

    private class Icon : Entity
    {
        public Image Sprite;
        public Icon(Vector2 position, float rotation, string iconName, string reskinFolder)
            : base(position)
        {
            Depth = Depths.Below;

            MTexture defaultTexture = GFX.Game["objects/CommunalHelper/moveBlockRedirect/" + iconName];
            MTexture icon;
            if (reskinFolder == null)
            {
                icon = defaultTexture;
            }
            else
            {
                GFX.Game.PushFallback(defaultTexture);
                icon = GFX.Game[$"{reskinFolder}/{iconName}"];
                GFX.Game.PopFallback();
            }
            Add(Sprite = new Image(icon));

            Sprite.CenterOrigin();
            Sprite.Rotation = rotation;
        }
    }

    #region Hooks

    private static IDetour hook_MoveBlock_Controller;

    internal static void Load()
    {
        IL.Celeste.Solid.GetPlayerOnTop += Solid_GetPlayerOnTop;
        hook_MoveBlock_Controller = new ILHook(typeof(MoveBlock).GetMethod("Controller", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
            MoveBlock_Controller);
    }

    internal static void Unload()
    {
        IL.Celeste.Solid.GetPlayerOnTop -= Solid_GetPlayerOnTop;
        hook_MoveBlock_Controller.Dispose();
    }

    // what the fuck
    private static void Solid_GetPlayerOnTop(ILContext il) { }

    private static void MoveBlock_Controller(ILContext il)
    {
        ILCursor cursor = new(il);

        Logger.Log("CommunalHelper", "Replacing MoveBlock SFX with near-identical custom event that supports a \"redirect_slowdown\" param");
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdstr(SFX.game_04_arrowblock_move_loop)))
        {
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldstr, CustomSFX.game_redirectMoveBlock_arrowblock_move);
        }
    }

    #endregion

}
