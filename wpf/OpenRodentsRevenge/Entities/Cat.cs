using OpenRodentsRevenge.Common;
using OpenRodentsRevenge.Managers;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Entities;

/// <summary>
/// The cat is the enemy in Open Rodent's Revenge.
/// Using <see cref="TiledMapPathfinder"/>, it tracks the mouse (the player) and
/// tries to eat it. If the player manages to trap the cat, it is transformed
/// into cheese.
///
/// This is based on the original <c>Cat</c> class but completes the parts the
/// original 1.0 source left unfinished: <c>isBlocked()</c> was a stub that
/// always returned <c>true</c>, the cheese transformation was never carried out,
/// and cats were never even placed by <c>GameScreen</c>. The timing constants
/// (<see cref="MOVE_TIME"/>, <see cref="WAITING_TIME"/>) and the chase/wait flow
/// are kept from the original.
/// </summary>
public class Cat : TiledEntity
{
    private const double MOVE_TIME = 0.75;    // time between each move, in s
    private const double WAITING_TIME = 3.0;  // time trapped before cheese transformation

    private TilePosList mTrackingPath = new();
    private readonly Clock mTrackingClock = new();
    private readonly Clock mWaitingClock = new();
    private bool mBlocked;

    public Cat(int x, int y)
        : base(x, y, "cat.png")
    {
        mBlocked = false;
    }

    /// <summary>Has this cat been trapped and turned into cheese?</summary>
    public bool IsCheese { get; private set; }

    /// <summary>
    /// Update the cat. Moves at most once every <see cref="MOVE_TIME"/> seconds.
    /// </summary>
    public void Update(TiledMap? level, Mouse mouse)
    {
        if (level == null || IsCheese)
            return;

        // Track clock : determines cat speed
        if (mTrackingClock.GetElapsedTimeAsSeconds() < MOVE_TIME)
            return;
        mTrackingClock.Restart();

        // Try to track the mouse
        var mousePos = new Vec2i(mouse.X, mouse.Y);
        mTrackingPath = level.ComputePath(new Vec2i(mX, mY), mousePos);

        // A solved path contains [current, step1, step2, ...]; index 1 is the
        // next tile to move onto (which may be the mouse itself = capture).
        if (mTrackingPath.Count >= 2)
        {
            Unblock();
            Vec2i next = mTrackingPath[1];
            mX = next.X;
            mY = next.Y;
            return;
        }

        // No path to the mouse: either wander, or wait if completely trapped.
        List<Vec2i> moves = FreeNeighbors(level);
        if (moves.Count == 0) // trapped
        {
            if (!mBlocked)
            {
                mBlocked = true;
                mWaitingClock.Restart();
                SetTextureAlias("cat_awaiting.png");
            }
            else if (mWaitingClock.GetElapsedTimeAsSeconds() >= WAITING_TIME)
            {
                IsCheese = true;
                SetTextureAlias("cheese.png");
            }
            return;
        }

        // Free to roam : move to a random adjacent ground tile.
        Unblock();
        Vec2i move = moves[Random.Shared.Next(moves.Count)];
        mX = move.X;
        mY = move.Y;
    }

    private void Unblock()
    {
        if (mBlocked)
        {
            SetTextureAlias("cat.png");
            mBlocked = false;
        }
    }

    /// <summary>The 8-adjacent ground tiles the cat can step onto.</summary>
    private List<Vec2i> FreeNeighbors(TiledMap level)
    {
        var result = new List<Vec2i>();
        for (int i = mX - 1; i <= mX + 1; i++)
        {
            for (int j = mY - 1; j <= mY + 1; j++)
            {
                if (i == mX && j == mY)
                    continue;
                TileInfo info = level.GetTileInfo(i, j);
                if (info.IsValid && info.Type == TileInfo.TYPE_GROUND)
                    result.Add(new Vec2i(i, j));
            }
        }
        return result;
    }
}
