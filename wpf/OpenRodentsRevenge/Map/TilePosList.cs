using OpenRodentsRevenge.Common;

namespace OpenRodentsRevenge.Map;

/// <summary>
/// Port of the original <c>typedef QList&lt;sf::Vector2i&gt; TilePosList;</c>.
/// A list of tile positions (in tile units).
/// </summary>
public class TilePosList : List<Vec2i>
{
    public TilePosList()
    {
    }

    public TilePosList(IEnumerable<Vec2i> source)
        : base(source)
    {
    }
}
