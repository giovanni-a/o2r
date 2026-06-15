using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using OpenRodentsRevenge.Common;
using OpenRodentsRevenge.Entities;
using OpenRodentsRevenge.Logging;
using OpenRodentsRevenge.Managers;
using OpenRodentsRevenge.Map;

namespace OpenRodentsRevenge.Game;

/// <summary>
/// The editor screen. Direct port of the original <c>EditorScreen</c> class.
/// </summary>
public class EditorScreen : Screen
{
    public const char MOUSE_POS_CHAR = 'm';
    private static readonly Color MOUSE_POS_INDICATOR_COLOR = Color.FromArgb(200, 50, 50, 50);

    private char mPlaceableChar;
    private bool mPlaceableCharUpdated;
    private Vec2i mLastPlacedPos;

    // The original used an sf::Sprite tinted by MOUSE_POS_INDICATOR_COLOR.
    private Brush? mIndicatorBrush;
    private Vec2i mIndicatorPixelPos;

    public EditorScreen(IGameView window)
        : base(window)
    {
        mPlaceableChar = '\0';
        mPlaceableCharUpdated = false;
        mLastPlacedPos = new Vec2i(-1, -1);
    }

    public override void Render(DrawingContext dc)
    {
        if (mLevelPtr == null)
            return;
        mLevelPtr.Draw(dc);
        if (!mLevelPtr.Info.MouseRandomPos)
            DrawMousePosIndicator(dc);
    }

    public override void Update(double dt)
    {
        if (mPlaceableChar == '\0' || mLevelPtr == null ||
            !mWindow.IsLeftMouseButtonPressed())
            return;
        Vec2i mousePos = mWindow.GetMousePosition();
        if (mousePos.X < 0 || mousePos.Y < 0)
            return;
        mousePos = new Vec2i(mousePos.X / TiledEntity.TILE_SIZE, mousePos.Y / TiledEntity.TILE_SIZE);
        // Is a placing needed? (avoid useless computation)
        if (!mPlaceableCharUpdated && mLastPlacedPos == mousePos)
            return;
        // Is the mouse inside the map?
        if (!mLevelPtr.IsInsideMap(mousePos.X, mousePos.Y, true))
            return;
        // Place the mouse starting position...
        if (mPlaceableChar == MOUSE_POS_CHAR)
        {
            if (!mLevelPtr.IsInsideMap(mousePos.X, mousePos.Y, false))
                return;
            mLevelPtr.Info.MouseRandomPos = false;
            mLevelPtr.Info.MousePosX = (uint)mousePos.X;
            mLevelPtr.Info.MousePosY = (uint)mousePos.Y;
            RelocateMousePosIndicator();
        }
        // or a tile, if different from the current one
        else if (mLevelPtr.GetTileChar(mousePos.X, mousePos.Y) != mPlaceableChar)
        {
            mLevelPtr.SetTileChar(mousePos.X, mousePos.Y, mPlaceableChar, true, true);
        }
        mLastPlacedPos = mousePos;
        mPlaceableCharUpdated = false;
    }

    public override void HandleEvent(Key key)
    {
    }

    public override bool Start(TiledMap? level)
    {
        if (!base.Start(level))
            return false;
        Logger.Info($"EditorScreen : started editing level (name = \"{level!.Info.Name}\", filepath = \"{level.Info.FilePath}\").");

        // Init the mouse start position indicator (but shown only if needed)
        Texture? texturePtr = AssetsManager.GetTexture("mouse.png");
        if (texturePtr == null)
            return false;
        mIndicatorBrush = texturePtr.Brush;
        RelocateMousePosIndicator();

        return true;
    }

    /// <summary>
    /// Set the current character ID to place (mouse position or tile).
    /// </summary>
    public void SetPlaceableChar(char c)
    {
        if (c == '\0' || c == ' ' || c == mPlaceableChar)
            return;
        mPlaceableChar = c;
        mPlaceableCharUpdated = true;
    }

    private void RelocateMousePosIndicator()
    {
        if (mLevelPtr == null)
            return;
        var mousePos = new Vec2i((int)mLevelPtr.Info.MousePosX, (int)mLevelPtr.Info.MousePosY);
        if (!mLevelPtr.IsInsideMap(mousePos.X, mousePos.Y))
            return;
        if (mLevelPtr.GetTileInfo(mousePos.X, mousePos.Y).Type != TileInfo.TYPE_GROUND)
            mLevelPtr.SetTileChar(mousePos.X, mousePos.Y, '0', true, true);
        mIndicatorPixelPos = new Vec2i(mousePos.X * TiledEntity.TILE_SIZE,
                                       mousePos.Y * TiledEntity.TILE_SIZE);
    }

    private void DrawMousePosIndicator(DrawingContext dc)
    {
        if (mIndicatorBrush == null)
            return;
        var rect = new Rect(mIndicatorPixelPos.X, mIndicatorPixelPos.Y,
                            TiledEntity.TILE_SIZE, TiledEntity.TILE_SIZE);
        // Emulate the original sf::Sprite tinted by a dark, semi-transparent color.
        dc.PushOpacity(MOUSE_POS_INDICATOR_COLOR.A / 255.0);
        dc.DrawRectangle(mIndicatorBrush, null, rect);
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(120, 50, 50, 50)), null, rect);
        dc.Pop();
    }
}
