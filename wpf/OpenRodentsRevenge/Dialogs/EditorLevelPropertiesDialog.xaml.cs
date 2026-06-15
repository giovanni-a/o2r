using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Dialogs;

/// <summary>
/// The level properties dialog of the editor. Also used when creating a new
/// level. Port of the original <c>EditorLevelPropertiesDialog</c>.
/// </summary>
public partial class EditorLevelPropertiesDialog : Window
{
    private readonly bool mNewLevelMode;

    public EditorLevelPropertiesDialog(TiledMap? level)
    {
        InitializeComponent();
        mNewLevelMode = level == null;

        // If a level is edited, load its current properties
        if (mNewLevelMode)
            return;
        textBoxXSize.Text = level!.SizeX.ToString(CultureInfo.InvariantCulture);
        textBoxYSize.Text = level.SizeY.ToString(CultureInfo.InvariantCulture);
        textBoxName.Text = level.Info.Name;
        textBoxAuthor.Text = level.Info.Author;
        checkBoxMouseRandomPos.IsChecked = level.Info.MouseRandomPos;
    }

    public uint LevelSizeX => Clamp(ParseOr(textBoxXSize.Text, TiledMap.SIZE_MIN_LIMIT_X),
                                    TiledMap.SIZE_MIN_LIMIT_X, TiledMap.SIZE_MAX_LIMIT_X);

    public uint LevelSizeY => Clamp(ParseOr(textBoxYSize.Text, TiledMap.SIZE_MIN_LIMIT_Y),
                                    TiledMap.SIZE_MIN_LIMIT_Y, TiledMap.SIZE_MAX_LIMIT_Y);

    public bool MouseRandomPos => checkBoxMouseRandomPos.IsChecked == true;

    public string LevelName => textBoxName.Text;

    public string LevelAuthor => textBoxAuthor.Text;

    private void TextBoxName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (mNewLevelMode && buttonOk != null)
            buttonOk.IsEnabled = !string.IsNullOrEmpty(textBoxName.Text);
    }

    private void ButtonOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private static uint ParseOr(string text, uint fallback)
    {
        return uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out uint value)
            ? value
            : fallback;
    }

    private static uint Clamp(uint value, uint min, uint max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
