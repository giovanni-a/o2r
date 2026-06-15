using System.Text;

namespace OpenRodentsRevenge.Game;

/// <summary>A single built-in level: a name, a TXT layout and a cat count.</summary>
public sealed record CampaignLevel(string Name, string Layout, int CatCount);

/// <summary>
/// Built-in levels for "New Game" / level selection.
///
/// These are not part of the original C++ source (which only shipped a handful
/// of loose level files and an unfinished campaign concept). They are generated
/// here in the classic "Rodent's Revenge" arena style: a wall border, a ground
/// ring where the cats roam, and a solid field of pushable blocks with the mouse
/// carved into the centre — the same structure as the bundled <c>levels/1.txt</c>.
/// </summary>
public static class Campaign
{
    public static readonly IReadOnlyList<CampaignLevel> Levels = new[]
    {
        new CampaignLevel("1 - Backyard", Arena(13, 13, 2), 1),
        new CampaignLevel("2 - The Pantry", Arena(15, 15, 2), 2),
        new CampaignLevel("3 - The Cellar", Arena(17, 15, 2), 2),
        new CampaignLevel("4 - Warehouse", Arena(17, 17, 2), 3),
        new CampaignLevel("5 - The Maze", Arena(19, 17, 2), 4),
        new CampaignLevel("6 - Big Cheese", Arena(21, 19, 3), 4),
        new CampaignLevel("7 - Cat Alley", Arena(23, 21, 3), 5),
        new CampaignLevel("8 - Rodent's Revenge", Arena(23, 23, 2), 6),
    };

    /// <summary>
    /// Build a classic arena as TXT level content.
    /// </summary>
    /// <param name="w">Width in tiles.</param>
    /// <param name="h">Height in tiles.</param>
    /// <param name="ring">Thickness of the ground ring inside the wall border.</param>
    private static string Arena(int w, int h, int ring)
    {
        var sb = new StringBuilder();
        sb.Append($"X={w}\n");
        sb.Append($"Y={h}\n");
        int cx = w / 2, cy = h / 2;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int d = Math.Min(Math.Min(x, w - 1 - x), Math.Min(y, h - 1 - y));
                char c;
                if (d == 0)
                    c = '2'; // wall border
                else if (d <= ring)
                    c = '0'; // ground ring (where the cats roam)
                else
                    c = '1'; // pushable block core
                if (x == cx && y == cy)
                    c = 'M'; // mouse start position
                sb.Append(c);
            }
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
