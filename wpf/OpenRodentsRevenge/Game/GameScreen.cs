using System.Windows.Controls;
using System.Windows.Input;
using OpenRodentsRevenge.Common;
using OpenRodentsRevenge.Entities;
using OpenRodentsRevenge.Logging;
using OpenRodentsRevenge.Map;
using OpenRodentsRevenge.Rendering;
using Mouse = OpenRodentsRevenge.Entities.Mouse;

namespace OpenRodentsRevenge.Game;

/// <summary>
/// The game screen. On start, places the Mouse and all AI entities.
/// Direct port of the original <c>GameScreen</c> class.
/// </summary>
public class GameScreen : Screen
{
    private const uint RANDOM_POS_MAX_ATTEMPTS = 500; // "bruteforce" work-around

    private enum Status { Playing, Won, Lost }

    private readonly Mouse mMouse = new(0, 0);
    private readonly List<Cat> mCats = new();
    private readonly List<Trap> mTraps = new();

    // Retained visuals (created in OnAttach, reconciled in Sync).
    private Canvas? mLayer;
    private EntitySprite? mMouseSprite;
    private readonly List<EntitySprite> mCatSprites = new();
    private readonly List<EntitySprite> mTrapSprites = new();

    private uint levelSizeX, levelSizeY;
    private int mCatCountToSpawn;
    private Status mStatus = Status.Playing;

    /// <summary>Raised once when every cat has been trapped (level cleared).</summary>
    public event Action? Won;

    /// <summary>Raised once when a cat catches the mouse.</summary>
    public event Action? Lost;

    public GameScreen(IGameView window)
        : base(window)
    {
    }

    /// <summary>
    /// Set how many cats to spawn on the next <see cref="Start"/>. The original
    /// 1.0 source left the "Place the cats" section empty; this drives it.
    /// </summary>
    public void PrepareCats(int count) => mCatCountToSpawn = count;

    public override void OnAttach(Canvas entityLayer)
    {
        mLayer = entityLayer;
        BuildSprites();
    }

    public override void OnDetach(Canvas entityLayer)
    {
        mMouseSprite?.Detach();
        mMouseSprite = null;
        foreach (EntitySprite sprite in mCatSprites)
            sprite.Detach();
        mCatSprites.Clear();
        foreach (EntitySprite sprite in mTrapSprites)
            sprite.Detach();
        mTrapSprites.Clear();
        mLayer = null;
    }

    public override void Sync()
    {
        mMouseSprite?.Sync();
        foreach (EntitySprite sprite in mCatSprites)
            sprite.Sync();
        foreach (EntitySprite sprite in mTrapSprites)
            sprite.Sync();
    }

    private void BuildSprites()
    {
        if (mLayer == null)
            return;
        mMouseSprite = new EntitySprite(mMouse);
        mMouseSprite.Attach(mLayer);
        mCatSprites.Clear();
        foreach (Cat cat in mCats)
        {
            var sprite = new EntitySprite(cat);
            sprite.Attach(mLayer);
            mCatSprites.Add(sprite);
        }
        mTrapSprites.Clear();
        foreach (Trap trap in mTraps)
        {
            var sprite = new EntitySprite(trap);
            sprite.Attach(mLayer);
            mTrapSprites.Add(sprite);
        }
        Sync();
    }

    public override void Update(double dt)
    {
        if (mStatus != Status.Playing)
            return;

        // Track which cells are occupied by cats so they never stack and they
        // block each other in (both live cats and cheese act as walls for
        // trapping). The set is kept current as each cat moves, so two cats
        // can't claim the same cell in a single tick.
        var occupied = new HashSet<Vec2i>();
        foreach (Cat cat in mCats)
            occupied.Add(new Vec2i(cat.X, cat.Y)); // includes cheese (static obstacles)

        for (int i = 0; i < mCats.Count; i++)
        {
            Cat cat = mCats[i];
            if (cat.IsCheese)
                continue; // cheese stays put and keeps blocking
            var from = new Vec2i(cat.X, cat.Y);
            occupied.Remove(from); // a cat doesn't block itself
            cat.Update(mLevelPtr, mMouse, occupied);
            occupied.Add(new Vec2i(cat.X, cat.Y));
        }

        // Lose : a (non-cheese) cat is on the mouse's tile.
        for (int i = 0; i < mCats.Count; i++)
        {
            Cat cat = mCats[i];
            if (!cat.IsCheese && cat.X == mMouse.X && cat.Y == mMouse.Y)
            {
                mStatus = Status.Lost;
                Lost?.Invoke();
                return;
            }
        }

        // Win : every cat has been trapped (turned into cheese).
        if (mCats.Count > 0)
        {
            bool allCheese = true;
            for (int i = 0; i < mCats.Count; i++)
            {
                if (!mCats[i].IsCheese)
                {
                    allCheese = false;
                    break;
                }
            }
            if (allCheese)
            {
                mStatus = Status.Won;
                Won?.Invoke();
            }
        }
    }

