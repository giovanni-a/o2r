using System.Diagnostics;

namespace OpenRodentsRevenge.Common;

/// <summary>
/// Port of <c>sf::Clock</c>: measures elapsed time since construction or the
/// last <see cref="Restart"/>.
/// </summary>
public sealed class Clock
{
    private readonly Stopwatch mStopwatch = Stopwatch.StartNew();

    /// <summary>Elapsed time, in seconds, since the last restart.</summary>
    public double GetElapsedTimeAsSeconds() => mStopwatch.Elapsed.TotalSeconds;

    /// <summary>Restart the clock and return the elapsed time before the reset.</summary>
    public double Restart()
    {
        double elapsed = mStopwatch.Elapsed.TotalSeconds;
        mStopwatch.Restart();
        return elapsed;
    }
}
