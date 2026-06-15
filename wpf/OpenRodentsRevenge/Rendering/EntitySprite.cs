using System.Windows;
using System.Windows.Controls;
using OpenRodentsRevenge.Entities;

namespace OpenRodentsRevenge.Rendering;

/// <summary>
/// Binds a <see cref="TiledEntity"/> to a retained <see cref="FrameworkElement"/>
/// living in a <see cref="Canvas"/> layer, and keeps them in sync.
///
/// This is the heart of the OpenSilver-friendly, retained-mode rendering model:
/// the visual is created once and then only touched when something actually
/// changes — its position (entity moved) or its sprite (alias changed, e.g. a
/// cat turning into cheese). No per-frame repaint, no allocations on a steady
/// state, minimal DOM mutations under OpenSilver.
/// </summary>
public sealed class EntitySprite
{
    private readonly TiledEntity mEntity;
    private readonly double mOpacity;

    private Canvas? mLayer;
    private FrameworkElement? mVisual;
    private string? mLastAlias;
    private int mLastX = int.MinValue;
    private int mLastY = int.MinValue;

    public EntitySprite(TiledEntity entity, double opacity = 1.0)
    {
        mEntity = entity;
        mOpacity = opacity;
    }

    /// <summary>Attach this sprite to a canvas layer (creates the visual lazily on first Sync).</summary>
    public void Attach(Canvas layer)
    {
        mLayer = layer;
        mLastAlias = null;
        mLastX = int.MinValue;
        mLastY = int.MinValue;
    }

    /// <summary>Remove the visual from its layer.</summary>
    public void Detach()
    {
        if (mVisual != null && mLayer != null)
            mLayer.Children.Remove(mVisual);
        mVisual = null;
        mLastAlias = null;
    }

    /// <summary>Refresh the visual from the entity's current state (cheap no-op when unchanged).</summary>
    public void Sync()
    {
        if (mLayer == null)
            return;

        string alias = mEntity.TextureAlias;
        if (alias != mLastAlias)
        {
            if (mVisual != null)
                mLayer.Children.Remove(mVisual);
            mVisual = EntityVisuals.Create(alias);
            if (mVisual != null)
            {
                mVisual.Opacity = mOpacity;
                mLayer.Children.Add(mVisual);
            }
            mLastAlias = alias;
            mLastX = int.MinValue; // force a reposition
        }

        if (mVisual != null && (mEntity.X != mLastX || mEntity.Y != mLastY))
        {
            Canvas.SetLeft(mVisual, (double)mEntity.X * TiledEntity.TILE_SIZE);
            Canvas.SetTop(mVisual, (double)mEntity.Y * TiledEntity.TILE_SIZE);
            mLastX = mEntity.X;
            mLastY = mEntity.Y;
        }
    }
}
