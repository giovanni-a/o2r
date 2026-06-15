using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OpenRodentsRevenge.Rendering;

/// <summary>
/// Builds the vector visuals used to replace the original (proprietary, not
/// shipped) bitmap assets for the moving entities (mouse, cats, cheese, …).
/// Each visual is a 16x16 <see cref="Canvas"/> of <see cref="Shape"/>s, matching
/// <c>TiledEntity::TILE_SIZE</c>.
///
/// Unlike the static map (which is rendered as a couple of grouped paths), there
/// are only a handful of entities on screen at once, so each can afford a few
/// shapes. They are created once and then merely repositioned via
/// <see cref="Canvas.SetLeft(UIElement,double)"/>/<see cref="Canvas.SetTop(UIElement,double)"/>
/// when the entity moves — this avoids per-frame redraws and keeps the element
/// count tiny, which is exactly what OpenSilver (DOM-backed) needs to stay fast.
///
/// This is the only part of the port that intentionally diverges from the
/// original implementation: the original loaded PNGs from the active mods.
/// Gameplay-relevant behaviour (which alias maps to which entity) is preserved.
/// </summary>
public static class EntityVisuals
{
    private const double Size = 16.0;

    /// <summary>
    /// Create a fresh visual for the given alias, or null if it has no entity
    /// representation (e.g. plain tiles, which the map layer draws instead).
    /// </summary>
    public static FrameworkElement? Create(string alias)
    {
        return alias switch
        {
            "mouse.png" => Mouse(),
            "cat.png" => Cat(Color.FromRgb(0xE0, 0x8A, 0x2B)),
            "cat_awaiting.png" => Cat(Color.FromRgb(0x9A, 0x9A, 0x9A)),
            "cheese.png" => Cheese(),
            "mousetrap.png" => MouseTrap(),
            _ => null,
        };
    }

    private static Canvas NewCell() => new() { Width = Size, Height = Size, IsHitTestVisible = false };

    private static FrameworkElement Mouse()
    {
        Canvas cell = NewCell();
        var body = Color.FromRgb(0x9E, 0x9E, 0x9E);
        AddEllipse(cell, body, 8, 9, 5.5, 5.5);
        AddEllipse(cell, body, 4, 4, 2.2, 2.2);
        AddEllipse(cell, body, 12, 4, 2.2, 2.2);
        AddEllipse(cell, Colors.Black, 6.2, 8, 0.9, 0.9);
        AddEllipse(cell, Colors.Black, 9.8, 8, 0.9, 0.9);
        AddEllipse(cell, Color.FromRgb(0xFF, 0x9A, 0x9A), 8, 11, 1.1, 1.1);
        return cell;
    }

    private static FrameworkElement Cat(Color fur)
    {
        Canvas cell = NewCell();
        AddEllipse(cell, fur, 8, 9.5, 6, 5.5);
        AddTriangle(cell, fur, new Point(2.5, 6), new Point(2.5, 0.5), new Point(6.5, 3.5));
        AddTriangle(cell, fur, new Point(13.5, 6), new Point(13.5, 0.5), new Point(9.5, 3.5));
        AddEllipse(cell, Color.FromRgb(0x20, 0x60, 0x20), 5.8, 9, 1.0, 1.3);
        AddEllipse(cell, Color.FromRgb(0x20, 0x60, 0x20), 10.2, 9, 1.0, 1.3);
        AddEllipse(cell, Color.FromRgb(0xC0, 0x40, 0x40), 8, 11.5, 0.9, 0.7);
        return cell;
    }

    private static FrameworkElement Cheese()
    {
        Canvas cell = NewCell();
        AddTriangle(cell, Color.FromRgb(0xF4, 0xD0, 0x3A), new Point(1, 13), new Point(15, 13), new Point(13, 3));
        AddEllipse(cell, Color.FromRgb(0xC9, 0xA8, 0x10), 6, 10, 1.1, 1.1);
        AddEllipse(cell, Color.FromRgb(0xC9, 0xA8, 0x10), 10, 11, 0.9, 0.9);
        AddEllipse(cell, Color.FromRgb(0xC9, 0xA8, 0x10), 11, 7, 0.8, 0.8);
        return cell;
    }

    private static FrameworkElement MouseTrap()
    {
        Canvas cell = NewCell();
        AddRect(cell, Color.FromRgb(0x8A, 0x5A, 0x2A), 2, 2, 12, 12);
        AddRect(cell, Color.FromRgb(0xCC, 0xCC, 0xCC), 3, 3, 10, 2);
        AddEllipse(cell, Color.FromRgb(0x44, 0x22, 0x10), 8, 9, 2.2, 2.2);
        return cell;
    }

    private static void AddRect(Canvas cell, Color color, double x, double y, double w, double h)
    {
        var rect = new Rectangle { Width = w, Height = h, Fill = new SolidColorBrush(color) };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        cell.Children.Add(rect);
    }

    private static void AddEllipse(Canvas cell, Color color, double cx, double cy, double rx, double ry)
    {
        var ellipse = new Ellipse { Width = rx * 2, Height = ry * 2, Fill = new SolidColorBrush(color) };
        Canvas.SetLeft(ellipse, cx - rx);
        Canvas.SetTop(ellipse, cy - ry);
        cell.Children.Add(ellipse);
    }

    private static void AddTriangle(Canvas cell, Color color, Point a, Point b, Point c)
    {
        var polygon = new Polygon
        {
            Fill = new SolidColorBrush(color),
            Points = new PointCollection { a, b, c },
        };
        cell.Children.Add(polygon);
    }
}
