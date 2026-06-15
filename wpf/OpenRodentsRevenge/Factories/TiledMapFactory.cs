using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using OpenRodentsRevenge.Entities;
using OpenRodentsRevenge.Game;
using OpenRodentsRevenge.Logging;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Factories;

/// <summary>
/// An option (name, value) pair, port of <c>typedef QPair&lt;QString, QString&gt; Option;</c>.
/// </summary>
public readonly struct Option
{
    public Option(string first, string second)
    {
        First = first;
        Second = second;
    }

    public string First { get; }
    public string Second { get; }
}

/// <summary>
/// Static class able to load/save a TiledMap. Direct port of the original
/// <c>TiledMapFactory</c>. XML manipulation uses <see cref="System.Xml"/> in
/// place of QtXml's DOM.
/// </summary>
public static class TiledMapFactory
{
    private const uint DSIZE_X = 23, DSIZE_Y = 23;
    private const string XML_FILE_SUFFIX = "xml";
    private const string OPTION_TXT_SIZE_X = "x";
    private const string OPTION_XML_SIZE_X = "sizeX";
    private const string OPTION_TXT_SIZE_Y = "y";
    private const string OPTION_XML_SIZE_Y = "sizeY";
    private const string OPTION_NAME = "name";
    private const string OPTION_AUTHOR = "author";
    private const string OPTION_RANDOM_MOUSE_POS = "randomMousePos";
    private const char TXT_CHAR_MOUSE = EditorScreen.MOUSE_POS_CHAR;
    private const char TXT_OPTION_SEP = '=';
    private const char DEFAULT_TILE = '0';

    /// <summary>Return the longest tile line size.</summary>
    public static uint ComputeLevelSizeX(TiledMap level)
    {
        int maxSizeX = 0;
        for (int i = 0; i < level.mTiles.Count; i++)
        {
            int tilesLineSize = level.mTiles[i].Count;
            if (tilesLineSize > maxSizeX)
                maxSizeX = tilesLineSize;
        }
        return (uint)maxSizeX;
    }

    public static TiledMap? LoadLevel(string path)
    {
        var level = new TiledMap(0, 0);
        try
        {
            if (!File.Exists(path))
                throw new InvalidOperationException("file does not exists");
            string suffix = Suffix(path);
            if (suffix == XML_FILE_SUFFIX)
                LoadMapXmlFormat(level, path);
            else
                LoadMapTxtFormat(level, path);
            FinalizeLoad(level, path);
        }
        catch (Exception e)
        {
            Logger.Error($"TiledMapFactory : cannot load level {path} : {e.Message} .");
            return null;
        }
        return level;
    }

    /// <summary>
    /// Load a TXT-format level from an in-memory string (used for the built-in
    /// campaign levels). Mirrors <see cref="LoadLevel"/> minus the file IO.
    /// </summary>
    public static TiledMap? LoadLevelFromText(string content, string name)
    {
        var level = new TiledMap(0, 0);
        try
        {
            string[] lines = content.Replace("\r\n", "\n").Split('\n');
            ProcessTxtLines(level, lines);
            FinalizeLoad(level, string.Empty);
            level.mInfo.Name = name;
        }
        catch (Exception e)
        {
            Logger.Error($"TiledMapFactory : cannot load built-in level {name} : {e.Message} .");
            return null;
        }
        return level;
    }

    private static void FinalizeLoad(TiledMap level, string filePath)
    {
        if (level.mTiles.Count == 0)
            throw new InvalidOperationException("reading error");

        // Level size fix
        uint cSizeX = ComputeLevelSizeX(level); // computed sizes
        uint cSizeY = (uint)level.mTiles.Count;
        if (level.mSizeX == 0)
        {
            level.mSizeX = cSizeX;
            Logger.Warn("No level X size specified : setting X size to " + level.mSizeX);
        }
        else if (level.mSizeX != cSizeX)
        {
            level.mSizeX = cSizeX;
            Logger.Warn("Incorrect specified level X size : setting X size to " + level.mSizeX);
        }
        if (level.mSizeY == 0)
        {
            level.mSizeY = cSizeY;
            Logger.Warn("No level Y size specified : setting Y size to " + level.mSizeY);
        }
        else if (level.mSizeY != (uint)level.mTiles.Count)
        {
            level.mSizeY = cSizeY;
            Logger.Warn("Incorrect specified level Y size : setting Y size to " + level.mSizeY);
        }

        LevelInfo info = level.mInfo;

        // Level path
        info.FilePath = filePath;

        // Level name
        if (string.IsNullOrEmpty(info.Name))
            Logger.Warn("No author specified.");
        else
            Logger.Info($"Name is \"{info.Name}\"");

        // Level author
        if (string.IsNullOrEmpty(info.Author))
            Logger.Warn("No author specified.");
        else
            Logger.Info($"Author is \"{info.Author}\"");
    }

