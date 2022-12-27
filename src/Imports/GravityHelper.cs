using MonoMod.ModInterop;

namespace Celeste.Mod.CommunalHelper.Imports;

/// <summary>
/// Import methods defined <see href="https://gist.github.com/swoolcock/c0d2a708a393c2c762ad8abf614a941b">GravityHelperExports</see>.
/// </summary>
[ModImportName("GravityHelper")]
public static class GravityHelper
{
    public static Action<string> RegisterModSupportBlacklist;

    public static Func<int, string> GravityTypeFromInt;
    public static Func<string, int> GravityTypeToInt;

    public static Func<int> GetPlayerGravity;
    public static Func<Actor, int> GetActorGravity;

    public static Action<int, float> SetPlayerGravity;
    public static Action<Actor, int, float> SetActorGravity;

    public static Func<bool> IsPlayerInverted;
    public static Func<Actor, bool> IsActorInverted;

    public static Func<Actor, Vector2> GetAboveVector;
    public static Func<Actor, Vector2> GetBelowVector;

    public static Func<Actor, Vector2> GetTopCenter;
    public static Func<Actor, Vector2> GetBottomCenter;
    public static Func<Actor, Vector2> GetTopLeft;
    public static Func<Actor, Vector2> GetBottomLeft;
    public static Func<Actor, Vector2> GetTopRight;
    public static Func<Actor, Vector2> GetBottomRight;

    public static Func<Actor, Action<Entity, int, float>, Component> CreateGravityListener;
    public static Func<Action<Player, int, float>, Component> CreatePlayerGravityListener;

    public static Action BeginOverride;
    public static Action EndOverride;
    public static Action<Action> ExecuteOverride;
    public static Func<IDisposable> WithOverride;
}

public enum GravityType
{
    None = -1,
    Normal = 0,
    Inverted,
    Toggle,
}
