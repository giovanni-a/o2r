using System.Windows;
using System.Windows.Media;

namespace OpenRodentsRevenge.Managers;

/// <summary>
/// Builds the vector sprites used to replace the original (proprietary, not
/// shipped) bitmap assets. Each sprite is rendered inside a 16x16 logical cell,
/// matching <c>TiledEntity::TILE_SIZE</c>.
///
/// This is the only part of the port that intentionally diverges from the
/// original implementation: the original loaded PNGs from the active mods via
/// <c>AssetsManager</c>/<c>FilespathProvider</c>. Gameplay-relevant behaviour
/// (which alias maps to which entity/tile) is preserved 1:1.
/// </summary>
internal static class SpriteLibrary
{
    private const double Size = 16.0;

    /// <summary>
    /// Create a frozen brush for the given texture alias, or null if the alias
    /// is unknown (mirrors a failed texture load in the original game).
    /// </summary>
    public static Brush? CreateBrush(string alias)
    {
        Brush? brush = alias switch
        {
            "void.png" => Solid(Color.FromRgb(0xE8, 0xE0, 0xC8)),
            "block.png" => Block(),
            "wall.png" => Wall(),
            "mouse.png" => Mouse(),
            "cat.png" => Cat(Color.FromRgb(0xE0, 0x8A, 0x2B)),
            "cat_awaiting.png" => Cat(Color.FromRgb(0x9A, 0x9A, 0x9A)),
            "cheese.png" => Cheese(),
            "mousetrap.png" => MouseTrap(),
            "hole.png" => Hole(),
            _ => null,
        };
        brush?.Freeze();
        return brush;
    }

    private static Brush Solid(Color color) => new SolidColorBrush(color);

    private static Brush Block()
    {
        var group = new DrawingGroup();
        AddRect(group, Color.FromRgb(0xC8, 0x96, 0x4B), new Rect(0, 0, Size, Size));
        AddRect(group, Color.FromRgb(0xA8, 0x76, 0x33), new Rect(0, 0, Size, 2));
        AddRect(group, Color.FromRgb(0x8A, 0x5E, 0x20), new Rect(0, Size - 2, Size, 2));
        AddRect(group, Color.FromRgb(0xE0, 0xB0, 0x66), new Rect(2, 2, Size - 4, Size - 4));
        return new DrawingBrush(group) { Stretch = Stretch.None };
    }

    private static Brush Wall()
    {
        var group = new DrawingGroup();
        AddRect(group, Color.FromRgb(0x55, 0x55, 0x58), new Rect(0, 0, Size, Size));
        // brick pattern
        var line = Color.FromRgb(0x33, 0x33, 0x36);
        AddRect(group, line, new Rect(0, 5, Size, 1));
        AddRect(group, line, new Rect(0, 10, Size, 1));
        AddRect(group, line, new Rect(5, 0, 1, 5));
        AddRect(group, line, new Rect(11, 5, 1, 5));
        AddRect(group, line, new Rect(5, 10, 1, 6));
        return new DrawingBrush(group) { Stretch = Stretch.None };
    }

    private static Brush Mouse()
    {
        var group = new DrawingGroup();
        var body = Color.FromRgb(0x9E, 0x9E, 0x9E);
        AddEllipse(group, body, new Point(8, 9), 5.5, 5.5);
        AddEllipse(group, body, new Point(4, 4), 2.2, 2.2);
        AddEllipse(group, body, new Point(12, 4), 2.2, 2.2);
        AddEllipse(group, Colors.Black, new Point(6.2, 8), 0.9, 0.9);
        AddEllipse(group, Colors.Black, new Point(9.8, 8), 0.9, 0.9);
        AddEllipse(group, Color.FromRgb(0xFF, 0x9A, 0x9A), new Point(8, 11), 1.1, 1.1);
        return new DrawingBrush(group) { Stretch = Stretch.None };
    }

    private static Brush Cat(Color fur)
    {
        var group = new DrawingGroup();
        AddEllipse(group, fur, new Point(8, 9.5), 6, 5.5);
        // ears
        AddTriangle(group, fur, new Point(2.5, 6), new Point(2.5, 0.5), new Point(6.5, 3.5));
        AddTriangle(group, fur, new Point(13.5, 6), new Point(13.5, 0.5), new Point(9.5, 3.5));
        AddEllipse(group, Color.FromRgb(0x20, 0x60, 0x20), new Point(5.8, 9), 1.0, 1.3);
        AddEllipse(group, Color.FromRgb(0x20, 0x60, 0x20), new Point(10.2, 9), 1.0, 1.3);
        AddEllipse(group, Color.FromRgb(0xC0, 0x40, 0x40), new Point(8, 11.5), 0.9, 0.7);
        return new DrawingBrush(group) { Stretch = Stretch.None };
    }

    private static Brush Cheese()
    {
        var group = new DrawingGroup();
        AddTriangle(group, Color.FromRgb(0xF4, 0xD0, 0x3A), new Point(1, 13), new Point(15, 13), new Point(13, 3));
        AddEllipse(group, Color.FromRgb(0xC9, 0xA8, 0x10), new Point(6, 10), 1.1, 1.1);
        AddEllipse(group, Color.FromRgb(0xC9, 0xA8, 0x10), new Point(10, 11), 0.9, 0.9);
        AddEllipse(group, Color.FromRgb(0xC9, 0xA8, 0x10), new Point(11, 7), 0.8, 0.8);
        return new DrawingBrush(group) { Stretch = Stretch.None };
    }

    private static Brush MouseTrap()
    {
        var group = new DrawingGroup();
        AddRect(group, Color.FromRgb(0x8A, 0x5A, 0x2A), new Rect(2, 2, 12, 12));
        AddRect(group, Color.FromRgb(0xCC, 0xCC, 0xCC), new Rect(3, 3, 10, 2));
        AddEllipse(group, Color.FromRgb(0x44, 0x22, 0x10), new Point(8, 9), 2.2, 2.2);
        return new DrawingBrush(group) { Stretch = Stretch.None };
    }

    private static Brush Hole()
    {
        var group = new DrawingGroup();
        AddRect(group, Color.FromRgb(0xE8, 0xE0, 0xC8), new Rect(0, 0, Size, Size));
        AddEllipse(group, Color.FromRgb(0x20, 0x20, 0x20), new Point(8, 8), 6, 6);
        AddEllipse(group, Colors.Black, new Point(8, 8), 4, 4);
        return new DrawingBrush(group) { Stretch = Stretch.None };
    }

    private static void AddRect(DrawingGroup group, Color color, Rect rect)
    {
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(color), null, new RectangleGeometry(rect)));
    }

    private static void AddEllipse(DrawingGroup group, Color color, Point center, double rx, double ry)
    {
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(color), null, new EllipseGeometry(center, rx, ry)));
    }

    private static void AddTriangle(DrawingGroup group, Color color, Point a, Point b, Point c)
    {
        var figure = new PathFigure { StartPoint = a, IsClosed = true };
        figure.Segments.Add(new LineSegment(b, false));
        figure.Segments.Add(new LineSegment(c, false));
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(color), null, geometry));
    }
}
