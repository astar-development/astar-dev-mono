namespace AStarDev.Utilities;

/// <summary>Provides extension methods for describing exceptions.</summary>
public static class ExceptionExtensions
{
    private const string CausedBy = " Caused by: ";

    extension(Exception exception)
    {
        /// <summary>Describes the exception and every exception nested inside it, outermost first, so the real cause is not hidden behind messages such as "See the inner exception for details".</summary>
        /// <returns>The messages joined with "Caused by:". Repeated and blank inner messages are skipped.</returns>
        public string ToMessageChain()
        {
            List<string> messages = [exception.Message];
            for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(inner.Message) && inner.Message != messages[^1]) messages.Add(inner.Message);
            }

            return string.Join(CausedBy, messages);
        }
    }
}
