namespace OpenRodentsRevenge.Common;

/// <summary>
/// Integer 2D vector, port of <c>sf::Vector2i</c>. Used for tile-unit positions.
/// </summary>
public readonly struct Vec2i : IEquatable<Vec2i>
{
    public Vec2i(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }

    public bool Equals(Vec2i other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is Vec2i other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(Vec2i a, Vec2i b) => a.Equals(b);

    public static bool operator !=(Vec2i a, Vec2i b) => !a.Equals(b);

    public override string ToString() => $"({X}, {Y})";
}
