using Celeste.Mod.CommunalHelper.Components;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

internal class CrushBlockRedirectable : Redirectable
{
    private const float CRASH_BLOCK_DEFAULT_MAX_SPEED = 240f;
    public override float Speed { get; set; } = 0;
    public override float TargetSpeed { get; set; } = CRASH_BLOCK_DEFAULT_MAX_SPEED;

    private bool isStuck = false;


    private static readonly ConstructorInfo MoveState_Constructor =
        typeof(CrushBlock).GetNestedType("MoveState", BindingFlags.NonPublic).GetConstructor(new Type[]
        {
            typeof(Vector2), typeof(Vector2)
        });

    private static readonly MethodInfo m_AttackSequence =
        typeof(CrushBlock).GetMethod("AttackSequence", BindingFlags.Instance | BindingFlags.NonPublic).GetStateMachineTarget();

    public CrushBlockRedirectable(DynamicData Data) : base(Data)
    {
        CanRedirect = false;
    }

    private void Redirect(Vector2 newDirection)
    {
        Vector2 oldDirection = Data.Get<Vector2>("crushDir");

        Data.Set("crushDir", newDirection);
        Audio.Play("event:/game/06_reflection/crushblock_impact", Entity.Center);
        Audio.Play("event:/game/06_reflection/crushblock_activate", Entity.Center);
        Data.Get<Sprite>("face").Play("hit");
        Data.Invoke("ClearRemainder");
        Data.Invoke("TurnOffImages");

        string newFaceDirection = null;
        List<Image> imagesToActivate = null;

        if (newDirection.X < 0)
        {
            newFaceDirection = "left";
            imagesToActivate = Data.Get<List<Image>>("activeLeftImages");
        }
        else if (newDirection.X > 0)
        {
            newFaceDirection = "right";
            imagesToActivate = Data.Get<List<Image>>("activeRightImages");
        }
        else if (newDirection.Y < 0)
        {
            newFaceDirection = "up";
            imagesToActivate = Data.Get<List<Image>>("activeTopImages");
        }
        else if (newDirection.Y > 0)
        {
            newFaceDirection = "down";
            imagesToActivate = Data.Get<List<Image>>("activeBottomImages");
        }

        if (newFaceDirection != null)
        {
            Data.Set("nextFaceDirection", newFaceDirection);
        }
        if (imagesToActivate != null)
        {
            foreach (Image activeImage in imagesToActivate)
            {
                activeImage.Visible = true;
            }
        }


        if (oldDirection != newDirection && oldDirection != -newDirection)
        {
            object newMoveState = MoveState_Constructor.Invoke(new object[] { Entity.Position, newDirection });
            IList returnStack = Data.Get<IList>("returnStack");
            returnStack.Add(newMoveState);
        }
    }

    public override MoveBlock.Directions Direction
    {
        get => Data.Get<Vector2>("crushDir").Direction();
        set => Redirect(value.Vector());
    }


    // we don't care about angle in crush block leave defaulted
    public override float Angle { get; set; }

    public override bool CanSteer => false;

    public override void MoveTo(Vector2 to)
    {

        (Entity as Platform)?.MoveTo(to);
    }

    public override void OnBreak(Coroutine moveCoroutine)
    {
        Data.Get<Sprite>("face").Play("idle");
        Data.Invoke("ClearRemainder");
        Data.Invoke("TurnOffImages");
        Audio.Play("event:/game/06_reflection/crushblock_impact", Entity.Center);
        Data.Get<SoundSource>("currentMoveLoopSfx")?.Param("end", 1f);
        CanRedirect = false;
        isStuck = true;
        moveCoroutine.Cancel();
    }

    protected override float GetInitialAngle()
    {
        return 0;
    }

    protected override MoveBlock.Directions GetInitialDirection()
    {
        return Direction;
    }


    private static ILHook hook_CrashBlock_AttackSequence;

    public static void Load()
    {
        hook_CrashBlock_AttackSequence = new(m_AttackSequence, CrashBlock_AttackSequence);
        On.Celeste.CrushBlock.OnDashed += CrushBlock_OnDashed;
        On.Celeste.CrushBlock.Attack += CrushBlock_Attack;
        On.Celeste.CrushBlock.MoveHCheck += CrushBlock_MoveHCheck;
        On.Celeste.CrushBlock.MoveVCheck += CrushBlock_MoveVCheck;
        On.Celeste.CrushBlock.ctor_Vector2_float_float_Axes_bool += CrushBlock_ctor;
    }

