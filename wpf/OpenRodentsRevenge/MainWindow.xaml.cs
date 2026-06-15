using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using OpenRodentsRevenge.Factories;
using OpenRodentsRevenge.Game;
using OpenRodentsRevenge.Logging;
using OpenRodentsRevenge.Managers;
using OpenRodentsRevenge.Map;
using ORDialogs = OpenRodentsRevenge.Dialogs;

namespace OpenRodentsRevenge;

/// <summary>Possible game modes. Port of the original <c>GAME_MODE</c> enum.</summary>
public enum GameMode
{
    PLAY,
    EDIT,
    NONE,
}

/// <summary>
/// The main window. Port of the original <c>MainWindow</c> class.
///
/// The modding system, multi-language support and the SFML-in-Qt embedding are
/// intentionally out of scope for v1 of this port (see README); everything else
/// follows the original control flow and method names closely.
/// </summary>
public partial class MainWindow : Window
{
    public static string VERSION = "1.0";

    private const int STATUS_BAR_MSG_TIME = 2000;
    private const int DEFAULT_FILE_CAT_COUNT = 3;

    private GameCanvas mGameCanvas = null!;
    private Screen mGameScreen = null!;
    private Screen mEditorScreen = null!;
    private ToggleButton? prevSelectedAction;
    private int mCampaignIndex = -1;

    public MainWindow()
    {
        InitializeComponent();

        InitManagers();
        FilespathProvider.RefreshAssetsList();
        SwitchToGameMode(GameMode.NONE);

        // Init editor bar
        prevSelectedAction = null;
        PopulateEditorBar();

        // Init game canvas
        mGameCanvas = new GameCanvas();
        mGameCanvas.RequestResize += ResizeCanvas;
        canvasHost.Child = mGameCanvas;

        // Init game screens
        mGameScreen = new GameScreen(mGameCanvas);
        mEditorScreen = new EditorScreen(mGameCanvas);

        // Win/lose flow (campaign progression)
        var gs = (GameScreen)mGameScreen;
        gs.Won += OnLevelWon;
        gs.Lost += OnLevelLost;

        BuildSelectLevelMenu();

        // Auto-start the first campaign level once the canvas is initialized
        // (its screens/render loop are set up in its own Loaded handler, which
        // runs before this one since it was registered first).
        mGameCanvas.Loaded += OnCanvasFirstLoaded;

        Closing += (_, _) => Logger.Info("Closing Open Rodent's Revenge.");

        Logger.Info($"Open Rodent's Revenge version {VERSION} started.");
    }

    private void InitManagers()
    {
        // Initialize FilespathProvider
        FilespathProvider.SetModsLocation("mods/");
        FilespathProvider.SetMainModFolder("original");
        FilespathProvider.SetAssetsNameFilters(new[] { "*.bmp", "*.dds", "*.jpg", "*.png", "*.tga", "*.psd" });

        // Set up default tiles
        TilesTypesManager.SetType('0', "void.png", TileInfo.TYPE_GROUND);
        TilesTypesManager.SetType('1', "block.png", TileInfo.TYPE_BLOCK);
        TilesTypesManager.SetType('2', "wall.png", TileInfo.TYPE_WALL);
    }

    private void PopulateEditorBar()
    {
        // Add "Mouse" action (place mouse)
        AddEditorBarButton(EditorScreen.MOUSE_POS_CHAR, "Place the mouse start position", "mouse.png");

        // Add all available tiles
        foreach (var pair in TilesTypesManager.GetMap())
            AddEditorBarAction(pair.Key, pair.Value.Type, pair.Value.TextureAlias);
    }

    private void AddEditorBarAction(char c, string type, string textureAlias)
    {
        string tip = type switch
        {
            TileInfo.TYPE_WALL => "Tile type : wall",
            TileInfo.TYPE_BLOCK => "Tile type : block",
            TileInfo.TYPE_GROUND => "Tile type : ground",
            _ => $"Tile ID : '{c}'",
        };
        AddEditorBarButton(c, tip, textureAlias);
    }

    private void AddEditorBarButton(char c, string tip, string textureAlias)
    {
        Brush? brush = AssetsManager.GetTexture(textureAlias)?.Brush;
        var button = new ToggleButton
        {
            Tag = c,
            ToolTip = tip,
            Width = 28,
            Height = 28,
            Margin = new Thickness(1),
            Content = new Rectangle
            {
                Width = 22,
                Height = 22,
                Fill = brush ?? Brushes.Transparent,
            },
        };
        button.Click += OnEditorBarActionTriggered;
        editorBar.Items.Add(button);
    }

