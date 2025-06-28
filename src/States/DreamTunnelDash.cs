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
        DynamicData playerData = player.GetData();
        DreamTunnelDashConfiguration config = CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration;

        player.StartedDashing = false;

        if (player.dreamSfxLoop == null)
        {
            player.dreamSfxLoop = new SoundSource();
            player.dreamSfxLoop.DisposeOnTransition = !config.AllowTransitions;
            player.Add(player.dreamSfxLoop);
        }

        // Extra correction for fast moving solids, this does not cause issues with dashdir leniency
        Vector2 dir = player.DashDir.Sign();
        if (!player.CollideCheck<Solid, DreamBlock>() && player.CollideCheck<Solid, DreamBlock>(player.Position + dir))
        {
            player.NaiveMove(dir);
        }

        // Hackfix to unduck when downdiagonal dashing next to solid, caused by forcing the player into the solid as part of fast-moving solid correction
        if (player.DashDir.Y > 0)
            player.Ducking = false;

        float playerSpeed = player.Speed.Length();
        Vector2 entryDir = (config.UseEntryDirection && playerSpeed > Player.DashSpeed) ? player.Speed.SafeNormalize() : player.DashDir;
        float entrySpeed = config.SpeedConfiguration switch
        {
            SpeedConfiguration.Default => Player.DashSpeed,
            SpeedConfiguration.NeverSlowDown => Math.Max(playerSpeed, Player.DashSpeed),
            SpeedConfiguration.UseCustomSpeed => config.CustomSpeed,
            _ => 0,
        };

        player.Speed = entryDir * entrySpeed;
        player.TreatNaive = true;
        player.Depth = Depths.PlayerDreamDashing;
        playerData.Set(Player_dreamTunnelDashCanEndTimer, 0.1f);
        player.Stamina = Player.ClimbMaxStamina;
        playerData.Set("dreamJump", false);
        player.Play(SFX.char_mad_dreamblock_enter, null, 0f);
        if (FeatherMode)
            player.Loop(player.dreamSfxLoop, CustomSFX.game_connectedDreamBlock_dreamblock_fly_travel);
        else
            player.Loop(player.dreamSfxLoop, SFX.char_mad_dreamblock_travel);

        // Allows DreamDashListener to also work from here, as this is basically a dream block, right?
        foreach (DreamDashListener component in player.Scene.Tracker.GetComponents<DreamDashListener>())
        {
            component.OnDreamDash?.Invoke(player.DashDir);
        }
    }

    public static void DreamTunnelDashEnd(this Player player)
    {
        DynamicData playerData = player.GetData();

        player.Depth = Depths.Player;
        if (!player.dreamJump)
        {
            player.AutoJump = true;
            player.AutoJumpTimer = 0f;
        }
        if (!player.Inventory.NoRefills)
        {
            player.RefillDash();
        }
        player.RefillStamina();
        player.TreatNaive = false;
        Solid solid = playerData.Get<Solid>(Player_solid);
        if (solid != null)
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
            solid.Components.GetAll<DreamTunnelInteraction>().ToList().ForEach(i => i.OnPlayerExit(player));
            solid = null;
        }
        player.Stop(player.dreamSfxLoop);
        player.Play(SFX.char_mad_dreamblock_exit, null, 0f);
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
    }

    private static void DreamTunnelDashRedirect(this Player player)
    {
        DreamTunnelDashConfiguration config = CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration;

        if (config.RedirectConsumesNormalDash ? player.Dashes <= 0 : DreamTunnelDashCount <= 0)
            return;

        bool flag = Input.GetAimVector().Sign() == player.Speed.Sign();
        if ((!config.AllowRedirect || flag) && (!config.AllowSameDirectionRedirect || !flag))
            return;
        
        if (config.RedirectConsumesNormalDash)
            player.Dashes = Math.Max(0, player.Dashes - 1);
        else
            DreamTunnelDashCount = Math.Max(0, DreamTunnelDashCount - 1);
                
        Audio.Play("event:/char/madeline/dreamblock_enter");
        if (Engine.TimeRate > 0.25f)
        {
            Celeste.Freeze(0.05f);
        }
                
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

        Solid solid = player.CollideFirst<Solid, DreamBlock>();
        if (solid == null && player.DreamTunneledIntoDeath())
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
        }
        if (vertical)
        {
            player.Speed.Y *= -1;
        }
    }

    private static bool OutsideAfterMove(this Player player, Vector2 offset)
    {
        player.NaiveMove(offset);
        bool outside = player.CollideFirst<Solid, DreamBlock>() == null;
        player.NaiveMove(-offset);

        return outside;
    }

    public static int DreamTunnelDashUpdate(this Player player)
    {
        DynamicData playerData = player.GetData();
        DreamTunnelDashConfiguration config = CommunalHelperModule.Session.CurrentDreamTunnelDashConfiguration;
        
        if (Input.Dash.Pressed && Input.Aim.Value != Vector2.Zero)
        {
            player.DreamTunnelDashRedirect();
        }
        
        if (player.AttemptBounce())
        {
            return St.DreamTunnelDash;
        }

        if (FeatherMode)
        {
            Vector2 input = Input.Aim.Value.SafeNormalize();
            if (input != Vector2.Zero)
            {
                Vector2 vector = player.Speed.SafeNormalize();
                if (vector != Vector2.Zero)
                {
                    vector = Vector2.Dot(input, vector) != -0.8f ? vector.RotateTowards(input.Angle(), 5f * Engine.DeltaTime) : vector;
                    vector = vector.CorrectJoystickPrecision();
                    player.DashDir = vector;
                    player.Speed = vector * config.SpeedConfiguration switch
                    {
                        SpeedConfiguration.Default => Player.DashSpeed,
                        SpeedConfiguration.NeverSlowDown => Math.Max(player.Speed.Length(), Player.DashSpeed),
                        SpeedConfiguration.UseCustomSpeed => config.CustomSpeed,
                        _ => 0,
                    };
                }
            }
        }

        Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
        Vector2 position = player.Position;

        Vector2 factor = Vector2.One;
        if (player.IsInverted())
            factor.Y = -1;
        player.NaiveMove(player.Speed * factor * Engine.DeltaTime);

        float dreamDashCanEndTimer = playerData.Get<float>(Player_dreamTunnelDashCanEndTimer);
        if (dreamDashCanEndTimer > 0f)
        {
            playerData.Set(Player_dreamTunnelDashCanEndTimer, dreamDashCanEndTimer - Engine.DeltaTime);
        }
        Solid solid = player.CollideFirst<Solid, DreamBlock>();
        if (solid == null)
        {
            if (player.DreamTunneledIntoDeath())
            {
                player.DreamDashDie(position);
            }
            else if (playerData.Get<float>(Player_dreamTunnelDashCanEndTimer) <= 0f)
            {
                Celeste.Freeze(0.05f);
                if (Input.Jump.Pressed && player.DashDir.X != 0f)
                {
                    player.dreamJump = true;
                    player.Jump(true, true);
                }
                else if (player.DashDir.Y >= 0f || player.DashDir.X != 0f)
                {
                    if (player.DashDir.X > 0f && player.CollideCheck<Solid>(player.Position - (Vector2.UnitX * 5f)))
                    {
                        player.MoveHExact(-5, null, null);
                    }
                    else if (player.DashDir.X < 0f && player.CollideCheck<Solid>(player.Position + (Vector2.UnitX * 5f)))
                    {
                        player.MoveHExact(5, null, null);
                    }
                    bool flag = player.ClimbCheck(-1, 0);
                    bool flag2 = player.ClimbCheck(1, 0);
                    int moveX = player.moveX;
                    if (Input.GrabCheck && ((moveX == 1 && flag2) || (moveX == -1 && flag)))
                    {
                        player.Facing = (Facings) moveX;
                        if (!SaveData.Instance.Assists.NoGrabbing)
                        {
                            return Player.StClimb;
                        }
                        player.ClimbTrigger(moveX);
                        player.Speed.X = 0f;
                    }
                }
                return Player.StNormal;
            }
        }
        else
        {
            playerData.Set(Player_solid, solid);
            if (player.Scene.OnInterval(0.1f))
            {
                player.CreateDreamTrail();
            }
            Level level = player.level;
            if (level.OnInterval(0.04f))
            {
                DisplacementRenderer.Burst burst = level.Displacement.AddBurst(player.Center, 0.3f, 0f, 40f, 1f, null, null);
                burst.WorldClipCollider = solid.Collider;
                burst.WorldClipPadding = 2;
            }
        }

        return St.DreamTunnelDash;
    }
}
