using OpenRodentsRevenge.Common;
using OpenRodentsRevenge.Entities;
using OpenRodentsRevenge.Logging;
using OpenRodentsRevenge.Managers;

namespace OpenRodentsRevenge.Map;

/// <summary>
/// A TiledMap handles a variable number of tiles to form a 2D map.
/// Direct port of the original <c>TiledMap</c> class.
///
/// The original used grouped SFML vertex arrays to batch the rendering. This
/// port keeps the map purely as data: the actual drawing is handled by the
/// renderer (<see cref="OpenRodentsRevenge.Rendering.MapRenderer"/>), which only
/// rebuilds its visuals when <see cref="Version"/> changes. That keeps the model
/// free of any UI/toolkit dependency, which is what makes it portable to
/// OpenSilver as-is.
/// </summary>
public class TiledMap
{
    public static readonly uint SIZE_MIN_LIMIT_X = 5;
    public static readonly uint SIZE_MAX_LIMIT_X = 250;
    public static readonly uint SIZE_MIN_LIMIT_Y = 5;
    public static readonly uint SIZE_MAX_LIMIT_Y = 250;

    private static readonly char NULL_TILE_CHAR = '\0';
    private static readonly TileInfo NULL_TILE_INFO = new();

    // Accessible to TiledMapFactory (the original declared it a friend class).
    internal uint mSizeX;
    internal uint mSizeY;
    internal readonly List<List<Tile>> mTiles = new();
    internal LevelInfo mInfo;

    private readonly TiledMapPathfinder mPathfinder;

    /// <summary>
    /// Bumped every time the tiles change (build, edit, push, resize). The
    /// renderer compares this against its last-rendered value to decide whether
    /// it needs to rebuild the (otherwise static) map visuals.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="sizeX">X size, in tiles units.</param>
    /// <param name="sizeY">Y size, in tiles units.</param>
    /// <param name="info">Optional: specify a custom LevelInfo.</param>
    public TiledMap(uint sizeX, uint sizeY, LevelInfo? info = null)
    {
        mSizeX = sizeX;
        mSizeY = sizeY;
        mInfo = info ?? new LevelInfo();
        mPathfinder = new TiledMapPathfinder(this);

        // Fill the map with default tiles
        for (uint i = 0; i < sizeY; i++)
        {
            mTiles.Add(new List<Tile>());
            for (uint j = 0; j < sizeX; j++)
                mTiles[(int)i].Add(new Tile((int)j, (int)i, '0'));
        }
    }

    /// <summary>
    /// (Re)load all tiles. Must be called when tiles change.
    /// </summary>
    public bool BuildMap()
    {
        for (int i = 0; i < mTiles.Count; i++)
        {
            List<Tile> list = mTiles[i];
            for (int j = 0; j < list.Count; j++)
            {
                Tile tile = list[j];
                char c = tile.GetChar();
                if (!tile.LoadTexture(true))
                {
                    Logger.Error($"Cannot build tile from character '{c}'");
                    return false;
                }
            }
        }
        Version++;
        return true;
    }

    /// <summary>
    /// Enumerate every tile as (x, y, info), row-major. Used by the renderer to
    /// build the map's vector geometry.
    /// </summary>
    public IEnumerable<(int x, int y, TileInfo info)> Tiles()
    {
        for (int i = 0; i < mTiles.Count; i++)
        {
            List<Tile> list = mTiles[i];
            for (int j = 0; j < list.Count; j++)
                yield return (j, i, list[j].GetInfo());
        }
    }

    /// <summary>
    /// Get the character of the tile at the given position.
    /// </summary>
    public char GetTileChar(int x, int y)
    {
        Tile? tile = FindTile(x, y);
        return tile != null ? tile.GetChar() : NULL_TILE_CHAR;
    }

    /// <summary>
    /// Get the TileInfo of the tile at the given position.
    /// </summary>
    public TileInfo GetTileInfo(int x, int y)
    {
        Tile? tile = FindTile(x, y);
        return tile != null ? tile.GetInfo() : NULL_TILE_INFO;
    }

    /// <summary>
    /// Helper: get the positions of all the tiles of the given characters.
    /// </summary>
    public TilePosList GetTilesOfChars(IReadOnlyList<char> charFilters)
    {
        var tiles = new TilePosList();
        if (charFilters.Count == 0)
            return tiles;
        for (int i = 0; i < mTiles.Count; i++)
        {
            List<Tile> list = mTiles[i];
            for (int j = 0; j < list.Count; j++)
                if (charFilters.Contains(list[j].GetChar()))
                    tiles.Add(new Vec2i(j, i));
        }
        return tiles;
    }

