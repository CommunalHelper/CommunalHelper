using System.Collections.Generic;

namespace Celeste.Mod.CommunalHelper.Entities;

// It's just a ConnectedDreamBlock that does the floaty thing
// Combining with ConnectedDreamBlocks gives a 50/50 chance it'll actually be floaty
[CustomEntity("CommunalHelper/DreamFloatySpaceBlock")]
public class DreamFloatySpaceBlock : ConnectedDreamBlock
{
    private Vector2 dashDirection;
    private float dashEase;

    private float sineWave;
    private float sinkTimer;
    private float yLerp;

    private Vector2 shake;

    public DreamFloatySpaceBlock(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        IncludeJumpThrus = true;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        TryToInitPosition();
    }

    private void TryToInitPosition()
    {
        if (MasterOfGroup)
        {
            foreach (ConnectedDreamBlock block in Group)
            {
                if (block is not DreamFloatySpaceBlock floatyBlock || !floatyBlock.awake)
                {
                    return;
                }
            }
            MoveToTarget();
            return;
        }
        (master as DreamFloatySpaceBlock)?.TryToInitPosition();
    }

    private void MoveToTarget()
    {
        float sine = (float)Math.Sin(sineWave) * 4f;
        Vector2 vector = Calc.YoYo(Ease.QuadIn(dashEase)) * dashDirection * 8f;
        for (int i = 0; i < 2; i++)
        {
            foreach (KeyValuePair<Platform, Vector2> keyValuePair in Moves)
            {
                Platform key = keyValuePair.Key;
                bool hasRider = false;
                JumpThru jumpThru = key as JumpThru;
                Solid solid = key as Solid;
                if ((jumpThru != null && jumpThru.HasRider()) || (solid != null && solid.HasRider()))
                {
                    hasRider = true;
                }

                if ((hasRider || i != 0) && (!hasRider || i != 1))
                {
                    Vector2 value = keyValuePair.Value;
                    float num2 = MathHelper.Lerp(value.Y, value.Y + 12f, Ease.SineInOut(yLerp)) + sine;
                    key.MoveToY(num2 + vector.Y);
                    key.MoveToX(value.X + vector.X);
                }
            }
        }
    }

    protected override DashCollisionResults OnDash(Player player, Vector2 dir)
    {
        if (!PlayerHasDreamDash)
        {
            if (MasterOfGroup && dashEase <= 0.2f)
            {
                dashEase = 1f;
                dashDirection = dir;
            }
            return DashCollisionResults.NormalOverride;
        }
        return DashCollisionResults.NormalCollision;
    }

    protected override DashCollisionResults OnDashJumpThru(Player player, Vector2 dir)
    {
        if (MasterOfGroup && dashEase <= 0.2f)
        {
            dashEase = 1f;
            dashDirection = dir;
        }
        return DashCollisionResults.NormalOverride;
    }

    public override void OnShake(Vector2 amount)
    {
        if (MasterOfGroup)
        {
            OnShake(amount);
            shake += amount;
            foreach (JumpThru jumpThru in JumpThrus)
            {
                foreach (Component component in jumpThru.Components)
                {
                    if (component is Image image)
                    {
                        image.Position += amount;
                    }
                }
            }
        }
    }

    public override void OnStaticMoverTrigger(StaticMover sm)
    {
        if (sm.Entity is Spring spring)
        {
            switch (spring.Orientation)
            {
                case Spring.Orientations.Floor:
                    sinkTimer = 0.5f;
                    return;
                case Spring.Orientations.WallLeft:
                    dashEase = 1f;
                    dashDirection = -Vector2.UnitX;
                    return;
                case Spring.Orientations.WallRight:
                    dashEase = 1f;
                    dashDirection = Vector2.UnitX;
                    break;
                default:
                    return;
            }
        }
    }

    public override void Update()
    {
        base.Update();
        if (MasterOfGroup && ShatterCheck())
        {
            bool hasPlayerRider = false;
            foreach (ConnectedDreamBlock block in Group)
            {
                if (block.HasPlayerRider())
                {
                    hasPlayerRider = true;
                    break;
                }
            }
            if (!hasPlayerRider)
            {
                foreach (JumpThru jumpThru in JumpThrus)
                {
                    if (jumpThru.HasPlayerRider())
                    {
                        hasPlayerRider = true;
                        break;
                    }
                }
            }
            if (hasPlayerRider)
            {
                sinkTimer = 0.3f;
            }
            else if (sinkTimer > 0f)
            {
                sinkTimer -= Engine.DeltaTime;
            }
            yLerp = sinkTimer > 0f ? Calc.Approach(yLerp, 1f, 1f * Engine.DeltaTime) : Calc.Approach(yLerp, 0f, 1f * Engine.DeltaTime);
            sineWave += Engine.DeltaTime;
            dashEase = Calc.Approach(dashEase, 0f, Engine.DeltaTime * 1.5f);
            MoveToTarget();
        }
        LiftSpeed = Vector2.Zero;
    }

}
