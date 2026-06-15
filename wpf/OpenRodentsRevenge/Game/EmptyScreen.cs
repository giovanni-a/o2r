using System.Windows.Input;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Game;

/// <summary>
/// The default, empty screen. Direct port of the original <c>EmptyScreen</c>.
/// </summary>
public class EmptyScreen : Screen
{
    public EmptyScreen(IGameView window)
        : base(window)
    {
    }

    public override void Update(double dt)
    {
    }

    public override void HandleEvent(Key key)
    {
    }

    public override bool Start(TiledMap? level)
    {
        return base.Start(level);
    }
}
