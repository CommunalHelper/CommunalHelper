using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Celeste.Mod.CommunalHelper.Entities;

public abstract class CustomBooster : Booster
{
    protected DynamicData BoosterData;

    public ParticleType P_CustomAppear, P_CustomBurst;

    private bool hasCustomSounds;
    private string enterSoundEvent, moveSoundEvent;
    private bool playMoveEventEnd;

    public bool RedBoost => BoosterData.Get<bool>("red");

    public Sprite Sprite => BoosterData.Get<Sprite>("sprite");

    public float MovementInBubbleFactor { get; set; } = 3f;

    public virtual bool IgnorePlayerSpeed => false;
    public virtual bool OffsetCameraBySpeed => true;

    public CustomBooster(Vector2 position, bool redBoost)
        : base(position, redBoost)
    {
        BoosterData = new(typeof(Booster), this);

        P_CustomAppear = P_Appear;
        P_CustomBurst = redBoost ? P_BurstRed : P_Burst;
    }

    protected void ReplaceSprite(Sprite newSprite)
    {
        Sprite oldSprite = BoosterData.Get<Sprite>("sprite");
        Remove(oldSprite);
        BoosterData.Set("sprite", newSprite);
        Add(newSprite);
    }

    protected void SetParticleColors(Color burstColor, Color appearColor)
    {
        BoosterData.Set("particleType", P_CustomBurst = new ParticleType(P_Burst)
        {
            Color = burstColor
        });
        P_CustomAppear = new ParticleType(P_Appear)
        {
            Color = appearColor
        };
    }

    protected void SetSoundEvent(string enterSound, string moveSound, bool playMoveEnd = false)
    {
        enterSoundEvent = enterSound;
        moveSoundEvent = moveSound;
        playMoveEventEnd = playMoveEnd;
        hasCustomSounds = true;
    }

    public void LoopingSfxParam(string path, float value)
    {
        BoosterData.Get<SoundSource>("loopingSfx").Param(path, value);
    }

    protected virtual void OnRespawn() { }
    protected virtual void OnPlayerEnter(Player player) { }
    protected virtual void OnPlayerExit(Player player) { }

    /// <summary>
    /// Executed before <see cref="Player"/>.RedDashUpdate, can be used to return a different <see cref="Player"/> state ID.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>
    /// An optional <see cref="Player"/> state ID. If set, it will be the returned <see cref="Player"/> state.<br/>
    /// Note: <see cref="RedDashUpdateAfter(Player)"/> takes priority over this method on which <see cref="Player"/> state is returned.
    /// </returns>
    protected virtual int? RedDashUpdateBefore(Player player)
    {
        return null;
    }

    /// <summary>
    /// Executed after <see cref="Player"/>.RedDashUpdate, can be used to return a different <see cref="Player"/> state ID.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>An optional <see cref="Player"/> state ID. If set, it will be the returned <see cref="Player"/> state.<br/></returns>
    protected virtual int? RedDashUpdateAfter(Player player)
    {
        return null;
    }

    /// <summary>
    /// An coroutine appended to Player.RedDashCoroutine.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The IEnumerator that represents the routine.</returns>
    protected virtual IEnumerator RedDashCoroutineAfter(Player player) { yield return null; }

    /// <summary>
    /// Replacement coroutine to Player.BoostRoutine.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The IEnumerator that represents the routine.</returns>
    protected virtual IEnumerator BoostCoroutine(Player player)
    {
        yield return 0.25f;
        if (RedBoost)
            player.StateMachine.State = RedBoost ? Player.StRedDash : Player.StDash;
    }

    #region Hooks

    private static readonly MethodInfo m_Player_orig_Update
        = typeof(Player).GetMethod("orig_Update", BindingFlags.Public | BindingFlags.Instance);

    private static readonly MethodInfo m_Player_get_CameraTarget
        = typeof(Player).GetProperty(nameof(Player.CameraTarget)).GetGetMethod();

    private static ILHook IL_Player_orig_Update;
    private static ILHook IL_Player_get_CameraTarget;

