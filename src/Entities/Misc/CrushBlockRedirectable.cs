using Celeste.Mod.CommunalHelper.Components;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

internal class CrushBlockRedirectable : Redirectable
{
    private const float CRASH_BLOCK_DEFAULT_MAX_SPEED = 240f;
    public float currentSpeed = 0;
    public float maxSpeed = CRASH_BLOCK_DEFAULT_MAX_SPEED;

    private static readonly ConstructorInfo MoveState_Contractor = typeof(CrushBlock).GetNestedType("MoveState", BindingFlags.NonPublic).GetConstructors()[0];
    private static readonly MethodInfo m_AttackSequence =
        typeof(CrushBlock).GetMethod("AttackSequence", BindingFlags.Instance | BindingFlags.NonPublic).GetStateMachineTarget();

    public CrushBlockRedirectable(DynamicData entityData) : base(entityData, (_) => { }, (_) => { })
    {
        OnBreak = (_) => { _.Cancel(); StopInPlace(); };
        Get_Speed = () => currentSpeed;
        Set_Speed = (float speed) =>
        {
            currentSpeed = speed;
        };
        Get_TargetSpeed = () => maxSpeed;
        Set_TargetSpeed = (maxSpeed) =>
        {
            this.maxSpeed = maxSpeed;
        };
        Get_Direction = () =>
            {
                Vector2 direction = Data.Get<Vector2>("crushDir");
                if (direction.X > 0)
                {
                    return MoveBlock.Directions.Right;
                }
                else if (direction.X < 0)
                {
                    return MoveBlock.Directions.Left;
                }
                else if (direction.Y > 0)
                {
                    return MoveBlock.Directions.Down;
                }
                else
                {
                    return MoveBlock.Directions.Up;
                }
            };
        Set_Direction = (direction) => Redirect(direction.Vector());
        Get_HomeAngle = () => 0;
        Set_HomeAngle = (_) => { };
        Get_CanSteer = () => false;
        Get_MoveSfx = () => entityData.Get<SoundSource>("returnLoopSfx");
        Set_TargetAngle = (_) => { };
        Set_Angle = (_) => { };
    }

    private void StopInPlace()
    {
        Data.Get<Sprite>("face").Play("idle");
        Data.Invoke("ClearRemainder");
        Data.Invoke("TurnOffImages");
        Audio.Play("event:/game/06_reflection/crushblock_impact", Entity.Center);
        Data.Get<SoundSource>("currentMoveLoopSfx")?.Param("end", 1f);
    }

    private void Redirect(Vector2 newDirection)
    {
        Vector2 oldDirection = Data.Get<Vector2>("crushDir");

        Data.Set("crushDir", newDirection);

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
            object newMoveState = MoveState_Contractor.Invoke(new object[] { Entity.Position, newDirection });
            IList returnStack = Data.Get<IList>("returnStack");
            returnStack.Add(newMoveState);
        }
    }



    private static ILHook hook_CrashBlock_AttackSequence;
    public static new void Load()
    {
        hook_CrashBlock_AttackSequence = new(m_AttackSequence, CrashBlock_AttackSequence);
        On.Celeste.CrushBlock.Attack += CrushBlock_Attack;
        On.Celeste.CrushBlock.MoveHCheck += CrushBlock_MoveHCheck;
        On.Celeste.CrushBlock.MoveVCheck += CrushBlock_MoveVCheck;
    }

    public static new void Unload()
    {
        hook_CrashBlock_AttackSequence?.Dispose();
        hook_CrashBlock_AttackSequence = null;
        On.Celeste.CrushBlock.Attack -= CrushBlock_Attack;
        On.Celeste.CrushBlock.MoveHCheck -= CrushBlock_MoveHCheck;
        On.Celeste.CrushBlock.MoveVCheck -= CrushBlock_MoveVCheck;
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
                return redirectable?.maxSpeed ?? CRASH_BLOCK_DEFAULT_MAX_SPEED;
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

    private static void CrushBlock_Attack(On.Celeste.CrushBlock.orig_Attack orig, CrushBlock self, Vector2 direction)
    {
        if (self.Components.Get<Redirectable>() == null)
        {
            self.Add(new CrushBlockRedirectable(new DynamicData(typeof(CrushBlock), self)));
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
            redirectable.RemoveSelf();
        }
    }
}
