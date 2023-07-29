namespace Celeste.Mod.CommunalHelper.Components;

[Tracked]
public class ElytraCollision : Component
{
    /// <summary>
    /// Describes a resulting behavior after an elytra collision.
    /// </summary>
    public enum Result
    {
        /// <summary>
        /// The elytra state is maintained.
        /// </summary>
        Maintain,
        /// <summary>
        /// The elytra state is left.
        /// </summary>
        Finish,
    }

    public Func<Player, Result> Callback { get; set; }

    public ElytraCollision(Func<Player, Result> callback)
        : base(active: false, visible: false)
    {
        Callback = callback;
    }
}
