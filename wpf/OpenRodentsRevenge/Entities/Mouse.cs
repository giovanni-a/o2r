using System.Windows.Input;
using OpenRodentsRevenge.Common;
using OpenRodentsRevenge.Managers;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Entities;

/// <summary>
/// Mouse controlled by the player. Direct port of the original <c>Mouse</c>.
/// </summary>
public class Mouse : TiledEntity
{
    public Mouse(int x, int y)
        : base(x, y, "mouse.png")
    {
    }

    /// <summary>
    /// Compute movement order from a key press. Returns the movement order, in
    /// tiles units (zero vector if the key is not a direction).
    /// </summary>
    public Vec2i HandleEvent(Key key)
    {
        switch (key)
        {
            case Key.Up:
                return new Vec2i(0, -1);
            case Key.Down:
                return new Vec2i(0, 1);
            case Key.Left:
                return new Vec2i(-1, 0);
            case Key.Right:
                return new Vec2i(1, 0);
            default:
                return new Vec2i();
        }
    }

    /// <summary>
    /// Move the mouse, taking into account its environment.
    /// </summary>
    public void Move(int dx, int dy, TiledMap level)
    {
        int newX = mX + dx, newY = mY + dy;
        TileInfo tileInfo = level.GetTileInfo(newX, newY);
        // Ground tile : move
        if (tileInfo.Type == TileInfo.TYPE_GROUND)
        {
            mX = newX;
            mY = newY;
        }
        // Block tile : move it if possible
        else if (tileInfo.Type == TileInfo.TYPE_BLOCK)
        {
            int blockEndX = newX, blockEndY = newY;
            do
            {
                blockEndX += dx;
                blockEndY += dy;
            } while (level.GetTileInfo(blockEndX, blockEndY).Type == TileInfo.TYPE_BLOCK);
            if (level.GetTileInfo(blockEndX, blockEndY).Type != TileInfo.TYPE_GROUND)
                return;
            level.SetTileChar(newX, newY, '0', false);
            level.SetTileChar(blockEndX, blockEndY, '1', true);
            mX = newX;
            mY = newY;
        }
    }
}
