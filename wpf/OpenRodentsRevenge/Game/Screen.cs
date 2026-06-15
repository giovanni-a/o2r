using System.Windows.Controls;
using System.Windows.Input;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Game;

/// <summary>
/// Abstract base class for all game screens. Port of the original
/// <c>Screen</c> class.
///
/// The original drew everything every frame through an immediate-mode target.
/// To support OpenSilver (which has no immediate-mode drawing) and to keep
/// rendering cheap, screens now use a retained scene: they create their visuals
/// once in <see cref="OnAttach"/>, remove them in <see cref="OnDetach"/>, and
/// just reconcile them with the game state in <see cref="Sync"/> (called after
/// each logic <see cref="Update"/>). The static map layer is owned by the host
/// and is not the screen's responsibility.
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
    /// Create the screen's visuals and add them to the given entity layer.
    /// Called once when the screen becomes active (after <see cref="Start"/>).
    /// </summary>
    public virtual void OnAttach(Canvas entityLayer)
    {
    }

    /// <summary>
    /// Remove the screen's visuals from the entity layer. Called when the screen
    /// is replaced.
    /// </summary>
    public virtual void OnDetach(Canvas entityLayer)
    {
    }

    /// <summary>
    /// Reconcile the screen's visuals with the current game state. Cheap to call
    /// repeatedly: it only touches elements that actually changed.
    /// </summary>
    public virtual void Sync()
    {
    }

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
