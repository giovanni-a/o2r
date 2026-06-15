using OpenRodentsRevenge.Logging;

namespace OpenRodentsRevenge.Managers;

/// <summary>
/// Set of functions managing the game assets. Direct port of the original
/// <c>AssetsManager</c> namespace: it keeps a cache of loaded textures keyed by
/// their resolved path, loading them lazily on first request.
///
/// The original resolved an alias to a file path via <see cref="FilespathProvider"/>
/// and loaded the image from disk; a missing/invalid file yielded a null
/// texture. Here "loading" is just validating that the alias is one of the
/// known sprites (the actual visuals are produced by the renderer). Everything
/// else (caching, null on failure, cache clearing) is preserved so the rest of
/// the game keeps treating a null texture as "asset missing".
/// </summary>
public static class AssetsManager
{
    private static readonly Dictionary<string, Texture?> TEXTURE_MAP = new();

    // The set of aliases this port knows how to render. Equivalent to "an image
    // file with this name exists in the active mod" in the original.
    private static readonly HashSet<string> KNOWN_ALIASES = new(StringComparer.Ordinal)
    {
        "void.png",
        "block.png",
        "wall.png",
        "mouse.png",
        "cat.png",
        "cat_awaiting.png",
        "cheese.png",
        "mousetrap.png",
        "hole.png",
    };

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
    /// Is the given alias a known/renderable sprite?
    /// </summary>
    public static bool IsKnownAlias(string alias) => KNOWN_ALIASES.Contains(alias);

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
        // FilespathProvider). The "load" succeeds if it is a known sprite.
        return KNOWN_ALIASES.Contains(path) ? new Texture(path) : null;
    }
}
