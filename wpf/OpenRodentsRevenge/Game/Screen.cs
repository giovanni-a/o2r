using System.Windows.Input;
using System.Windows.Media;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Game;

/// <summary>
/// Abstract base class for all game screens. Direct port of the original
/// <c>Screen</c> class.
/// </summary>
public abstract class Screen
{
    protected readonly IGameView mWindow;
    protected TiledMap? mLevelPtr;
    protected bool mStarted;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="window">Containing view (needed for relative mouse position).</param>
    protected Screen(IGameView window)
    {
        mWindow = window;
        mLevelPtr = null;
        mStarted = false;
    }

    /// <summary>
    /// Render to the given drawing context. Note: on each frame the screen is
    /// cleared by GameCanvas.
    /// </summary>
    public abstract void Render(DrawingContext dc);

    /// <summary>
    /// Update.
    /// </summary>
    /// <param name="dt">Elapsed time, in seconds, since last update.</param>
    public abstract void Update(double dt);

    /// <summary>
    /// Handle a key-press event.
    /// </summary>
    public abstract void HandleEvent(Key key);

    /// <summary>
    /// (Re)start the screen with the specified level.
    /// </summary>
    public virtual bool Start(TiledMap? level)
    {
        mLevelPtr = level;
        mStarted = mLevelPtr != null;
        return mStarted;
    }

    /// <summary>
    /// Stop the screen.
    /// </summary>
    public virtual void Stop()
    {
        mStarted = false;
        mLevelPtr = null;
    }

    /// <summary>
    /// Reload all used textures. Default implementation: update the level.
    /// </summary>
    public virtual void ReloadTextures()
    {
        mLevelPtr?.BuildMap();
    }
}