    public static void Load()
    {
        DreamBoosterHooks.Hook();

        On.Celeste.Booster.Respawn += Booster_Respawn;
        On.Celeste.Booster.AppearParticles += Booster_AppearParticles;
        On.Celeste.Booster.OnPlayer += Booster_OnPlayer;
        On.Celeste.Booster.PlayerBoosted += Booster_PlayerBoosted;
        On.Celeste.Booster.PlayerReleased += Booster_PlayerReleased;
        On.Celeste.Booster.BoostRoutine += Booster_BoostRoutine;

        IL.Celeste.Player.BoostUpdate += Player_BoostUpdate;
        On.Celeste.Player.BoostCoroutine += Player_BoostCoroutine;
        On.Celeste.Player.RedDashUpdate += Player_RedDashUpdate;
        On.Celeste.Player.RedDashCoroutine += Player_RedDashCoroutine;

        IL_Player_orig_Update = new ILHook(m_Player_orig_Update, Player_orig_Update);
        IL_Player_get_CameraTarget = new ILHook(m_Player_get_CameraTarget, Player_get_CameraTarget);
    }

    public static void Unload()
    {
        DreamBoosterHooks.Unhook();

        On.Celeste.Booster.Respawn -= Booster_Respawn;
        On.Celeste.Booster.AppearParticles -= Booster_AppearParticles;
        On.Celeste.Booster.OnPlayer -= Booster_OnPlayer;
        On.Celeste.Booster.PlayerBoosted -= Booster_PlayerBoosted;
        On.Celeste.Booster.PlayerReleased -= Booster_PlayerReleased;
        On.Celeste.Booster.BoostRoutine -= Booster_BoostRoutine;

        On.Celeste.Player.BoostCoroutine -= Player_BoostCoroutine;
        On.Celeste.Player.RedDashUpdate -= Player_RedDashUpdate;
        On.Celeste.Player.RedDashCoroutine -= Player_RedDashCoroutine;

        IL_Player_orig_Update.Dispose();
        IL_Player_get_CameraTarget.Dispose();
    }

    private static void Booster_Respawn(On.Celeste.Booster.orig_Respawn orig, Booster self)
    {
        orig(self);
        if (self is CustomBooster booster)
            booster.OnRespawn();
    }

    private static IEnumerator Booster_BoostRoutine(On.Celeste.Booster.orig_BoostRoutine orig, Booster self, Player player, Vector2 dir)
    {
        IEnumerator origEnum = orig(self, player, dir);
        while (origEnum.MoveNext())
            yield return origEnum.Current;

        if (self is CustomBooster booster)
            booster.OnPlayerExit(player);
    }

    private static void Booster_PlayerReleased(On.Celeste.Booster.orig_PlayerReleased orig, Booster self)
    {
        orig(self);
        if (self is CustomBooster booster && booster.RedBoost && booster.hasCustomSounds && booster.playMoveEventEnd)
        {
            booster.BoosterData.Get<SoundSource>("loopingSfx").Play(booster.moveSoundEvent, "end", 1f);
        }
    }

    private static void Booster_PlayerBoosted(On.Celeste.Booster.orig_PlayerBoosted orig, Booster self, Player player, Vector2 direction)
    {
        orig(self, player, direction);
        if (self is CustomBooster booster && booster.hasCustomSounds && booster.RedBoost)
        {
            booster.BoosterData.Get<SoundSource>("loopingSfx").Play(booster.moveSoundEvent);
        }
    }

    private static void Booster_OnPlayer(On.Celeste.Booster.orig_OnPlayer orig, Booster self, Player player)
    {
        if (self is CustomBooster booster)
        {
            bool justEntered = booster.BoosterData.Get<float>("respawnTimer") <= 0f && booster.BoosterData.Get<float>("cannotUseTimer") <= 0f && !self.BoostingPlayer;
            if (booster.hasCustomSounds)
            {
                if (justEntered)
                {
                    booster.BoosterData.Set("cannotUseTimer", 0.45f);

                    if (booster.RedBoost)
                        player.RedBoost(self);
                    else
                        player.Boost(self);

                    Audio.Play(booster.enterSoundEvent, self.Position);
                    booster.BoosterData.Get<Wiggler>("wiggler").Start();

                    Sprite sprite = booster.BoosterData.Get<Sprite>("sprite");
                    sprite.Play("inside");
                    sprite.FlipX = player.Facing == Facings.Left;
                }
            }
            else
            {
                orig(self, player);
            }
            if (justEntered)
                booster.OnPlayerEnter(player);
        }
        else
        {
            orig(self, player);
        }
    }

