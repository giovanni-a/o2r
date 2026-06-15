namespace OpenRodentsRevenge.Managers;

/// <summary>
/// A <see cref="TileInfo"/> contains the Tile's texture alias and type.
/// Direct port of the original <c>TileInfo</c> struct.
/// </summary>
public readonly struct TileInfo
{
    public TileInfo()
    {
        TextureAlias = string.Empty;
        Type = string.Empty;
        IsValid = false;
    }

    public TileInfo(string textureAlias, string type)
    {
        TextureAlias = textureAlias;
        Type = type;
        IsValid = true;
    }

    public string TextureAlias { get; }
    public string Type { get; }
    public bool IsValid { get; }

    public const string TYPE_GROUND = "GROUND";
    public const string TYPE_BLOCK = "BLOCK";
    public const string TYPE_WALL = "WALL";
}