    /// <summary>
    /// Helper: get the positions of all the tiles of the given type.
    /// </summary>
    public TilePosList GetTilesOfTypes(IReadOnlyList<string> typeFilters)
    {
        var tiles = new TilePosList();
        if (typeFilters.Count == 0)
            return tiles;
        for (int i = 0; i < mTiles.Count; i++)
        {
            List<Tile> list = mTiles[i];
            for (int j = 0; j < list.Count; j++)
                if (typeFilters.Contains(list[j].GetInfo().Type))
                    tiles.Add(new Vec2i(j, i));
        }
        return tiles;
    }

    /// <summary>
    /// Change the character of the tile at the given position (if possible) and load it.
    /// </summary>
    public void SetTileChar(int x, int y, char c,
                            bool allowOutsideIfContiguous = false,
                            bool rebuildNow = true)
    {
        // If outside and not contiguous, ignore
        if (!IsInsideMap(x, y, true))
            return;
        // If outside but contiguous (tile on the left), add a tile if requested
        if (!IsInsideMap(x, y, false))
        {
            if (allowOutsideIfContiguous && IsInsideMap(x - 1, y, false))
                mTiles[y].Add(new Tile(x, y, c));
            else
                return;
        }
        // If inside, change the tile char
        else
        {
            Tile tile = mTiles[y][x];
            tile.SetChar(c);
        }
        if (rebuildNow)
            BuildMap();
        mPathfinder.Reset();
    }

    /// <summary>
    /// Get the LevelInfo.
    /// </summary>
    public LevelInfo Info => mInfo;

    /// <summary>
    /// Compute a path between <paramref name="start"/> and <paramref name="end"/>.
    /// </summary>
    public TilePosList ComputePath(Vec2i start, Vec2i end)
    {
        var path = new TilePosList();
        var rawPath = new List<Vec2i>();
        // Compute path
        TiledMapPathfinder.Result result = mPathfinder.ComputePath(start, end, rawPath);
        if (result != TiledMapPathfinder.Result.SOLVED)
            return path; // if unsolved : return empty path
        // Convert to TilePosList
        for (int i = 0; i < rawPath.Count; i++)
            path.Add(rawPath[i]);
        return path;
    }

    /// <summary>
    /// Is the given position inside the map?
    /// </summary>
    /// <param name="acceptUndefinedTiles">If false, a tile must be present at
    /// the given position.</param>
    public bool IsInsideMap(int x, int y, bool acceptUndefinedTiles = false)
    {
        bool inMap = x >= 0 && x < mSizeX && y >= 0 && y < mSizeY;
        if (!inMap)
            return false;
        return acceptUndefinedTiles || x < mTiles[y].Count;
    }

    public uint SizeX => mSizeX;

    public uint SizeY => mSizeY;

    /// <summary>
    /// Set the X size. <see cref="BuildMap"/> should be called right after.
    /// </summary>
    public void ResizeX(uint sizeX)
    {
        if (sizeX < SIZE_MIN_LIMIT_X || sizeX > SIZE_MAX_LIMIT_X || sizeX == mSizeX)
            return;
        if (sizeX > mSizeX)
        {
            for (int i = 0; i < mTiles.Count; i++)
            {
                List<Tile> line = mTiles[i];
                for (uint j = (uint)line.Count; j < sizeX; j++)
                    line.Add(new Tile((int)j, i, '0'));
            }
        }
        else
        {
            for (int i = 0; i < mTiles.Count; i++)
            {
                List<Tile> line = mTiles[i];
                while (line.Count > sizeX && line.Count > 0)
                    line.RemoveAt(line.Count - 1);
            }
        }
        mSizeX = sizeX;
    }

    /// <summary>
    /// Set the Y size. <see cref="BuildMap"/> should be called right after.
    /// </summary>
    public void ResizeY(uint sizeY)
    {
        if (sizeY < SIZE_MIN_LIMIT_Y || sizeY > SIZE_MAX_LIMIT_Y || sizeY == mSizeY)
            return;
        if (sizeY > mSizeY)
        {
            for (uint i = (uint)mTiles.Count; i < sizeY; i++)
            {
                mTiles.Add(new List<Tile>());
                for (uint j = 0; j < mSizeX; j++)
                    mTiles[(int)i].Add(new Tile((int)j, (int)i, '0'));
            }
        }
        else
        {
            while (mTiles.Count > sizeY && mTiles.Count > 0)
                mTiles.RemoveAt(mTiles.Count - 1);
        }
        mSizeY = sizeY;
    }

    private Tile? FindTile(int x, int y)
    {
        return IsInsideMap(x, y, false) ? mTiles[y][x] : null;
    }
}
