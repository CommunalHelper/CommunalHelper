namespace Celeste.Mod.CommunalHelper.States;

public static class St
{
    public static int Elytra { get; private set; }

    internal static void Initialize()
    {
        States.Elytra.Initialize();
    }

    internal static void Load()
    {
        On.Celeste.Player.ctor += Mod_Player_ctor;

        States.Elytra.Load();
    }

    internal static void Unload()
    {
        On.Celeste.Player.ctor -= Mod_Player_ctor;

        States.Elytra.Unload();
    }

    private static void Mod_Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
    {
        orig(self, position, spriteMode);
        Elytra = self.StateMachine.AddState(self.GlideUpdate, self.GlideRoutine, self.GlideBegin, self.GlideEnd);
    }
}
