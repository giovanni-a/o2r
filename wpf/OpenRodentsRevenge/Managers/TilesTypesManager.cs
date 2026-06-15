using OpenRodentsRevenge.Game;
using OpenRodentsRevenge.Logging;

namespace OpenRodentsRevenge.Managers;

/// <summary>
/// Set of functions managing the different types of tiles.
/// Each tile type has a character (ex : '0'), a texture alias (ex : "void.png")
/// and a type for collisions (ex : "GROUND").
///
/// By default, the game uses:
/// <list type="bullet">
/// <item>"0" = "void.png"  = "GROUND" (no interaction)</item>
/// <item>"1" = "block.png" = "BLOCK"  (mouse can move them)</item>
/// <item>"2" = "wall.png"  = "WALL"   (blocks mouse)</item>
/// </list>
///
/// Direct port of the original <c>TilesTypesManager</c> namespace.
/// </summary>
public static class TilesTypesManager
{
    // Ordered map to mirror Qt's QMap (sorted by key) which the editor bar relies on.
    private static readonly SortedDictionary<char, TileInfo> TILES_TYPES = new();
    private static readonly TileInfo NULL_TILE_INFO = new();

    /// <summary>
    /// Get a tile info from its character.
    /// </summary>
    public static TileInfo TileInfoFromChar(char c)
    {
        return TILES_TYPES.TryGetValue(c, out var info) ? info : NULL_TILE_INFO;
    }

    /// <summary>
    /// Define a tile info from its character.
    /// </summary>
    public static void SetType(char c, string textureAlias, string type)
    {
        string log = $"character '{c}', texture alias \"{textureAlias}\", type \"{type}\"";
        if (c == '\0' || c == ' ' || string.IsNullOrEmpty(textureAlias) || string.IsNullOrEmpty(type))
        {
            Logger.Warn("TilesTypesManager : cannot add invalid tile type " + log);
            return;
        }
        if (c == EditorScreen.MOUSE_POS_CHAR)
        {
            Logger.Warn("TilesTypesManager : cannot add tile type " + log
                        + " : same character as mouse position");
            return;
        }
        TILES_TYPES[c] = new TileInfo(textureAlias, type);
        Logger.Info("TilesTypesManager : defined tile type " + log);
    }

    /// <summary>
    /// Get the tiles types map.
    /// </summary>
    public static IReadOnlyDictionary<char, TileInfo> GetMap() => TILES_TYPES;
}
