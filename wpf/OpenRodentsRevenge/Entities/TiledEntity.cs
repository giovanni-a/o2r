using OpenRodentsRevenge.Managers;

namespace OpenRodentsRevenge.Entities;

/// <summary>
/// A TiledEntity consists of a single texture with a fixed size.
/// Direct port of the original <c>TiledEntity</c> class.
/// </summary>
public class TiledEntity
{
    /// <summary>
    /// Tile size. A size of 16 means that every tile entity's texture must have
    /// a size of 16x16 pixels.
    /// </summary>
    public const int TILE_SIZE = 16;

    protected int mX;
    protected int mY;

    private string mTextureAlias;
    private Texture? mTexturePtr;

    public TiledEntity(int x, int y, string textureAlias)
    {
        mX = x;
        mY = y;
        mTextureAlias = textureAlias;
    }

    /// <summary>
    /// Change the texture alias and load the associated texture.
    /// </summary>
    public bool SetTextureAlias(string textureAlias)
    {
        mTextureAlias = textureAlias;
        return LoadTexture();
    }

    /// <summary>
    /// (Re)load the texture. Must be called at least once, AFTER having set the
    /// main mod.
    /// </summary>
    public bool LoadTexture()
    {
        mTexturePtr = AssetsManager.GetTexture(mTextureAlias);
        return mTexturePtr != null;
    }

    /// <summary>
    /// Get the texture.
    /// </summary>
    public Texture? GetTexture() => mTexturePtr;

    /// <summary>
    /// Current texture alias (ex: "cat.png"). The renderer watches this to know
    /// which sprite to show and when to swap it (e.g. cat -&gt; cheese).
    /// </summary>
    public string TextureAlias => mTextureAlias;

    public int X => mX;

    public int Y => mY;

    public TiledEntity SetX(int x)
    {
        mX = x;
        return this;
    }

    public TiledEntity SetY(int y)
    {
        mY = y;
        return this;
    }
}
