using Celeste.Mod.CommunalHelper.Entities.SSC26;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Components;
internal class WeightAndChainComponent : Component
{
    public WeightAndChain ball = null;
    private int framesAtMaxChainLength = 0;

    public bool AffectPlayerDummy = false;

    private Player player => Entity as Player;
    public bool Attached => ball != null && player != null;
    public bool IsAtMaxChainLength => Attached && (ConnPtPlayer - ConnPtBall).LengthSquared() >= ball.chainLength * ball.chainLength;
    public Vector2 ConnPtPlayer => player.ExactPosition + new Vector2(0, player.Height * -0.5f);
    public Vector2 ConnPtBall => ball.ChainConnectionPhysicsPosition;
    public float DistBetweenPlayerAndBall => (ConnPtPlayer - ConnPtBall).Length();

    public WeightAndChainComponent() : base(true, false) { }

    internal Vector2 debugVec = Vector2.Zero;

    public override void Update()
    {
        base.Update();
        if (!IsAtMaxChainLength) { return; }
        if (!AffectPlayerDummy && player.StateMachine.State == Player.StDummy) { return; }
        // Pull the player towards the ball
        Vector2 dir = (ConnPtPlayer - ConnPtBall).SafeNormalize();
        Vector2 target = ConnPtBall + dir * (ball.chainLength + 0.01f);
        Vector2 movement = target - ConnPtPlayer;
        Vector2 relativePlayerSpeed = player.Speed - ball.Speed;
        // If the player is dashing horizontally and close to the ball's Y position, don't apply any vertical movement
        float noLiftTolerance = ball.chainLength / 1.414f;  // roughly 45 degrees
        bool noLift = Math.Abs(ConnPtPlayer.Y - ConnPtBall.Y) < noLiftTolerance;
        if (noLift)
        {
            movement.Y = 0;
        }
        // If the player speed is moving away from the ball, cancel the component in that direction
        if (Vector2.Dot(target - ConnPtBall, relativePlayerSpeed) > 0)
        {
            Vector2 parallel = Vector2.Dot(relativePlayerSpeed, dir) * dir;
            Vector2 perpendicular = relativePlayerSpeed - parallel;
            if (framesAtMaxChainLength <= 1)
            {
                // On the first frame of being at max chain length, just cancel the player's speed away from the ball
                // This is just the chain snapping tight
                relativePlayerSpeed -= parallel;
            }
            else if (perpendicular.LengthSquared() > 100f)
            {
                // After the first frame, swing the player around the ball
                if (perpendicular.LengthSquared() > 1f)
                {
                    debugVec = perpendicular.SafeNormalize() * relativePlayerSpeed.Length();
                    relativePlayerSpeed = debugVec;
                }
            }
            Vector2 newPlayerSpeed = ball.Speed + relativePlayerSpeed;
            if (noLift)
            {
                newPlayerSpeed.Y = player.Speed.Y;
            }
            player.Speed = newPlayerSpeed;
            player.LiftSpeed = ball.Speed;
        }

        // Move the player towards the target position
        player.MoveH(movement.X);
        if (!noLift)
        {
            player.MoveV(movement.Y);
        }
    }

    internal void DeduplicateBalls(Level level)
    {
        WeightAndChain new_ball = null;
        float min_dist2 = float.MaxValue;
        WeightAndChain[] balls = level.Tracker.GetEntities<WeightAndChain>().Cast<WeightAndChain>().ToArray();
        foreach (WeightAndChain other in balls)
        {
            if (other?.State == WeightAndChain.States.Inactive)
            {  // Prevent normal linking process during the intro cutscene
                return;
            }
            if (other?.IsTransitioningOut ?? true) continue;
            if (other.Hold.Holder != null)
            {
                min_dist2 = 0f;
                new_ball = other;
                break;
            }
            var dist2 = (player.Center - other.Center).LengthSquared();
            if (dist2 < min_dist2)
            {
                min_dist2 = dist2;
                new_ball = other;
            }
        }
        foreach (WeightAndChain other in balls)
        {
            if (other != new_ball)
            {
                other.RemoveSelf();
            }
        }
        ball = new_ball;
        if (ball != null)
        {
            ball.BaCComponent = this;
        }
    }
}
