namespace OpenRodentsRevenge.Entities;

/// <summary>
/// A trap immobilizes (holes) or kills (mousetrap) the Mouse.
/// Direct port of the original <c>Trap</c> class.
/// </summary>
public class Trap : TiledEntity
{
    private readonly bool mDeadly;

    public Trap(int x, int y, bool deadly)
        : base(x, y, deadly ? "mousetrap.png" : "hole.png")
    {
        mDeadly = deadly;
    }

    /// <summary>Is the trap deadly?</summary>
    public bool Deadly => mDeadly;
}