    public static void Unload()
    {
        hook_CrashBlock_AttackSequence?.Dispose();
        hook_CrashBlock_AttackSequence = null;
        On.Celeste.CrushBlock.OnDashed -= CrushBlock_OnDashed;
        On.Celeste.CrushBlock.Attack -= CrushBlock_Attack;
        On.Celeste.CrushBlock.MoveHCheck -= CrushBlock_MoveHCheck;
        On.Celeste.CrushBlock.MoveVCheck -= CrushBlock_MoveVCheck;
        On.Celeste.CrushBlock.ctor_Vector2_float_float_Axes_bool -= CrushBlock_ctor;
    }

    public static void CrushBlock_ctor(On.Celeste.CrushBlock.orig_ctor_Vector2_float_float_Axes_bool orig,
                                       CrushBlock self,
                                       Vector2 position,
                                       float width,
                                       float height,
                                       CrushBlock.Axes axes,
                                       bool chillOut)
    {
        orig(self, position, width, height, axes, chillOut);
        if (self.GetType() == typeof(CrushBlock))
            self.Add(new CrushBlockRedirectable(new DynamicData(self)));
    }

    static private void CrashBlock_AttackSequence(ILContext context)
    {
        ILCursor cursor = new(context);
        FieldReference f_speed = null;

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(CRASH_BLOCK_DEFAULT_MAX_SPEED)))
        {
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate((float maxSpeed, CrushBlock crushBlock) =>
            {
                CrushBlockRedirectable redirectable = (CrushBlockRedirectable) crushBlock.Components.Get<Redirectable>();
                return redirectable?.TargetSpeed ?? maxSpeed;
            });
        }

        cursor.Index = 0;

        cursor.GotoNext(MoveType.Before, instr => instr.MatchLdcR4(0), instr => instr.MatchStfld(out f_speed));

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld(f_speed)))
        {
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate((float speed, CrushBlock crushBlock) =>
            {
                CrushBlockRedirectable redirectable = (CrushBlockRedirectable) crushBlock.Components.Get<Redirectable>();
                return redirectable?.Speed ?? speed;
            });
        }

        cursor.Index = 0;

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld(f_speed)))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_speed);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate((float speed, CrushBlock crushBlock) =>
            {
                CrushBlockRedirectable redirectable = (CrushBlockRedirectable) crushBlock.Components.Get<Redirectable>();
                if (redirectable != null)
                {
                    redirectable.Speed = speed;
                }
            });
        }

    }

    private static DashCollisionResults CrushBlock_OnDashed(On.Celeste.CrushBlock.orig_OnDashed orig, CrushBlock self, Player player, Vector2 direction)
    {
        CrushBlockRedirectable redirectable = (CrushBlockRedirectable) self.Components.Get<Redirectable>();
        if (redirectable != null && redirectable.isStuck)
        {
            return DashCollisionResults.NormalCollision;
        }
        return orig(self, player, direction);
    }


    private static void CrushBlock_Attack(On.Celeste.CrushBlock.orig_Attack orig, CrushBlock self, Vector2 direction)
    {
        CrushBlockRedirectable redirectable = (CrushBlockRedirectable) self.Components.Get<Redirectable>();
        if (redirectable != null)
        {
            if (redirectable.isStuck)
            {
                return;
            }

            redirectable.CanRedirect = true;
        }
        orig(self, direction);
    }

    private static bool CrushBlock_MoveHCheck(On.Celeste.CrushBlock.orig_MoveHCheck orig, CrushBlock self, float amount)
    {
        bool result = orig(self, amount);
        if (result)
        {
            OnWallCoalition(self);
        }

        return result;
    }

    private static bool CrushBlock_MoveVCheck(On.Celeste.CrushBlock.orig_MoveVCheck orig, CrushBlock self, float amount)
    {
        bool result = orig(self, amount);
        if (result)
        {
            OnWallCoalition(self);
        }

        return result;
    }

    private static void OnWallCoalition(CrushBlock self)
    {
        Redirectable redirectable = self.Components.Get<Redirectable>();

        if (redirectable != null)
        {
            redirectable.CanRedirect = false;
        }
    }
}