    private static void LoadMapTxtFormat(TiledMap level, string path)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch
        {
            throw new InvalidOperationException("file not readable");
        }
        ProcessTxtLines(level, lines);
    }

    private static void ProcessTxtLines(TiledMap level, string[] lines)
    {
        // Read the level file
        uint lineCount = 0, nbOfOptions = 0;
        foreach (string line in lines)
        {
            // If empty : ignore
            if (string.IsNullOrEmpty(line))
                continue;
            // Increment line number
            ++lineCount;
            // If option : interpret
            Option option = ProcessTxtOptionLine(line);
            if (!string.IsNullOrEmpty(option.First))
            {
                if (!InterpretOption(option, level, true))
                    Logger.Warn($"TiledMapFactory : invalid level option (name = {option.First}  value = {option.Second}).");
                ++nbOfOptions;
            }
            // Else : interpret line of tiles
            else if (!ProcessTilesLine(line, level, lineCount - nbOfOptions, true))
            {
                throw new InvalidOperationException(
                    $"invalid tiles line n°{lineCount - nbOfOptions} (\"{line}\")");
            }
        }
        Logger.Info($"TiledMapFactory : finished reading TXT level ({lineCount - nbOfOptions} tiles lines and {nbOfOptions} options lines)");
    }

    private static void LoadMapXmlFormat(TiledMap level, string path)
    {
        var doc = new XmlDocument();
        try
        {
            doc.Load(path);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException(
                $"parsing error ; {ex.Message} (line {ex.LineNumber}, column {ex.LinePosition})");
        }
        catch
        {
            throw new InvalidOperationException("file not readable");
        }

        XmlElement? levelRoot = FirstChildElement(doc, "Level");
        if (levelRoot == null)
            throw new InvalidOperationException("no <Level> root element found");

        // Read the options
        if (levelRoot.Attributes != null)
        {
            foreach (XmlAttribute option in levelRoot.Attributes)
                InterpretOption(new Option(option.Name, option.Value), level, false);
        }

        // Read the mouse position info
        XmlElement? mouseNode = FirstChildElement(levelRoot, "Mouse");
        if (mouseNode != null)
        {
            if (mouseNode.HasAttribute("x"))
            {
                if (TryParseUInt(mouseNode.GetAttribute("x"), out uint x))
                    level.Info.MousePosX = x;
                else
                    Logger.Warn($"TiledMapFactory : invalid mouse X position '{mouseNode.GetAttribute("x")}'");
            }
            if (mouseNode.HasAttribute("y"))
            {
                if (TryParseUInt(mouseNode.GetAttribute("y"), out uint y))
                    level.Info.MousePosY = y;
                else
                    Logger.Warn($"TiledMapFactory : invalid mouse Y position '{mouseNode.GetAttribute("y")}'");
            }
            if (mouseNode.HasAttribute(OPTION_RANDOM_MOUSE_POS))
                InterpretOption(new Option(OPTION_RANDOM_MOUSE_POS,
                                           mouseNode.GetAttribute(OPTION_RANDOM_MOUSE_POS)),
                                level, false);
        }
        else
        {
            Logger.Warn("TiledMapFactory : no <Mouse> node (mouse start position infos)");
        }

        // Interpret lines of tiles
        XmlElement? tilesNode = FirstChildElement(levelRoot, "Tiles");
        if (tilesNode == null)
            throw new InvalidOperationException("no <Tiles> element found");
        uint lineNb = 0;
        XmlNodeList lineNodes = tilesNode.GetElementsByTagName("line");
        for (int i = 0; i < lineNodes.Count; i++)
        {
            string line = lineNodes[i]!.InnerText;
            if (!ProcessTilesLine(line, level, ++lineNb, false))
                throw new InvalidOperationException($"invalid tiles line n°{lineNb} (\"{line}\")");
        }
    }

    // Ex. : "X=35" ==> Option("X", "35")
    private static Option ProcessTxtOptionLine(string line)
    {
        string[] split = line.Split(TXT_OPTION_SEP);
        if (split.Length > 1)
            return new Option(split[0], split[1]);
        return new Option(string.Empty, string.Empty);
    }

    private static bool InterpretOption(Option option, TiledMap lvl, bool oldFormat)
    {
        if (string.IsNullOrEmpty(option.First) || string.IsNullOrEmpty(option.Second))
            return false;
        LevelInfo info = lvl.mInfo;
        // X size
        if (string.Equals(option.First, oldFormat ? OPTION_TXT_SIZE_X : OPTION_XML_SIZE_X,
                          StringComparison.OrdinalIgnoreCase))
        {
            if (TryParseUInt(option.Second, out uint number))
            {
                if (number < TiledMap.SIZE_MAX_LIMIT_X)
                {
                    lvl.mSizeX = number;
                    Logger.Info($"Level X size set to {number} .");
                }
                else
                {
                    lvl.mSizeX = number;
                    Logger.Warn($"Excessive X level size ({number}) : level X size set to limit ({TiledMap.SIZE_MAX_LIMIT_X})");
                }
                return true;
            }
            return false;
        }
        // Y size
        if (string.Equals(option.First, oldFormat ? OPTION_TXT_SIZE_Y : OPTION_XML_SIZE_Y,
                          StringComparison.OrdinalIgnoreCase))
        {
            if (TryParseUInt(option.Second, out uint number))
            {
                // NOTE: the original compares against SIZE_MAX_LIMIT_X here too; preserved verbatim.
                if (number < TiledMap.SIZE_MAX_LIMIT_X)
                {
                    lvl.mSizeY = number;
                    Logger.Info($"Level Y size set to {number} .");
                }
                else
                {
                    lvl.mSizeY = number;
                    Logger.Warn($"Excessive Y level size ({number}) : level Y size set to limit ({TiledMap.SIZE_MAX_LIMIT_Y})");
                }
                return true;
            }
            return false;
        }
        // Mouse random position
        if (option.First == OPTION_RANDOM_MOUSE_POS)
        {
            if (option.Second == "true" || option.Second == "1")
            {
                info.MouseRandomPos = true;
            }
            else if (option.Second == "false" || option.Second == "0")
            {
                info.MouseRandomPos = false;
            }
            else
            {
                Logger.Warn($"TiledMapFactory : the option {option.First} has an invalid value ('{option.Second}'");
                return false;
            }
            return true;
        }
        // Author
        if (option.First == OPTION_AUTHOR)
        {
            info.Author = option.Second;
            return true;
        }
        // Name
        if (option.First == OPTION_NAME)
        {
            info.Name = option.Second;
            return true;
        }

        return false;
    }

    private static bool ProcessTilesLine(string line, TiledMap lvl, uint lineNb, bool oldFormat)
    {
        var tilesLine = new List<Tile>();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            // Old format : if mouse pos, place the mouse and add a default tile there
            if (oldFormat && char.ToLowerInvariant(c) == char.ToLowerInvariant(TXT_CHAR_MOUSE))
            {
                lvl.mInfo.MousePosX = (uint)i;
                lvl.mInfo.MousePosY = lineNb - 1;
                tilesLine.Add(new Tile(i, (int)(lineNb - 1), DEFAULT_TILE, false));
            }
            // Invalid tile : place default tile
            else if (c == '\0' || c == ' ')
            {
                tilesLine.Add(new Tile(i, (int)(lineNb - 1), DEFAULT_TILE, false));
            }
            // Valid tile : place the tile
            else
            {
                tilesLine.Add(new Tile(i, (int)(lineNb - 1), c, false));
            }
        }
        lvl.mTiles.Add(tilesLine);
        return true;
    }

    public static bool SaveLevel(TiledMap level, string path)
    {
        // File extension check
        string suffix = Suffix(path);
        if (string.IsNullOrEmpty(suffix))
        {
            Logger.Error($"TiledMapFactory : cannot save level to \"{path}\" (invalid extension).");
            return false;
        }
        // Save the level
        bool success;
        level.mInfo.FilePath = path;
        if (suffix == XML_FILE_SUFFIX)
            success = SaveMapXmlFormat(level);
        else
            success = SaveMapTxtFormat(level);
        if (!success)
        {
            Logger.Error($"TiledMapFactory : cannot save level to \"{path}\" (writing error).");
            return false;
        }
        return true;
    }

    private static bool SaveMapTxtFormat(TiledMap level)
    {
        LevelInfo info = level.Info;
        var sb = new StringBuilder();

        // Write options
        sb.Append(FormTxtOptionLine(OPTION_TXT_SIZE_X, level.SizeX.ToString(CultureInfo.InvariantCulture)));
        sb.Append(FormTxtOptionLine(OPTION_TXT_SIZE_Y, level.SizeY.ToString(CultureInfo.InvariantCulture)));
        if (!string.IsNullOrEmpty(info.Name))
            sb.Append(FormTxtOptionLine(OPTION_NAME, info.Name));
        if (!string.IsNullOrEmpty(info.Author))
            sb.Append(FormTxtOptionLine(OPTION_AUTHOR, info.Author));

        // Write lines of tiles
        List<List<Tile>> tiles = level.mTiles;
        for (int i = 0; i < tiles.Count; i++)
        {
            List<Tile> list = tiles[i];
            for (int j = 0; j < list.Count; j++)
            {
                // Old format : place mouse pos char ('m' or 'M') if no random pos
                if (j == (int)info.MousePosX && i == (int)info.MousePosY && !info.MouseRandomPos)
                    sb.Append(TXT_CHAR_MOUSE);
                // Place a tile char
                else
                    sb.Append(list[j].GetChar());
            }
            // avoid empty line at the end (not really important though)
            if (i != tiles.Count)
                sb.Append('\n');
        }

        try
        {
            File.WriteAllText(info.FilePath, sb.ToString());
        }
        catch
        {
            return false;
        }
        Logger.Info($"TiledMapFactory : finished saving TXT level to \"{info.FilePath}\".");
        return true;
    }

    private static string FormTxtOptionLine(string name, string value)
    {
        return name + TXT_OPTION_SEP + value + '\n';
    }

    private static bool SaveMapXmlFormat(TiledMap level)
    {
        LevelInfo info = level.Info;
        try
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false),
            };
            using var writer = XmlWriter.Create(info.FilePath, settings);
            writer.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");

            // Add the "Level" root element, with its optional options if needed
            writer.WriteStartElement("Level");
            writer.WriteAttributeString(OPTION_XML_SIZE_X, level.SizeX.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString(OPTION_XML_SIZE_Y, level.SizeY.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(info.Name))
                writer.WriteAttributeString(OPTION_NAME, info.Name);
            if (!string.IsNullOrEmpty(info.Author))
                writer.WriteAttributeString(OPTION_AUTHOR, info.Author);

            // Write mouse position info
            writer.WriteStartElement("Mouse");
            writer.WriteAttributeString("x", info.MousePosX.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("y", info.MousePosY.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString(OPTION_RANDOM_MOUSE_POS, info.MouseRandomPos ? "true" : "false");
            writer.WriteEndElement(); // Mouse

            // Write lines of tiles
            writer.WriteStartElement("Tiles");
            List<List<Tile>> tiles = level.mTiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                List<Tile> list = tiles[i];
                var line = new StringBuilder();
                for (int j = 0; j < list.Count; j++)
                    line.Append(list[j].GetChar());
                writer.WriteElementString("line", line.ToString());
            }
            writer.WriteEndElement(); // Tiles

            writer.WriteEndElement(); // Level
            writer.Flush();
        }
        catch
        {
            return false;
        }
        return true;
    }

    // QString::section('.', -1) equivalent: text after the last '.', or the
    // whole string if there is none.
    private static string Suffix(string path)
    {
        int idx = path.LastIndexOf('.');
        return idx < 0 ? path : path.Substring(idx + 1);
    }

    private static XmlElement? FirstChildElement(XmlNode parent, string name)
    {
        foreach (XmlNode child in parent.ChildNodes)
            if (child is XmlElement element && element.Name == name)
                return element;
        return null;
    }

    private static bool TryParseUInt(string value, out uint result)
    {
        return uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
    }
}
