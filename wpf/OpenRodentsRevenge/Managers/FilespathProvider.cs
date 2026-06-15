using OpenRodentsRevenge.Logging;

namespace OpenRodentsRevenge.Managers;

/// <summary>
/// Set of functions providing an asset filepath from an asset ID.
///
/// The original built its asset list from the enabled "mods" folders and could
/// override sprites per mod. The modding system is explicitly out of scope for
/// v1 of this port, so this is a simplified stand-in: an alias resolves to
/// itself, which <see cref="AssetsManager"/> then turns into a vector sprite.
/// The public surface is kept so the rest of the code (and a future mod port)
/// stays structurally identical to the original.
/// </summary>
public static class FilespathProvider
{
    private static readonly List<string> MODS_LIST = new();

    /// <summary>
    /// Get an asset path from its name. In this port the alias is its own path.
    /// </summary>
    public static string AssetPathFromAlias(string alias) => alias;

    /// <summary>
    /// Refresh the assets list. No-op in this port (sprites are generated).
    /// </summary>
    public static void RefreshAssetsList()
    {
        Logger.Info("FilespathProvider : assets are generated sprites (modding disabled in v1).");
    }

    public static IReadOnlyList<string> ModsList() => MODS_LIST;

    public static void AddMods(IEnumerable<string> mods, bool resetModsList)
    {
        if (resetModsList)
            MODS_LIST.Clear();
        MODS_LIST.AddRange(mods);
    }

    public static void SetMainModFolder(string folder)
    {
        Logger.Info($"FilespathProvider : main mod set to {folder} (no-op in v1).");
    }

    public static void SetModsLocation(string path)
    {
        Logger.Info($"FilespathProvider : mods location set to {path} (no-op in v1).");
    }

    public static void SetAssetsNameFilters(IEnumerable<string> nameFilters)
    {
        // No-op: the port does not scan the filesystem for assets.
    }
}
