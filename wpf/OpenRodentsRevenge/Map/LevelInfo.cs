namespace OpenRodentsRevenge.Map;

/// <summary>
/// LevelInfo contains all the information needed in order to form a complete
/// level: name, author, creation date, (optional) external LES file, and mouse
/// start position information. Each <see cref="TiledMap"/> has its own LevelInfo.
///
/// Direct port of the original <c>LevelInfo</c> class.
/// </summary>
public class LevelInfo
{
    public LevelInfo()
    {
        Name = string.Empty;
        Author = string.Empty;
        Date = DateTime.Today;
        FilePath = string.Empty;
        LesFilePath = string.Empty;
        MousePosX = 0;
        MousePosY = 0;
        MouseRandomPos = false;
    }

    public string Name { get; set; }
    public string Author { get; set; }
    public DateTime Date { get; set; }
    public string FilePath { get; set; }
    public string LesFilePath { get; set; }
    public uint MousePosX { get; set; }
    public uint MousePosY { get; set; }
    public bool MouseRandomPos { get; set; }
}