    private void SwitchToGameMode(GameMode mode)
    {
        if (mode == GameMode.PLAY)
        {
            ToggleEditorActions(false);
            ToggleGameActions(true);
        }
        else if (mode == GameMode.EDIT)
        {
            ToggleGameActions(false);
            ToggleEditorActions(true);
        }
        else
        {
            ToggleGameActions(false);
            ToggleEditorActions(false);
        }
    }

    private void ToggleGameActions(bool enable)
    {
        menuEditorCurrentLevel.IsEnabled = enable;
        menuRestartLevel.IsEnabled = enable;
    }

    private void BuildSelectLevelMenu()
    {
        for (int i = 0; i < Campaign.Levels.Count; i++)
        {
            int index = i; // capture
            var item = new MenuItem { Header = Campaign.Levels[i].Name };
            item.Click += (_, _) => StartCampaignLevel(index);
            menuSelectLevel.Items.Add(item);
        }
    }

    private void OnCanvasFirstLoaded(object sender, RoutedEventArgs e)
    {
        mGameCanvas.Loaded -= OnCanvasFirstLoaded; // one-shot
        StartCampaignLevel(0);
    }

    private void OnActionNewGame(object sender, RoutedEventArgs e)
    {
        StartCampaignLevel(0);
    }

    private void OnActionRestartLevel(object sender, RoutedEventArgs e)
    {
        RestartCurrentLevel();
    }

    private void StartCampaignLevel(int index)
    {
        if (index < 0 || index >= Campaign.Levels.Count)
            return;
        CampaignLevel def = Campaign.Levels[index];
        TiledMap? level = TiledMapFactory.LoadLevelFromText(def.Layout, def.Name);
        if (level == null || !level.BuildMap())
        {
            CriticalError($"Cannot load level \"{def.Name}\".");
            return;
        }
        mCampaignIndex = index;
        mGameCanvas.SetLevel(level);
        ((GameScreen)mGameScreen).PrepareCats(def.CatCount);
        mGameCanvas.Focus();
        if (!mGameCanvas.SetScreen(mGameScreen))
        {
            CriticalError($"Cannot play level \"{def.Name}\".");
            SwitchToGameMode(GameMode.NONE);
            mGameCanvas.SetScreen(null);
            return;
        }
        Title = $"Open Rodent's Revenge - {def.Name}";
        SwitchToGameMode(GameMode.PLAY);
    }

    private void RestartCurrentLevel()
    {
        if (mCampaignIndex >= 0)
        {
            StartCampaignLevel(mCampaignIndex);
            return;
        }
        // Non-campaign (file) level : reload from its path if available.
        TiledMap? level = mGameCanvas.Level;
        string path = level?.Info.FilePath ?? string.Empty;
        if (!string.IsNullOrEmpty(path) && mGameCanvas.LoadLevel(path))
        {
            ((GameScreen)mGameScreen).PrepareCats(DEFAULT_FILE_CAT_COUNT);
            mGameCanvas.Focus();
            mGameCanvas.SetScreen(mGameScreen);
            SwitchToGameMode(GameMode.PLAY);
        }
    }

