#nullable enable

using Celeste.Mod.CommunalHelper.Entities;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Directions = Celeste.MoveBlock.Directions;

namespace Celeste.Mod.CommunalHelper.Components;

[Tracked(true)]
public class MoveBlockRedirectable : Redirectable
{
    // Pre-initialize this with some known types
    private static readonly Dictionary<Type, FieldInfo> reflectionCache = new()
    {
        { typeof(MoveBlock), typeof(MoveBlock).GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>")).GetField("<>4__this") },
        { typeof(DreamMoveBlock), typeof(DreamMoveBlock).GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>")).GetField("<>4__this") },
        { typeof(CassetteMoveBlock), typeof(CassetteMoveBlock).GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>")).GetField("<>4__this") },
        { typeof(MoveSwapBlock), typeof(MoveSwapBlock).GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>")).GetField("<>4__this") },
    };

    private float initialAngle;
    private Directions initialDirection;

    public MoveBlockRedirectable(DynamicData Data, Func<bool> canSteer = null, Func<Directions> get_Direction = null, Action<Directions> set_Direction = null) : base(Data)
    {
        IsRedirectable = !(canSteer?.Invoke() ?? Data.Get<bool>("canSteer"));
        Get_Direction = get_Direction;
        Set_Direction = set_Direction;
        initialAngle = Angle;
        initialDirection = Direction;
        OnResumeAction = GetControllerDelegate(Data, 3);
        OnBreakAction = GetControllerDelegate(Data, 4);
    }

    public Action<Coroutine> OnResumeAction;
    public Action<Coroutine> OnBreakAction;
    public Func<float>? Get_Speed = null;
    public Action<float>? Set_Speed = null;

    public override float Speed
    {
        get => Get_Speed?.Invoke() ?? Data.Get<float>("speed");
        set
        {
            if (Set_Speed != null)
                Set_Speed.Invoke(value);
            else
                Data.Set("speed", value);
        }
    }


    public Func<float>? Get_TargetSpeed = null;
    public Action<float>? Set_TargetSpeed = null;
    public override float TargetSpeed
    {
        get => Get_TargetSpeed?.Invoke() ?? Data.Get<float>("targetSpeed");
        set
        {
            if (Set_TargetSpeed != null)
                Set_TargetSpeed.Invoke(value);
            else
                Data.Set("targetSpeed", value);
        }
    }

    public Func<Directions>? Get_Direction = null;
    public Action<Directions>? Set_Direction = null;

    public override Directions Direction
    {
        get => Get_Direction?.Invoke() ?? Data.Get<Directions>("direction");
        set
        {
            if (Set_Direction != null)
                Set_Direction.Invoke(value);
            else
                Data.Set("direction", value);
        }
    }


    public override float Angle
    {
        get => Data.Get<float>("angle"); set
        {
            Data.Set("angle", value);
            Data.Set("homeAngle", value);
            Data.Set("targetAngle", value);
        }
    }

    public Func<SoundSource>? Get_MoveSfx = null;
    public SoundSource MoveSfx => Get_MoveSfx?.Invoke() ?? Data.Get<SoundSource>("moveSfx");

    public static Action<Coroutine> GetControllerDelegate(DynamicData targetData, int jumpPoint)
    {
        Type t_Controller;
        if (!reflectionCache.TryGetValue(targetData.TargetType, out FieldInfo f_Controller_this))
        {
            t_Controller = targetData.TargetType.GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>"));
            f_Controller_this = t_Controller.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
            reflectionCache[targetData.TargetType] = f_Controller_this;
        }
        else
            t_Controller = f_Controller_this.DeclaringType;

        return orig =>
        {
            IEnumerator controller;
            orig.Replace(controller = (IEnumerator) Activator.CreateInstance(t_Controller, jumpPoint));
            f_Controller_this.SetValue(controller, targetData.Target);
        };
    }


    public override void ResetBlock()
    {
        Angle = initialAngle;
        Direction = initialDirection;
    }

    public override void MoveTo(Vector2 to)
    {
        (Entity as Platform)?.MoveTo(to);
    }

    public override void OnPause(Coroutine moveCoroutine)
    {
        MoveSfx?.Param("redirect_slowdown", 1f);
        (Entity as Platform)?.StartShaking(0.2f);
    }

    public override void OnResume(Coroutine moveCoroutine)
    {
        OnResumeAction(moveCoroutine);
    }

    public override void OnBreak(Coroutine moveCoroutine)
    {
        OnBreakAction(moveCoroutine);
    }

    public override void BeforeBreakAnimation()
    {
        MoveSfx?.Stop();
    }

    public override void BeforeResumeAnimation()
    {
        (Entity as Platform)?.StartShaking(0.18f);
        MoveSfx?.Param("redirect_slowdown", 0f);
    }

    internal static void Load()
    {
        On.Celeste.MoveBlock.ctor_Vector2_int_int_Directions_bool_bool += MoveBlock_ctor_Vector2_int_int_Directions_bool_bool;
        On.Celeste.MoveBlock.BreakParticles += MoveBlock_BreakParticles;
    }

    internal static void Unload()
    {
        On.Celeste.MoveBlock.ctor_Vector2_int_int_Directions_bool_bool -= MoveBlock_ctor_Vector2_int_int_Directions_bool_bool;
    }

    private static void MoveBlock_ctor_Vector2_int_int_Directions_bool_bool(On.Celeste.MoveBlock.orig_ctor_Vector2_int_int_Directions_bool_bool orig, MoveBlock self, Vector2 position, int width, int height, Directions direction, bool canSteer, bool fast)
    {
        orig(self, position, width, height, direction, canSteer, fast);
        if (self.GetType() == typeof(MoveBlock))
            self.Add(new MoveBlockRedirectable(new DynamicData(typeof(MoveBlock), self)));
    }

    private static void MoveBlock_BreakParticles(On.Celeste.MoveBlock.orig_BreakParticles orig, MoveBlock self)
    {
        orig(self);
        ((MoveBlockRedirectable) self.Get<Redirectable>())?.ResetBlock();
    }
}
