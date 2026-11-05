using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

internal static class UnitTask
{
    public static async Task<Unit> RunAsync(Func<Task> operation)
    {
        await operation().ConfigureAwait(false);

        return Unit.Instance;
    }
}
