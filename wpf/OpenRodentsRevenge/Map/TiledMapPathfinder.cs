using OpenRodentsRevenge.Common;
using OpenRodentsRevenge.Managers;

namespace OpenRodentsRevenge.Map;

/// <summary>
/// Pathfinding class able to compute a path between a start and an end position.
///
/// The original used the MicroPather A* library. Porting MicroPather's memory
/// pool verbatim was deemed unnecessary (the cat AI, while ported, is never
/// placed by the incomplete 1.0 game logic, so pathfinding is not exercised in
/// normal play). Instead this is a faithful A* reimplementation that mirrors the
/// original graph definition exactly:
/// <list type="bullet">
/// <item>States are tile positions encoded as <c>y * sizeX + x</c>.</item>
/// <item>Adjacency: the 8 surrounding tiles, only if inside the map and of type
/// GROUND; every step costs 1.</item>
/// <item>Heuristic: squared Euclidean distance (as in the original
/// <c>LeastCostEstimate</c>).</item>
/// </list>
/// The returned path includes both the start and end states, matching
/// MicroPather's output contract.
/// </summary>
public class TiledMapPathfinder
{
    public enum Result
    {
        SOLVED = 0,
        NO_SOLUTION = 1,
        START_END_SAME = 2,
    }

    private readonly TiledMap mLevelRef;

    // Cells that are walkable terrain but currently occupied (e.g. by other
    // cats), set for the duration of a single ComputePath call. The cat AI uses
    // this so cats path around each other instead of stacking. The end (target)
    // cell is always allowed, so a cat can still path onto the mouse.
    private IReadOnlySet<Vec2i>? mBlocked;
    private Vec2i mEnd;

    public TiledMapPathfinder(TiledMap level)
    {
        mLevelRef = level;
    }

    /// <summary>
    /// Find a path between start and end positions in the given map.
    /// </summary>
    /// <param name="blocked">Optional set of otherwise-walkable cells that are
    /// currently occupied and must be avoided (the end cell is never blocked).</param>
    public Result ComputePath(Vec2i start, Vec2i end, List<Vec2i> path,
                              IReadOnlySet<Vec2i>? blocked = null)
    {
        path.Clear();
        if (start == end)
            return Result.START_END_SAME;

        mBlocked = blocked;
        mEnd = end;

        var open = new PriorityQueue<Vec2i, float>();
        var gScore = new Dictionary<Vec2i, float>();
        var cameFrom = new Dictionary<Vec2i, Vec2i>();
        var closed = new HashSet<Vec2i>();

        gScore[start] = 0f;
        open.Enqueue(start, LeastCostEstimate(start, end));

        var adjacent = new List<Vec2i>();
        while (open.Count > 0)
        {
            Vec2i current = open.Dequeue();
            if (current == end)
            {
                ReconstructPath(cameFrom, current, start, path);
                return Result.SOLVED;
            }
            if (!closed.Add(current))
                continue; // stale duplicate already processed

            AdjacentCost(current, adjacent);
            float currentG = gScore[current];
            for (int i = 0; i < adjacent.Count; i++)
            {
                Vec2i neighbor = adjacent[i];
                if (closed.Contains(neighbor))
                    continue;
                float tentativeG = currentG + 1f; // every tile has the same cost
                if (!gScore.TryGetValue(neighbor, out float knownG) || tentativeG < knownG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    open.Enqueue(neighbor, tentativeG + LeastCostEstimate(neighbor, end));
                }
            }
        }

        return Result.NO_SOLUTION;
    }

    /// <summary>
    /// Must be called whenever the map changes. The reimplemented pather keeps
    /// no cross-call state, so this is a no-op (each solve is computed fresh).
    /// </summary>
    public void Reset()
    {
    }

    /// <summary>
    /// Least possible cost between 2 states (squared Euclidean distance, as in
    /// the original for better performance).
    /// </summary>
    public float LeastCostEstimate(Vec2i start, Vec2i end)
    {
        int dx = end.X - start.X, dy = end.Y - start.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Exact cost from the given state to all its (walkable) neighboring states.
    /// </summary>
    public void AdjacentCost(Vec2i pos, List<Vec2i> adjacent)
    {
        adjacent.Clear();
        // Initial check
        if (!mLevelRef.IsInsideMap(pos.X, pos.Y))
            return;
        // Look through the 8 adjacent tiles
        for (int i = pos.X - 1; i <= pos.X + 1; i++)
        {
            for (int j = pos.Y - 1; j <= pos.Y + 1; j++)
            {
                // If current pos : ignore
                if (i == pos.X && j == pos.Y)
                    continue;

                TileInfo tileInfo = mLevelRef.GetTileInfo(i, j);
                // If outside the map / invalid : ignore
                if (!tileInfo.IsValid)
                    continue;
                // If uncrossable : ignore
                if (tileInfo.Type != TileInfo.TYPE_GROUND)
                    continue;
                var neighbor = new Vec2i(i, j);
                // If occupied by another entity (but not the target) : ignore
                if (mBlocked != null && neighbor != mEnd && mBlocked.Contains(neighbor))
                    continue;
                adjacent.Add(neighbor);
            }
        }
    }

    public Vec2i StateToPos(int state)
    {
        int index = state, y = index / (int)mLevelRef.SizeX;
        return new Vec2i(index - y * (int)mLevelRef.SizeX, y);
    }

    public int PosToState(Vec2i pos)
    {
        return pos.Y * (int)mLevelRef.SizeX + pos.X;
    }

    private static void ReconstructPath(Dictionary<Vec2i, Vec2i> cameFrom, Vec2i current,
                                        Vec2i start, List<Vec2i> path)
    {
        var reversed = new List<Vec2i> { current };
        while (current != start && cameFrom.TryGetValue(current, out Vec2i previous))
        {
            current = previous;
            reversed.Add(current);
        }
        reversed.Reverse();
        path.AddRange(reversed);
    }
}
