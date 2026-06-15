using OpenRodentsRevenge.Logging;

namespace OpenRodentsRevenge.Managers;

/// <summary>
/// Set of functions managing the game assets. Direct port of the original
/// <c>AssetsManager</c> namespace: it keeps a cache of loaded textures keyed by
/// their resolved path, loading them lazily on first request.
///
/// The original resolved an alias to a file path via <see cref="FilespathProvider"/>
/// and loaded it from disk. Here the "loading" step builds a vector sprite (see
/// <see cref="SpriteLibrary"/>); everything else (caching, null on failure,
/// cache clearing) is preserved.
/// </summary>
public static class AssetsManager
{
    private static readonly Dictionary<string, Texture?> TEXTURE_MAP = new();

    /// <summary>
    /// Get a texture (load it if not already done).
    /// </summary>
    /// <param name="path">Texture name (ex: "mouse.png").</param>
    /// <param name="isAlias">If true, resolve the path through <see cref="FilespathProvider"/>.</param>
    public static Texture? GetTexture(string path, bool isAlias = true)
    {
        string texturePath = isAlias ? FilespathProvider.AssetPathFromAlias(path) : path;

        // Texture already loaded
        if (TEXTURE_MAP.TryGetValue(texturePath, out var cached) && cached != null)
            return cached;

        // Texture must be loaded
        Texture? texture = LoadTexture(texturePath);
        if (texture != null)
        {
            TEXTURE_MAP[texturePath] = texture;
            Logger.Info($"AssetsManager : loaded texture {texturePath} .");
            return texture;
        }

        // Cannot load the texture
        Logger.Error($"AssetsManager : cannot load texture {texturePath} .");
        return null;
    }

    /// <summary>
    /// Clear the texture cache. All used textures should be reloaded through
    /// <see cref="GetTexture"/> then.
    /// </summary>
    public static void ClearTextureCache()
    {
        Logger.Info("AssetsManager : cleared texture cache.");
        TEXTURE_MAP.Clear();
    }

    private static Texture? LoadTexture(string path)
    {
        // The resolved path is, for this port, simply the alias (see
        // FilespathProvider). Build a vector sprite from it.
        var brush = SpriteLibrary.CreateBrush(path);
        return brush != null ? new Texture(path, brush) : null;
    }
}
