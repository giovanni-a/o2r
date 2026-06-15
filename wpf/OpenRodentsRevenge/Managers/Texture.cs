namespace OpenRodentsRevenge.Managers;

/// <summary>
/// A loaded texture handle for a single tile/entity alias.
///
/// In the original SFML game this wrapped an <c>sf::Texture</c> loaded from an
/// image file. The proprietary sprite images are not shipped with the source
/// repository (only mod <c>credit.txt</c> files remain), so this port renders
/// equivalent vector visuals generated in code (see
/// <see cref="OpenRodentsRevenge.Rendering.TilePalette"/> for tiles and
/// <see cref="OpenRodentsRevenge.Rendering.EntityVisuals"/> for entities).
///
/// The public contract is the same as the original: a texture either exists
/// (valid) or is null. We deliberately keep this a thin validity token rather
/// than holding a WPF <c>Brush</c>: the OpenSilver-friendly renderer builds its
/// visuals from <see cref="System.Windows.Shapes"/> elements (retained mode),
/// not from immediate-mode brushes.
/// </summary>
public sealed class Texture
{
    public Texture(string alias)
    {
        Alias = alias;
    }

    /// <summary>Texture alias this was loaded from (ex: "mouse.png").</summary>
    public string Alias { get; }
}
