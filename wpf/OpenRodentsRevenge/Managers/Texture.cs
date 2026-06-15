using System.Windows.Media;

namespace OpenRodentsRevenge.Managers;

/// <summary>
/// A drawable texture for a single tile/entity cell.
///
/// In the original SFML game this wrapped an <c>sf::Texture</c> loaded from an
/// image file. The proprietary sprite images are not shipped with the source
/// repository (only mod <c>credit.txt</c> files remain), so this port renders
/// equivalent vector sprites generated in code (see <see cref="SpriteLibrary"/>).
/// The public contract is the same: a texture either exists (valid) or is null.
/// </summary>
public sealed class Texture
{
    public Texture(string alias, Brush brush)
    {
        Alias = alias;
        Brush = brush;
    }

    /// <summary>Texture alias this was loaded from (ex: "mouse.png").</summary>
    public string Alias { get; }

    /// <summary>Frozen brush used to fill the entity's 16x16 cell.</summary>
    public Brush Brush { get; }
}
