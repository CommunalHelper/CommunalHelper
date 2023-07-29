#nullable enable

using Celeste;
using Celeste.Mod.CommunalHelper.Entities;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Directions = Celeste.MoveBlock.Directions;

namespace Celeste.Mod.CommunalHelper.Components;

/// <summary>
/// get/set
///     homeAngle
///     direction
///     targetSpeed
///     speed
/// get
///     canSteer
///     moveSfx
/// set
///     targetAngle
///     angle
/// </summary>
[Tracked]
public class Redirectable : Component
{
    // Pre-initialize this with some known types
    private static readonly Dictionary<Type, FieldInfo> reflectionCache = new()
    {
        { typeof(MoveBlock), typeof(MoveBlock).GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>")).GetField("<>4__this") },
        { typeof(DreamMoveBlock), typeof(DreamMoveBlock).GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>")).GetField("<>4__this") },
        { typeof(CassetteMoveBlock), typeof(CassetteMoveBlock).GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>")).GetField("<>4__this") },
        { typeof(MoveSwapBlock), typeof(MoveSwapBlock).GetNestedTypes(BindingFlags.NonPublic).First(t => t.Name.StartsWith("<Controller>")).GetField("<>4__this") },
    };

    public Func<float>? Get_Speed = null;
    public Action<float>? Set_Speed = null;
    public float Speed
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
    public float TargetSpeed
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
    public Directions Direction
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

    public Func<float>? Get_HomeAngle = null;
    public Action<float>? Set_HomeAngle = null;
    public float HomeAngle
    {
        get => Get_HomeAngle?.Invoke() ?? Data.Get<float>("homeAngle");
        set
        {
            if (Set_HomeAngle != null)
                Set_HomeAngle.Invoke(value);
            else
                Data.Set("homeAngle", value);
        }
    }

    public Func<bool>? Get_CanSteer = null;
    public bool CanSteer => Get_CanSteer?.Invoke() ?? Data.Get<bool>("canSteer");

    public Func<SoundSource>? Get_MoveSfx = null;
    public SoundSource MoveSfx => Get_MoveSfx?.Invoke() ?? Data.Get<SoundSource>("moveSfx");

    public Action<float>? Set_TargetAngle = null;
    public float TargetAngle
    {
        set
        {
            if (Set_TargetAngle != null)
                Set_TargetAngle(value);
            else
                Data.Set("targetAngle", value);
        }
    }

    public Action<float>? Set_Angle = null;
    public float Angle
    {
        set
        {
            if (Set_Angle != null)
                Set_Angle(value);
            else
                Data.Set("angle", value);
        }
    }

    public float? InitialAngle;
    public Directions? InitialDirection;

    public Action<float>? OnStartShaking;
    public Action<Vector2>? OnMoveTo;

    public Action<Coroutine> OnResume;
    public Action<Coroutine> OnBreak;

    public DynamicData Data;

    public Redirectable(DynamicData entityData, Action<Coroutine>? onResume = null, Action<Coroutine>? onBreak = null)
        : base(false, false)
    {
        Data = entityData;
        OnResume = onResume ?? GetControllerDelegate(entityData, 3);
        OnBreak = onBreak ?? GetControllerDelegate(entityData, 4);
    }

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

    public void StartShaking(float time = 0)
    {
        if (OnStartShaking != null)
            OnStartShaking.Invoke(time);
        else
            (Entity as Platform)?.StartShaking(time);
    }

    public void MoveTo(Vector2 position)
    {
        if (OnMoveTo != null)
            OnMoveTo.Invoke(position);
        else
            (Entity as Platform)?.MoveTo(position);
    }

    public void ResetBlock()
    {
        if (InitialAngle != null)
            Angle = TargetAngle = HomeAngle = InitialAngle.Value;
        if (InitialDirection != null)
            Direction = InitialDirection.Value;
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
            self.Add(new Redirectable(new DynamicData(typeof(MoveBlock), self)));
    }

    private static void MoveBlock_BreakParticles(On.Celeste.MoveBlock.orig_BreakParticles orig, MoveBlock self)
    {
        orig(self);
        self.Get<Redirectable>()?.ResetBlock();
    }

}
