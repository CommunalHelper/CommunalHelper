using Celeste.Mod.CommunalHelper.Components;
using Celeste.Mod.CommunalHelper.DashStates;
using MonoMod.Utils;
using System.Linq;
using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.States;

public static class DreamTunnelDash
{
    public static void DreamTunnelDashBegin(this Player player)
    {
        if (player.Get<DreamTunnelDashComponent>() is not { } component)
            return;
        component.Reset();
        
        DreamTunnelDashConfiguration config = CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration;

        // Extra correction for fast moving solids
        Vector2 dir = player.DashDir.Sign();
        if (!DreamTunnelDangerous.CollideCheckNonDangerous(player) && DreamTunnelDangerous.CollideCheckNonDangerous(player, player.Position + dir))
            player.NaiveMove(dir);
        // Hackfix to unduck when downdiagonal dashing next to solid, caused by forcing the player into the solid as part of fast-moving solid correction
        if (player.DashDir.Y > 0)
            player.Ducking = false;

        float playerSpeed = player.Speed.Length();
        Vector2 entryDir = (config.UseEntryDirection && playerSpeed > Player.DashSpeed) ? player.Speed.SafeNormalize() : player.DashDir;
        float entrySpeed = config.SpeedConfiguration switch
        {
            DreamTunnelDashConfiguration.SpeedConfigurations.Default => Player.DashSpeed,
            DreamTunnelDashConfiguration.SpeedConfigurations.NeverSlowDown => Math.Max(playerSpeed, Player.DashSpeed),
            DreamTunnelDashConfiguration.SpeedConfigurations.UseCustomSpeed => config.CustomSpeed,
            _ => 0,
        };

        player.Depth = Depths.PlayerDreamDashing;
        player.Speed = entryDir * entrySpeed;
        player.StartedDashing = false;
        player.TreatNaive = true;
        player.Stamina = Player.ClimbMaxStamina;
        player.dreamJump = false;
        component.DreamTunnelDashCanEndTimer = Player.DreamDashMinTime;
        
        if (player.dreamSfxLoop is null)
            player.Add(player.dreamSfxLoop = new SoundSource { DisposeOnTransition = !config.AllowTransitions });
        player.Play(SFX.char_mad_dreamblock_enter);
        player.Loop(player.dreamSfxLoop, component.FeatherMode ? CustomSFX.game_connectedDreamBlock_dreamblock_fly_travel : SFX.char_mad_dreamblock_travel);

        // Allows DreamDashListener to also work from here, as this is basically a dream block, right?
        foreach (DreamDashListener listener in player.Scene.Tracker.GetComponents<DreamDashListener>().Cast<DreamDashListener>())
            listener.OnDreamDash?.Invoke(player.DashDir);
    }

    public static void DreamTunnelDashEnd(this Player player)
    {
        if (player.Get<DreamTunnelDashComponent>() is not { } component)
            return;

        player.Depth = Depths.Player;
        player.TreatNaive = false;
        if (!player.dreamJump)
        {
            player.AutoJump = true;
            player.AutoJumpTimer = 0f;
        }
        
        if (!player.Inventory.NoRefills)
            player.RefillDash();
        player.RefillStamina();
        
        if (component.Solid is not null)
        {
            if (player.DashDir.X != 0f)
            {
                player.jumpGraceTimer = 0.1f;
                player.dreamJump = true;
            }
            else
            {
                player.jumpGraceTimer = 0f;
            }
            component.Solid.Components.GetAll<DreamTunnelInteraction>().ToList().ForEach(i => i.OnPlayerExit(player));
            component.Solid = null;
        }
        
        player.Stop(player.dreamSfxLoop);
        player.Play(SFX.char_mad_dreamblock_exit);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
    }

    private static void DreamTunnelDashRedirect(this Player player)
    {
        if (player.Get<DreamTunnelDashComponent>() is not { } component)
            return;
        
        DreamTunnelDashConfiguration config = CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration;

        if (config.RedirectConsumesNormalDash ? player.Dashes <= 0 : component.DreamTunnelDashCount <= 0)
            return;

        bool flag = Input.GetAimVector().Sign() == player.DashDir.Sign();
        if ((!config.AllowRedirect || flag) && (!config.AllowSameDirectionRedirect || !flag))
            return;

        if (config.RedirectConsumesNormalDash)
            player.Dashes = Math.Max(0, player.Dashes - 1);
        else
            component.DreamTunnelDashCount = Math.Max(0, component.DreamTunnelDashCount - 1);

        Audio.Play(SFX.char_mad_dreamblock_enter);
        if (Engine.TimeRate > 0.25f)
            Celeste.Freeze(0.05f);

        if (flag)
        {
            player.Speed *= config.SameDirectionSpeedMultiplier;
            player.DashDir *= Math.Sign(config.SameDirectionSpeedMultiplier);
        }
        else
        {
            player.DashDir = Input.GetAimVector();
            player.Speed = player.DashDir * player.Speed.Length();
        }

        Input.Dash.ConsumeBuffer();
    }

