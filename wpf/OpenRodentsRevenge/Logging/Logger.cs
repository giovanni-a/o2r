using System.Diagnostics;

namespace OpenRodentsRevenge.Logging;

/// <summary>
/// Minimal replacement for the original QsLog-based logging mechanism
/// (<c>QLOG_INFO</c>, <c>QLOG_WARN</c>, <c>QLOG_ERROR</c>).
///
/// The original game wired QsLog to both a file ("log.txt") and the debug
/// output. Here we forward everything to <see cref="Trace"/>, which is enough
/// to preserve behaviour without porting the whole QsLog library (deemed
/// non-important functionality for the port).
/// </summary>
public static class Logger
{
    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        Trace.WriteLine($"[{level}] {message}");
    }
}
