using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AStarDev.WallpaperScraper.TestsUnit.Fakes;

/// <summary>Creates an exception that has really been thrown, so its stack trace records the original throw site.</summary>
internal static class ThrownFailure
{
    public const string ThrowSite = nameof(RaiseOriginalFailure);

    public static InvalidOperationException Create(string message)
    {
        try
        {
            RaiseOriginalFailure(message);
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }

        throw new UnreachableException();
    }

    /// <summary>The stack trace of the exception, which is null (not empty) until it has been thrown.</summary>
    public static string TraceOf(Exception exception)
        => exception.StackTrace ?? string.Empty;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RaiseOriginalFailure(string message)
        => throw new InvalidOperationException(message);
}
