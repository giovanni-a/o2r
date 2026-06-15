using System.Windows.Media;
using OpenRodentsRevenge.Managers;

namespace OpenRodentsRevenge.Rendering;

/// <summary>
/// Flat colour scheme used to render the map tiles.
///
/// The map (potentially hundreds of cells) is the performance-critical layer,
/// especially under OpenSilver where every <c>UIElement</c> becomes a DOM node.
/// Instead of one element per tile, the <see cref="MapRenderer"/> groups all
/// cells that share a fill colour into a single vector <c>Path</c>. This palette
/// supplies that fill (and a matching outline) per tile alias, keeping the whole
/// map down to a couple of elements while staying crisp at any zoom.
/// </summary>
public static class TilePalette
{
    /// <summary>Background colour for ground tiles (drawn once, full-map).</summary>
    public static readonly Color GroundColor = Color.FromRgb(0xE8, 0xE0, 0xC8);

    private static readonly Brush GroundBrushImpl = Frozen(GroundColor);

    /// <summary>Frozen ground background brush.</summary>
    public static Brush GroundBrush => GroundBrushImpl;

    /// <summary>
    /// Fill + outline colours for a non-ground tile, keyed by its texture alias.
    /// Unknown aliases fall back to a neutral grey.
    /// </summary>
    public static (Color fill, Color stroke) Appearance(TileInfo info)
    {
        return info.TextureAlias switch
        {
            "block.png" => (Color.FromRgb(0xC8, 0x96, 0x4B), Color.FromRgb(0x8A, 0x5E, 0x20)),
            "wall.png" => (Color.FromRgb(0x55, 0x55, 0x58), Color.FromRgb(0x33, 0x33, 0x36)),
            "hole.png" => (Color.FromRgb(0x20, 0x20, 0x20), Color.FromRgb(0x10, 0x10, 0x10)),
            _ => (Color.FromRgb(0x88, 0x88, 0x88), Color.FromRgb(0x55, 0x55, 0x55)),
        };
    }

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
