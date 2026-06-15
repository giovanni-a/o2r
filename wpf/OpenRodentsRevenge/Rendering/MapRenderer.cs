using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using OpenRodentsRevenge.Entities;
using OpenRodentsRevenge.Managers;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Rendering;

/// <summary>
/// Renders a <see cref="TiledMap"/> into a <see cref="Canvas"/> layer using a
/// tiny, fixed number of retained vector elements:
///
///  * one full-map background rectangle for the ground, plus
///  * one <see cref="Path"/> per distinct non-ground tile colour, each holding a
///    <see cref="GeometryGroup"/> of all that colour's cell rectangles.
///
/// So a 23x23 arena with hundreds of blocks still becomes just ~3 elements
/// (1 DOM node each under OpenSilver) instead of one element per cell. The layer
/// is rebuilt only when the map actually changes, detected via
/// <see cref="TiledMap.Version"/>; an unchanged map makes <see cref="Sync"/> a
/// no-op. This replaces the original WPF port's immediate-mode
/// <c>DrawingContext</c> drawing, which OpenSilver does not support.
/// </summary>
public sealed class MapRenderer
{
    private const double Tile = TiledEntity.TILE_SIZE;

    private readonly Canvas mLayer;
    private readonly List<UIElement> mElements = new();

    private TiledMap? mLevel;
    private int mLastVersion = -1;

    public MapRenderer(Canvas layer)
    {
        mLayer = layer;
    }

    /// <summary>Set the level to render (forces a rebuild on the next Sync).</summary>
    public void SetLevel(TiledMap? level)
    {
        mLevel = level;
        mLastVersion = -1;
        if (level == null)
            Clear();
    }

    /// <summary>Force a rebuild on the next Sync (e.g. after a texture/theme reload).</summary>
    public void Invalidate() => mLastVersion = -1;

    /// <summary>Rebuild the map elements if the map changed since the last sync.</summary>
    public void Sync()
    {
        if (mLevel == null)
            return;
        if (mLevel.Version == mLastVersion)
            return;
        Rebuild();
        mLastVersion = mLevel.Version;
    }

    private void Clear()
    {
        foreach (UIElement element in mElements)
            mLayer.Children.Remove(element);
        mElements.Clear();
    }

    private void Rebuild()
    {
        Clear();
        if (mLevel == null)
            return;

        double w = mLevel.SizeX * Tile;
        double h = mLevel.SizeY * Tile;

        // Ground background (single element under the whole map).
        var ground = new Rectangle { Width = w, Height = h, Fill = TilePalette.GroundBrush };
        Canvas.SetLeft(ground, 0);
        Canvas.SetTop(ground, 0);
        AddElement(ground);

        // Group every non-ground cell by its fill colour into one geometry each.
        var geometries = new Dictionary<Color, GeometryGroup>();
        var strokes = new Dictionary<Color, Color>();
        foreach ((int x, int y, TileInfo info) in mLevel.Tiles())
        {
            if (!info.IsValid || info.Type == TileInfo.TYPE_GROUND)
                continue;
            (Color fill, Color stroke) = TilePalette.Appearance(info);
            if (!geometries.TryGetValue(fill, out GeometryGroup? group))
            {
                group = new GeometryGroup();
                geometries[fill] = group;
                strokes[fill] = stroke;
            }
            group.Children.Add(new RectangleGeometry(new System.Windows.Rect(x * Tile, y * Tile, Tile, Tile)));
        }

        foreach (KeyValuePair<Color, GeometryGroup> pair in geometries)
        {
            var path = new Path
            {
                Data = pair.Value,
                Fill = new SolidColorBrush(pair.Key),
                Stroke = new SolidColorBrush(strokes[pair.Key]),
                StrokeThickness = 1,
            };
            AddElement(path);
        }
    }

    private void AddElement(UIElement element)
    {
        mLayer.Children.Add(element);
        mElements.Add(element);
    }
}
