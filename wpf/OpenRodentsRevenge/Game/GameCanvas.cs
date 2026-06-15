using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OpenRodentsRevenge.Common;
using OpenRodentsRevenge.Entities;
using OpenRodentsRevenge.Factories;
using OpenRodentsRevenge.Logging;
using OpenRodentsRevenge.Managers;
using OpenRodentsRevenge.Map;
using OpenRodentsRevenge.Rendering;

namespace OpenRodentsRevenge.Game;

/// <summary>
/// The game surface, where the whole game is rendered. Actual game logic lives
/// in the <see cref="Screen"/> subclasses.
///
/// Port of the original <c>GameCanvas</c> (a SFML-in-Qt widget that redrew every
/// frame). This version is a retained-mode scene built from cross-platform
/// primitives — a <see cref="Canvas"/> holding a map layer and an entity layer —
/// so the exact same code runs under WPF and OpenSilver. A modest
/// <see cref="DispatcherTimer"/> advances the logic; rendering is reconciled
/// (not repainted) and only ever touches elements that actually changed, which
/// is what keeps it fast on OpenSilver's DOM-backed renderer.
///
/// Important: <see cref="FilespathProvider"/>/<see cref="TilesTypesManager"/>
/// must be initialized before running any screen.
/// </summary>
public class GameCanvas : Canvas, IGameView
{
    public static readonly uint DEFAULT_WIDTH = (uint)TiledEntity.TILE_SIZE * 32;
    public static readonly uint DEFAULT_HEIGHT = (uint)TiledEntity.TILE_SIZE * 32;

    // Logic cadence. Cats move at most a few times per second and the editor
    // only needs to poll the mouse, so a light ~30 Hz tick is ample. Each tick
    // is near-free when nothing changed, so this stays gentle on OpenSilver.
    private static readonly TimeSpan FRAME_INTERVAL = TimeSpan.FromMilliseconds(33);

    private readonly Canvas mMapLayer = new();
    private readonly Canvas mEntityLayer = new();
    private readonly MapRenderer mMapRenderer;
    private readonly Clock mFrameClock = new();

    private bool mRunning;
    private TiledMap? mCurrentLevel;
    private Screen mDefaultScreen = null!;
    private Screen mCurrentScreen = null!;
    private bool mInitialized;
    private DispatcherTimer? mTimer;

    public GameCanvas()
    {
        // The entity layer must not intercept hit-testing so the editor can read
        // the mouse position over the (hit-testable) ground beneath it.
        mEntityLayer.IsHitTestVisible = false;
        Children.Add(mMapLayer);
        Children.Add(mEntityLayer);
        mMapRenderer = new MapRenderer(mMapLayer);

        Width = DEFAULT_WIDTH;
        Height = DEFAULT_HEIGHT;
        SizeLayers();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void OnPause() => mRunning = false;

    public void OnResume() => mRunning = true;

    public void OnRetranslate()
    {
    }

    /// <summary>
    /// Get the current level (may be null).
    /// </summary>
    public TiledMap? Level => mCurrentLevel;

    /// <summary>
    /// Load a level from the given file path and set it as the current level.
    /// Warning: won't affect the current screen.
    /// </summary>
    public bool LoadLevel(string path)
    {
        Logger.Info($"Game : loading level {path} .");
        TiledMap? newLevel = TiledMapFactory.LoadLevel(path);
        if (newLevel == null || !newLevel.BuildMap() ||
            (newLevel.SizeX < TiledMap.SIZE_MIN_LIMIT_X &&
             newLevel.SizeY < TiledMap.SIZE_MIN_LIMIT_Y))
        {
            Logger.Error($"Game : cannot load level {path} .");
            return false;
        }
        Logger.Info($"Game : loaded level {path} .");

        SetLevel(newLevel);
        return true;
    }

    /// <summary>
    /// Set the current level and resize the canvas.
    /// Warning: won't affect the current screen.
    /// </summary>
    public void SetLevel(TiledMap? level)
    {
        if (level == null)
            return;
        mCurrentLevel = level;
        mMapRenderer.SetLevel(level);
        AdjustSizeToLevel(); // sizes the layers and reconciles the scene
    }

    /// <summary>
    /// Set the current screen.
    /// </summary>
    public bool SetScreen(Screen? screen, bool start = true)
    {
        if (screen == null)
            return SetScreen(mDefaultScreen);

        mCurrentScreen.OnDetach(mEntityLayer);
        mCurrentScreen.Stop();

        mCurrentScreen = screen;
        mRunning = start;
        bool ok = start ? mCurrentScreen.Start(mCurrentLevel) : true;

        mCurrentScreen.OnAttach(mEntityLayer);
        SyncScene();
        return ok;
    }

    /// <summary>
    /// Reload all used textures.
    /// </summary>
    public void ReloadTextures()
    {
        AssetsManager.ClearTextureCache();
        mCurrentScreen.ReloadTextures();
        mMapRenderer.Invalidate();
        SyncScene();
    }

    /// <summary>
    /// Adjust the canvas's natural size to the level. The hosting Viewbox then
    /// scales it uniformly to whatever space the (resizable) window provides.
    /// </summary>
    public void AdjustSizeToLevel()
    {
        if (mCurrentLevel == null)
            return;
        Width = mCurrentLevel.SizeX * TiledEntity.TILE_SIZE;
        Height = mCurrentLevel.SizeY * TiledEntity.TILE_SIZE;
        SizeLayers();
        // A resize (editor) can change tiles; make sure the map re-renders.
        mMapRenderer.Invalidate();
        SyncScene();
    }

    /// <summary>
    /// Dispatch a key press to the active screen (called from the host window so
    /// input works regardless of focus, mirroring OpenSilver's page-level
    /// keyboard handling).
    /// </summary>
    public void HandleKey(Key key)
    {
        if (!mRunning)
            return;
        mCurrentScreen.HandleEvent(key);
        SyncScene(); // reflect the move immediately
    }

    Vec2i IGameView.GetMousePosition()
    {
        Point p = System.Windows.Input.Mouse.GetPosition(this);
        return new Vec2i((int)p.X, (int)p.Y);
    }

    bool IGameView.IsLeftMouseButtonPressed()
    {
        return System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed && IsMouseOver;
    }

    private void SizeLayers()
    {
        mMapLayer.Width = Width;
        mMapLayer.Height = Height;
        mEntityLayer.Width = Width;
        mEntityLayer.Height = Height;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (mInitialized)
            return;
        OnInit();
        mTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = FRAME_INTERVAL };
        mTimer.Tick += (_, _) => OnUpdate();
        mTimer.Start();
        mInitialized = true;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        mTimer?.Stop();
        mTimer = null;
        mInitialized = false;
    }

    private void OnInit()
    {
        Logger.Info("Initializing game.");
        mDefaultScreen = new EmptyScreen(this);
        mCurrentScreen = mDefaultScreen;
        mRunning = true;
    }

    private void OnUpdate()
    {
        double dt = mFrameClock.Restart();
        // When idle (no game running) we do nothing, so the app stays quiet.
        if (!mRunning)
            return;
        mCurrentScreen.Update(dt);
        SyncScene();
    }

    private void SyncScene()
    {
        mMapRenderer.Sync();
        mCurrentScreen.Sync();
    }
}