    private static void Booster_AppearParticles(On.Celeste.Booster.orig_AppearParticles orig, Booster self)
    {
        if (self is CustomBooster booster)
        {
            ParticleSystem particlesBG = self.SceneAs<Level>().ParticlesBG;
            for (int i = 0; i < 360; i += 30)
            {
                particlesBG.Emit(booster.P_CustomAppear, 1, self.Center, Vector2.One * 2f, i * ((float) Math.PI / 180f));
            }
        }
        else
        {
            orig(self);
        }
    }

    private static void Player_BoostUpdate(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(3));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<float, Player, float>>((f, player) => player.LastBooster is CustomBooster booster ? booster.MovementInBubbleFactor : f);
    }

    private static IEnumerator Player_BoostCoroutine(On.Celeste.Player.orig_BoostCoroutine orig, Player self)
    {
        IEnumerator routine = self.LastBooster is CustomBooster booster
            ? booster.BoostCoroutine(self)
            : orig(self);
        while (routine.MoveNext())
            yield return routine.Current;
    }

    private static int Player_RedDashUpdate(On.Celeste.Player.orig_RedDashUpdate orig, Player self)
    {
        if (self.LastBooster is not CustomBooster booster)
            return orig(self);

        // execute RedDashUpdateBefore, store its potential replacement for returned state
        int? pre = booster.RedDashUpdateBefore(self);
        // original update
        int res = orig(self);
        // execute RedDashUpdateAfter, store its potential replacement for returned state
        int? post = booster.RedDashUpdateAfter(self);

        // return the 'latest' returned state.
        // 'post' takes priority first, then 'pre', and lastly the original result.
        return post ?? pre ?? res;
    }

    private static IEnumerator Player_RedDashCoroutine(On.Celeste.Player.orig_RedDashCoroutine orig, Player self)
    {
        // get the booster now, it'll be set to null in the coroutine
        Booster currentBooster = self.CurrentBooster;

        // do the entire coroutine, thanks max480 :)
        IEnumerator origRoutine = orig(self);
        while (origRoutine.MoveNext())
            yield return origRoutine.Current;

        if (currentBooster is not CustomBooster booster)
            yield break;

        IEnumerator routine = booster.RedDashCoroutineAfter(self);
        while (routine.MoveNext())
            yield return routine.Current;
    }

    // Related to all curved boosters:
    // Makes the player unaffected by its own speed if it is inside a curved booster.
    // This allows us to set the player's speed, so that stuff like spikes-player checks work correctly.
    // But in reality, we are moving the player ourselves to the position we desire.
    private static void Player_orig_Update(ILContext il)
    {
        ILCursor cursor = new(il);

        ILLabel label = null;

        cursor.GotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.Update)));

        // Prevent player being affected by its horizontal speed if UnaffectedBySpeed(Player) returns true.
        cursor.GotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.MoveH)));
        cursor.GotoPrev(MoveType.After, instr => instr.MatchBeq(out label));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Predicate<Player>>(UnaffectedBySpeed);
        cursor.Emit(OpCodes.Brtrue_S, label);

        // Prevent player being affected by its vertical speed if UnaffectedBySpeed(Player) returns true.
        cursor.GotoNext(instr => instr.MatchCall<Actor>(nameof(Actor.MoveV)));
        cursor.GotoPrev(MoveType.After, instr => instr.MatchBeq(out label));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Predicate<Player>>(UnaffectedBySpeed);
        cursor.Emit(OpCodes.Brtrue_S, label);
    }

    private static bool UnaffectedBySpeed(Player player)
        => player.StateMachine.State is Player.StRedDash
        && player.LastBooster is CustomBooster booster
        && booster.IgnorePlayerSpeed;

    private static void Player_get_CameraTarget(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchLdcI4(Player.StRedDash));
        ILLabel label = (ILLabel) cursor.Next.Operand;

        cursor.GotoNext(MoveType.After, instr => instr.MatchBneUn(label));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Predicate<Player>>(DoesNotAffectCamera);
        cursor.Emit(OpCodes.Brtrue, label);
    }

    private static bool DoesNotAffectCamera(Player player)
        => player.LastBooster is CustomBooster booster
        && !booster.OffsetCameraBySpeed;

    #endregion
}