    private void OnLevelWon()
    {
        // Deferred so we don't show a modal dialog from within the update tick.
        Dispatcher.BeginInvoke(() =>
        {
            mGameCanvas.OnPause();
            if (mCampaignIndex >= 0 && mCampaignIndex < Campaign.Levels.Count - 1)
            {
                MessageBox.Show(this, "Level cleared! On to the next one.", "Open Rodent's Revenge",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                StartCampaignLevel(mCampaignIndex + 1);
            }
            else
            {
                MessageBox.Show(this, "Congratulations — you trapped every cat and finished all levels!",
                    "You win!", MessageBoxButton.OK, MessageBoxImage.Information);
                mCampaignIndex = -1;
                SwitchToGameMode(GameMode.NONE);
                mGameCanvas.SetScreen(null);
                Title = "Open Rodent's Revenge";
            }
        });
    }

    private void OnLevelLost()
    {
        Dispatcher.BeginInvoke(() =>
        {
            mGameCanvas.OnPause();
            MessageBox.Show(this, "A cat caught you! Try again.", "Game over",
                MessageBoxButton.OK, MessageBoxImage.Exclamation);
            RestartCurrentLevel();
        });
    }

    private void ToggleEditorActions(bool enable)
    {
        menuEditorSaveLevel.IsEnabled = enable;
        menuEditorSaveLevelAs.IsEnabled = enable;
        menuEditorLevelProperties.IsEnabled = enable;
        editorBarTray.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool ExecModalDialog(Window dialog)
    {
        mGameCanvas.OnPause();
        dialog.Owner = this;
        bool result = dialog.ShowDialog() == true;
        mGameCanvas.OnResume();
        return result;
    }

    private void OnEditorBarActionTriggered(object sender, RoutedEventArgs e)
    {
        var action = (ToggleButton)sender;
        ((EditorScreen)mEditorScreen).SetPlaceableChar((char)action.Tag);
        action.IsChecked = true;
        if (prevSelectedAction != null && !ReferenceEquals(prevSelectedAction, action))
            prevSelectedAction.IsChecked = false;
        prevSelectedAction = action;
    }

    private void ResizeCanvas(int w, int h)
    {
        Rect screen = SystemParameters.WorkArea;
        if (w >= screen.Width || h >= screen.Height)
        {
            MessageBox.Show(this,
                "Level size is bigger than screen size, level cannot be displayed properly.",
                "Size problem", MessageBoxButton.OK, MessageBoxImage.Warning);
            mGameCanvas.Width = GameCanvas.DEFAULT_WIDTH;
            mGameCanvas.Height = GameCanvas.DEFAULT_HEIGHT;
            return;
        }
        // The window auto-sizes to the canvas (SizeToContent).
    }

    private void OnActionPlayLevel(object sender, RoutedEventArgs e)
    {
        string? path = PromptOpenLevel("Play a level");
        if (path == null)
            return;
        if (mGameCanvas.LoadLevel(path))
        {
            mCampaignIndex = -1; // not a campaign level
            ((GameScreen)mGameScreen).PrepareCats(DEFAULT_FILE_CAT_COUNT);
            mGameCanvas.Focus();
            if (!mGameCanvas.SetScreen(mGameScreen))
            {
                CriticalError($"Cannot play level \"{path}\".");
                SwitchToGameMode(GameMode.NONE);
                mGameCanvas.SetScreen(null);
                return;
            }
            Title = $"Open Rodent's Revenge - {System.IO.Path.GetFileName(path)}";
            SwitchToGameMode(GameMode.PLAY);
        }
        else
        {
            CriticalError($"Cannot load level \"{path}\".");
        }
    }

    private void OnActionEditorNewLevel(object sender, RoutedEventArgs e)
    {
        var newLevelDialog = new ORDialogs.EditorLevelPropertiesDialog(null)
        {
            Title = "New level properties",
        };
        if (!ExecModalDialog(newLevelDialog))
            return;
        // Init the new level
        uint sizeX = newLevelDialog.LevelSizeX, sizeY = newLevelDialog.LevelSizeY;
        var info = new LevelInfo
        {
            Name = newLevelDialog.LevelName,
            Author = newLevelDialog.LevelAuthor,
            MousePosX = sizeX / 2,
            MousePosY = sizeY / 2,
        };
        var newLevel = new TiledMap(sizeX, sizeY, info);
        // Launch the editor screen
        if (!newLevel.BuildMap())
        {
            CriticalError($"Cannot create level \"{info.Name}\".");
            SwitchToGameMode(GameMode.NONE);
            mGameCanvas.SetScreen(null);
            return;
        }
        mGameCanvas.SetLevel(newLevel);
        mGameCanvas.SetScreen(mEditorScreen, true);
        SwitchToGameMode(GameMode.EDIT);
    }

    private void OnActionEditorExistingLevel(object sender, RoutedEventArgs e)
    {
        string? path = PromptOpenLevel("Edit an existing level");
        if (path == null)
            return;
        if (mGameCanvas.LoadLevel(path))
        {
            mGameCanvas.Focus();
            if (!mGameCanvas.SetScreen(mEditorScreen))
            {
                CriticalError($"Cannot edit level \"{path}\".");
                SwitchToGameMode(GameMode.NONE);
                mGameCanvas.SetScreen(null);
                return;
            }
            SwitchToGameMode(GameMode.EDIT);
        }
        else
        {
            CriticalError($"Cannot load level \"{path}\".");
        }
    }

    private void OnActionEditorCurrentLevel(object sender, RoutedEventArgs e)
    {
        if (mGameCanvas.Level != null && mGameCanvas.SetScreen(mEditorScreen))
        {
            SwitchToGameMode(GameMode.EDIT);
        }
        else
        {
            CriticalError("Cannot edit the current level.");
            SwitchToGameMode(GameMode.NONE);
        }
    }

    private void OnActionEditorSaveLevel(object sender, RoutedEventArgs e)
    {
        TiledMap? level = mGameCanvas.Level;
        if (level == null)
        {
            CriticalError("Empty level, cannot save it.");
            return;
        }

        string filepath = level.Info.FilePath;
        if (string.IsNullOrEmpty(filepath))
        {
            OnActionEditorSaveLevelAs(sender, e);
            return;
        }
        if (!TiledMapFactory.SaveLevel(level, filepath))
            CriticalError($"Error while saving the level as \"{filepath}\".");
    }

    private void OnActionEditorSaveLevelAs(object sender, RoutedEventArgs e)
    {
        TiledMap? level = mGameCanvas.Level;
        if (level == null)
        {
            CriticalError("Empty level, cannot save it.");
            return;
        }

        string currentPath = level.Info.FilePath;
        var dialog = new SaveFileDialog
        {
            Title = "Save level as",
            Filter = "Level new format (*.xml)|*.xml|Level old format (*.txt)|*.txt",
            FileName = currentPath,
        };
        if (!string.IsNullOrEmpty(currentPath) && !currentPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            dialog.FilterIndex = 2;
        if (dialog.ShowDialog(this) != true)
            return;

        if (!TiledMapFactory.SaveLevel(level, dialog.FileName))
            CriticalError($"Error while saving the level as \"{dialog.FileName}\".");
    }

    private void OnActionEditorLevelProperties(object sender, RoutedEventArgs e)
    {
        TiledMap? level = mGameCanvas.Level;
        if (level == null)
        {
            CriticalError("Empty level, cannot edit its properties.");
            return;
        }

        var propertiesDialog = new ORDialogs.EditorLevelPropertiesDialog(level);
        int oldX = (int)level.SizeX, oldY = (int)level.SizeY;
        if (!ExecModalDialog(propertiesDialog))
            return;
        // Apply LevelInfo changes
        level.Info.Name = propertiesDialog.LevelName;
        level.Info.Author = propertiesDialog.LevelAuthor;
        level.Info.MouseRandomPos = propertiesDialog.MouseRandomPos;
        // Apply level size changes (warning if needed)
        int newX = (int)propertiesDialog.LevelSizeX, newY = (int)propertiesDialog.LevelSizeY;
        if (newX == oldX && newY == oldY)
            return;
        if (newX < oldX || newY < oldY) // reducing level size : ask for confirmation
        {
            MessageBoxResult result = MessageBox.Show(this,
                "Do you really want to reduce the size of the level? The affected tiles will be lost for good.",
                "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;
        }
        level.ResizeX((uint)newX);
        level.ResizeY((uint)newY);
        if (!level.BuildMap())
            Logger.Error("build error");
        mGameCanvas.AdjustSizeToLevel();
    }

    private void OnActionAbout(object sender, RoutedEventArgs e)
    {
        mGameCanvas.OnPause();
        MessageBox.Show(this,
            $"Open Rodent's Revenge\nVersion {VERSION}\n\n" +
            "C# / WPF port of the open-source remake of Microsoft's \"Rodent's Revenge\" (1991).",
            "About Open Rodent's Revenge", MessageBoxButton.OK, MessageBoxImage.Information);
        mGameCanvas.OnResume();
    }

    private string? PromptOpenLevel(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "Level files (*.txt;*.xml)|*.txt;*.xml",
            InitialDirectory = LevelsDirectory(),
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private static string LevelsDirectory()
    {
        string dir = System.IO.Path.Combine(AppContext.BaseDirectory, "levels");
        return Directory.Exists(dir) ? dir : AppContext.BaseDirectory;
    }

    private void CriticalError(string message)
    {
        MessageBox.Show(this, message, "Critical error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