    public override void HandleEvent(Key key)
    {
        if (mLevelPtr == null)
            return;
        // Move the mouse, but only inside the level
        Vec2i moveOffset = mMouse.HandleEvent(key);
        if (moveOffset.X == 0 && moveOffset.Y == 0)
            return;
        var newPos = new Vec2i(mMouse.X + moveOffset.X, mMouse.Y + moveOffset.Y);
        if (mLevelPtr.IsInsideMap(newPos.X, newPos.Y))
            mMouse.Move(moveOffset.X, moveOffset.Y, mLevelPtr);
    }

    public override bool Start(TiledMap? level)
    {
        if (!base.Start(level))
            return false;

        Logger.Info($"Game Screen : playing level {level!.Info.FilePath} .");
        mMouse.LoadTexture();
        LevelInfo info = mLevelPtr!.Info;
        levelSizeX = mLevelPtr.SizeX;
        levelSizeY = mLevelPtr.SizeY;

        TilePosList groundTiles = mLevelPtr.GetTilesOfTypes(new[] { "GROUND" });

        // Place the mouse
        uint mouseX = 0, mouseY = 0;
        if (!info.MouseRandomPos &&
            mLevelPtr.GetTileChar((int)info.MousePosX, (int)info.MousePosY) == '0')
        {
            mouseX = info.MousePosX;
            mouseY = info.MousePosY;
        }
        else
        {
            Vec2i pos = RandomEmptyPos(groundTiles);
            if (!mLevelPtr.IsInsideMap(pos.X, pos.Y, false))
            {
                Logger.Error("GameScreen : CANNOT PLACE MOUSE");
                return false;
            }
            mouseX = (uint)pos.X;
            mouseY = (uint)pos.Y;
        }
        mMouse.SetX((int)mouseX).SetY((int)mouseY);

        // Place the cats (if needed)
        mCats.Clear();
        mTraps.Clear();
        mStatus = Status.Playing;
        var forbidden = new TilePosList { new Vec2i((int)mouseX, (int)mouseY) };
        for (int i = 0; i < mCatCountToSpawn; i++)
        {
            Vec2i pos = RandomEmptyPos(groundTiles, forbidden);
            if (!mLevelPtr.IsInsideMap(pos.X, pos.Y, false))
                break; // no room left for more cats
            var cat = new Cat(pos.X, pos.Y);
            cat.LoadTexture();
            mCats.Add(cat);
            forbidden.Add(pos);
        }

        // Place the traps (if needed)

        Logger.Info($"Game Screen : starting game ({mCats.Count} cats).");

        return true;
    }

    public override void ReloadTextures()
    {
        mMouse.LoadTexture();
        for (int i = 0; i < mCats.Count; i++)
            mCats[i].LoadTexture();
        for (int i = 0; i < mTraps.Count; i++)
            mTraps[i].LoadTexture();
        base.ReloadTextures();
    }

    private Vec2i RandomEmptyPos(TilePosList emptyTiles,
                                 TilePosList? forbiddenPos = null,
                                 uint attempts = 0)
    {
        forbiddenPos ??= new TilePosList();
        if (emptyTiles.Count == 0)
            return new Vec2i(-1, -1);
        int index = Random.Shared.Next(emptyTiles.Count);
        Vec2i pos = emptyTiles[index];
        if (forbiddenPos.Contains(pos))
        {
            if (attempts++ < RANDOM_POS_MAX_ATTEMPTS)
                return RandomEmptyPos(emptyTiles, forbiddenPos, attempts);
            return new Vec2i(-1, -1);
        }
        return pos;
    }
}
