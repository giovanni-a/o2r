using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using OpenRodentsRevenge.Common;
using OpenRodentsRevenge.Entities;
using OpenRodentsRevenge.Factories;
using OpenRodentsRevenge.Logging;
using OpenRodentsRevenge.Managers;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Game;

/// <summary>
/// The Game canvas, where the whole game is rendered. Actual game logic lives in
/// the <see cref="Screen"/> subclasses.
///
/// Port of the original <c>GameCanvas</c> (which derived from a SFML-in-Qt
/// widget). Here it is a WPF <see cref="FrameworkElement"/> driven by
/// <see cref="CompositionTarget.Rendering"/> for its per-frame update, and it
/// renders through <see cref="OnRender"/>.
///
/// Important: <see cref="FilespathProvider"/>/<see cref="TilesTypesManager"/>
/// must be initialized before running any screen.
/// </summary>
public class GameCanvas : FrameworkElement, IGameView
{
    public static readonly uint DEFAULT_WIDTH = (uint)TiledEntity.TILE_SIZE * 32;
    public static readonly uint DEFAULT_HEIGHT = (uint)TiledEntity.TILE_SIZE * 32;

    private static readonly Color DEFAULT_CLEAR_COLOR = Color.FromRgb(0, 0, 0);
    private static readonly Brush DEFAULT_CLEAR_BRUSH = CreateFrozen(DEFAULT_CLEAR_COLOR);

    private bool mRunning;
    private TiledMap? mCurrentLevel;
    private Screen mDefaultScreen = null!;
    private Screen mCurrentScreen = null!;
    private readonly Clock mFrameClock = new();
    private bool mInitialized;

    // ~60 FPS update/render cadence. The original SFML loop ran as fast as the
    // host would allow; an uncapped WPF render loop pegs the CPU/GPU, so we cap
    // it here.
    private static readonly TimeSpan FRAME_INTERVAL = TimeSpan.FromMilliseconds(16);
    private DispatcherTimer? mTimer;

    public GameCanvas()
    {
        Focusable = true;
        Width = DEFAULT_WIDTH;
        Height = DEFAULT_HEIGHT;
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
        AdjustSizeToLevel();
    }

    /// <summary>
    /// Set the current screen.
    /// </summary>
    public bool SetScreen(Screen? screen, bool start = true)
    {
        if (screen == null)
            return SetScreen(mDefaultScreen);
        mCurrentScreen.Stop();
        mCurrentScreen = screen;
        mRunning = start;
        return start ? mCurrentScreen.Start(mCurrentLevel) : true;
    }

    /// <summary>
    /// Reload all used textures.
    /// </summary>
    public void ReloadTextures()
    {
        AssetsManager.ClearTextureCache();
        mCurrentScreen.ReloadTextures();
    }

    /// <summary>
    /// Adjust the canvas's natural size to the level. The hosting Viewbox then
    /// scales it uniformly to whatever space the (resizable) window provides.
    /// </summary>
    public void AdjustSizeToLevel()
    {
        if (mCurrentLevel == null)
            return;
        int w = (int)(mCurrentLevel.SizeX * TiledEntity.TILE_SIZE),
            h = (int)(mCurrentLevel.SizeY * TiledEntity.TILE_SIZE);
        Width = w;
        Height = h;
        InvalidateVisual();
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

        Width = DEFAULT_WIDTH;
        Height = DEFAULT_HEIGHT;
    }

    private void OnUpdate()
    {
        double dt = mFrameClock.Restart();
        // Update current Screen (if running). When idle we don't redraw at all,
        // which keeps the app from consuming CPU while no game is in progress.
        if (!mRunning)
            return;
        // (Key events are dispatched directly from OnKeyDown, mirroring the
        // original event polling loop.)
        mCurrentScreen.Update(dt);
        // Request a repaint (clearing + rendering happens in OnRender).
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (mRunning)
            mCurrentScreen.HandleEvent(e.Key);
        // Prevent the arrow keys from being consumed by WPF focus navigation,
        // which would otherwise move keyboard focus away from the canvas.
        if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right)
            e.Handled = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        // Clear previous render
        dc.DrawRectangle(DEFAULT_CLEAR_BRUSH, null, new Rect(0, 0, Width, Height));
        mCurrentScreen?.Render(dc);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Width, Height);
    }

    private static Brush CreateFrozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
