using Celeste.Mod.CommunalHelper.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

// Originally I made this as a standalone entity for someone's map they were working on, but to make this fully work with DreamTunnelDash I moved it to CommunalHelper
// I gave them a plugin for the old version when i finished and I'd like to keep some compatability to the old version so they dont have to redo their berries using it
[CustomEntity("CommunalHelper/DreamStrawberry", "DreamDashListener/DreamDashBerry")]
public class DreamStrawberry : Strawberry
{
    // Original OnDash method from Celeste.Strawberry
    private static readonly MethodInfo m_Strawberry_OnDash = typeof(Strawberry).GetMethod("OnDash", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

    public static Color[] DreamTrailColors = new Color[] {
        Calc.HexToColor("FFEF11"),
        Calc.HexToColor("08A310"),
        Calc.HexToColor("FF00D0"),
        Calc.HexToColor("5FCDE4"),
        Calc.HexToColor("E0564C")
    };

    public DynamicData dreamStrawberryData;

    public static int DreamTrailColorIndex = 0;

    public DreamStrawberry(EntityData data, Vector2 offset, EntityID id) : base(FixData(data), offset, id)
    {
        // Removes any default DashListeners from the strawberry as we do not want to use those
        foreach (Component comp in Components.ToArray())
        {
            if (comp is DashListener)
                Components.Remove(comp);
        }

        // To account for the DashListeners I just brutally murdered, we add a DreamDashListener instead
        // as we want it to activate from dream blocks and not normal player dashes
        Add(new DreamDashListener
        {
            OnDreamDash = new Action<Vector2>(OnDreamBerryDash)
        });

        dreamStrawberryData = DynamicData.For(this);
    }

    public override void Update()
    {
        base.Update();

        // Creates and updates the dream trail
        if (Visible && Scene.OnInterval(0.1f))
            CreateDreamTrail();
    }

    // Calls the original OnDash from our DreamDashListener
    public void OnDreamBerryDash(Vector2 dir)
    {
        m_Strawberry_OnDash.Invoke(this, new object[] { dir });
    }

    // Code to create a trail for the berry to make it separate from normal berries
    public void CreateDreamTrail()
    {
        Sprite berrySprite = dreamStrawberryData.Get<Sprite>("sprite");
        Vector2 scale = new(Math.Abs(berrySprite.Scale.X), berrySprite.Scale.Y);
        TrailManager.Add(this, scale, DreamTrailColors[DreamTrailColorIndex]);
        ++DreamTrailColorIndex;
        DreamTrailColorIndex %= 5;
    }

    // Fixes the entity data of the strawberry, specifically setting winged to true so you don't have to.
    private static EntityData FixData(EntityData data)
    {
        data.Values["winged"] = true;
        return data;
    }

    #region Hooks

    internal static void Hook()
    {
        IL.Celeste.Strawberry.Added += Strawberry_Added;
        IL.Celeste.StrawberrySeed.Awake += StrawberrySeed_Awake;
        IL.Celeste.StrawberrySeed.Update += StrawberrySeed_Update;
        On.Celeste.Player.Update += Player_Update;
        On.Celeste.Player.WallJump += Player_WallJump;
        On.Celeste.Player.ctor += Player_ctor;
    }

    internal static void Unhook()
    {
        IL.Celeste.Strawberry.Added -= Strawberry_Added;
        IL.Celeste.StrawberrySeed.Awake -= StrawberrySeed_Awake;
        IL.Celeste.StrawberrySeed.Update -= StrawberrySeed_Update;
        On.Celeste.Player.Update -= Player_Update;
        On.Celeste.Player.WallJump -= Player_WallJump;
        On.Celeste.Player.ctor -= Player_ctor;
    }

    private static void Strawberry_Added(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(instr => instr.MatchLdstr("strawberry"));
        cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<SpriteBank>(nameof(SpriteBank.Create)));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Sprite, Strawberry, Sprite>>((sprite, strawberry) =>
        {
            if (strawberry is DreamStrawberry)
                sprite = CommunalHelperGFX.SpriteBank.Create("dreamStrawberry");
            return sprite;
        });
    }

    private static void StrawberrySeed_Awake(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, instr => instr.MatchCallvirt<SpriteBank>(nameof(SpriteBank.Create)));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Sprite, StrawberrySeed, Sprite>>((sprite, seed) =>
        {
            if (seed.Strawberry is DreamStrawberry)
                sprite = CommunalHelperGFX.SpriteBank.Create("dreamStrawberrySeed");
            return sprite;
        });
    }

    private static void StrawberrySeed_Update(ILContext il)
    {
        ILCursor cursor = new(il);

        ILLabel label = null;
        cursor.GotoNext(instr => instr.MatchStfld<StrawberrySeed>("losing"));
        cursor.GotoPrev(MoveType.After, instr => instr.MatchBrfalse(out label));

        // We have the label at which to break if don't want the seed to be lost.
        // Let's break to it if the following predicate is true.
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Predicate<StrawberrySeed>>(seed => seed.Strawberry is DreamStrawberry);
        cursor.Emit(OpCodes.Brtrue_S, label);
    }

    private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
    {
        orig(self);

        bool loseDreamSeeds = self.StateMachine.State switch
        {
            Player.StNormal => self.CollideCheck<Platform, DreamBlock>(self.Position + new Vector2(0, self.IsInverted() ? -1 : 1)),
            Player.StClimb => self.CollideCheck<Platform, DreamBlock>(self.Position + (Vector2.UnitX * (int) self.Facing)),
            _ => false,
        };

        if (loseDreamSeeds)
            self.LoseDreamSeeds();
    }

    private static void Player_WallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir)
    {
        orig(self, dir);
        if (self.CollideCheck<Solid, DreamBlock>(self.Position + (Vector2.UnitX * -(3 * dir))))
            self.LoseDreamSeeds();
    }

    private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
    {
        orig(self, position, spriteMode);
        self.Add(new DreamDashListener(_ => self.LoseDreamSeeds()));
    }

    #endregion
}
