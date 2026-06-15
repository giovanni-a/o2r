using OpenRodentsRevenge.Common;

namespace OpenRodentsRevenge.Game;

/// <summary>
/// Abstraction of the rendering surface / window passed to <see cref="Screen"/>s.
///
/// In the original code, screens received a <c>const sf::Window&amp;</c> used to
/// query the relative mouse position and button state (via
/// <c>sf::Mouse::getPosition(window)</c> / <c>sf::Mouse::isButtonPressed</c>).
/// This interface exposes the same capabilities for the WPF host.
/// </summary>
public interface IGameView
{
    /// <summary>Mouse position relative to the canvas, in pixels.</summary>
    Vec2i GetMousePosition();

    /// <summary>Is the left mouse button currently pressed?</summary>
    bool IsLeftMouseButtonPressed();
}