    // Do a bounce check and bounce if possible
    private static bool AttemptBounce(this Player player)
    {
        if (!CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration.BounceOnCollision)
            return false;

        Vector2 moveCheckVector = player.Speed * Engine.DeltaTime;
        player.NaiveMove(moveCheckVector);

        Solid solid = DreamTunnelDangerous.CollideFirstNonDangerous(player);
        if (solid is null && player.DreamTunneledIntoDeath())
        {
            // Move the player out of the wall properly, then bounce
            player.NaiveMove(-moveCheckVector);
            player.DreamDashBounce();
            return true;
        }

        // Make sure we undo the check movement
        player.NaiveMove(-moveCheckVector);
        return false;
    }

    private static void DreamDashBounce(this Player player)
    {
        Vector2 horizontalMoveCheckVector = new(player.Speed.X * Engine.DeltaTime, 0f);
        Vector2 verticalMoveCheckVector = new(0f, player.Speed.Y * Engine.DeltaTime);

        bool horizontal = player.OutsideAfterMove(horizontalMoveCheckVector);
        bool vertical = player.OutsideAfterMove(verticalMoveCheckVector);

        if (horizontal)
        {
            player.Speed.X *= -1;
            player.DashDir.X *= -1;
        }
        if (vertical)
        {
            player.Speed.Y *= -1;
            player.DashDir.Y *= -1;
        }
    }

    private static bool OutsideAfterMove(this Player player, Vector2 offset)
    {
        player.NaiveMove(offset);
        bool outside = !DreamTunnelDangerous.CollideCheckNonDangerous(player);
        player.NaiveMove(-offset);

        return outside;
    }

    public static int DreamTunnelDashUpdate(this Player player)
    {
        if (player.Get<DreamTunnelDashComponent>() is not { } component)
            return Player.StNormal;
        
        DreamTunnelDashConfiguration config = CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration;

        if (Input.Dash.Pressed && Input.Aim.Value != Vector2.Zero)
            player.DreamTunnelDashRedirect();

        if (player.AttemptBounce())
            return St.DreamTunnelDash;

        if (component.FeatherMode)
        {
            Vector2 input = Input.Aim.Value.SafeNormalize();
            Vector2 vector = player.Speed.SafeNormalize();
            if (input != Vector2.Zero && vector != Vector2.Zero)
            {
                vector = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector; // wtf is this dot check
                vector = vector.CorrectJoystickPrecision();
                player.DashDir = vector;
                player.Speed = vector * config.SpeedConfiguration switch
                {
                    DreamTunnelDashConfiguration.SpeedConfigurations.Default => Player.DashSpeed,
                    DreamTunnelDashConfiguration.SpeedConfigurations.NeverSlowDown => Math.Max(player.Speed.Length(), Player.DashSpeed),
                    DreamTunnelDashConfiguration.SpeedConfigurations.UseCustomSpeed => config.CustomSpeed,
                    _ => 0,
                };
            }
        }

        Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
        Vector2 position = player.Position;

        Vector2 factor = player.IsInverted() ? new Vector2(1f, -1f) : Vector2.One;
        player.NaiveMove(player.Speed * factor * Engine.DeltaTime);

        if (component.DreamTunnelDashCanEndTimer > 0f)
            component.DreamTunnelDashCanEndTimer -= Engine.DeltaTime;
        Solid solid = DreamTunnelDangerous.CollideFirstNonDangerous(player);
        if (solid is null)
        {
            if (player.DreamTunneledIntoDeath())
                player.DreamDashDie(position);
            else if (component.DreamTunnelDashCanEndTimer <= 0f)
            {
                Celeste.Freeze(0.05f);
                
                if (Input.Jump.Pressed && player.DashDir.X != 0f)
                {
                    player.dreamJump = true;
                    player.Jump();
                }
                else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f)
                {
                    if (player.DashDir.X > 0f && player.CollideCheck<Solid>(player.Position - Vector2.UnitX * 5f))
                        player.MoveHExact(-5);
                    else if (player.DashDir.X < 0f && player.CollideCheck<Solid>(player.Position + Vector2.UnitX * 5f))
                        player.MoveHExact(5);
                    
                    bool canGrabLeft = player.ClimbCheck(-1);
                    bool canGrabRight = player.ClimbCheck(1);
                    if (!Input.GrabCheck || ((player.moveX != 1 || !canGrabRight) && (player.moveX != -1 || !canGrabLeft)))
                        return Player.StNormal;
                    
                    player.Facing = (Facings) player.moveX;
                    if (!SaveData.Instance.Assists.NoGrabbing)
                        return Player.StClimb;
                    
                    player.ClimbTrigger(player.moveX);
                    player.Speed.X = 0f;
                }
                
                return Player.StNormal;
            }
        }
        else
        {
            component.Solid = solid;
            if (player.Scene.OnInterval(0.1f))
                player.CreateDreamTrail();
            
            Level level = player.SceneAs<Level>();
            if (level.OnInterval(0.04f))
            {
                DisplacementRenderer.Burst burst = level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f);
                burst.WorldClipCollider = solid.Collider;
                burst.WorldClipPadding = 2;
            }
        }

        return St.DreamTunnelDash;
    }
}
