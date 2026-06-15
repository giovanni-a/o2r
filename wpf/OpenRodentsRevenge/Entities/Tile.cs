using OpenRodentsRevenge.Managers;

namespace OpenRodentsRevenge.Entities;

/// <summary>
/// A Tile is a fragment of a TiledMap. Direct port of the original <c>Tile</c>.
/// </summary>
public class Tile : TiledEntity
{
    private TileInfo mInfo;
    private char mC;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="x">X position, in tiles units.</param>
    /// <param name="y">Y position, in tiles units.</param>
    /// <param name="c">Tile's character.</param>
    /// <param name="buildNow">Load the tile's infos and texture now. True by
    /// default. If false, <see cref="LoadTexture(bool)"/> must be called manually.</param>
    public Tile(int x, int y, char c, bool buildNow = true)
        : base(x, y, "void.png")
    {
        mC = c;
        if (buildNow)
            SetChar(c, true);
    }

    /// <summary>
    /// Update if needed and (re)load the texture.
    /// </summary>
    public bool LoadTexture(bool updateInfo = false)
    {
        if (updateInfo)
            SetChar(mC, false);
        if (!mInfo.IsValid)
            return false;
        return SetTextureAlias(mInfo.TextureAlias);
    }

    /// <summary>
    /// Get the tile's information.
    /// </summary>
    public TileInfo GetInfo() => mInfo;

    /// <summary>
    /// Get the tile's character ID.
    /// </summary>
    public char GetChar() => mC;

    /// <summary>
    /// Change the tile character ID and refresh the TileInfo.
    /// </summary>
    public bool SetChar(char c, bool updateTexture = true)
    {
        if (c == '\0' || c == ' ')
            return false;
        mC = c;
        mInfo = TilesTypesManager.TileInfoFromChar(mC);
        return updateTexture ? LoadTexture(false) : mInfo.IsValid;
    }
}
