using System.Reflection;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.TestHelpers.Common;

public static class RecoveryBlockerCases
{
    public static IReadOnlyList<RecoveryBlocker> All() =>
        [.. typeof(RecoveryBlockerFactory).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.ReturnType == typeof(RecoveryBlocker) && method.GetParameters().Length == 0)
            .Select(method => (RecoveryBlocker)method.Invoke(null, null)!)];

    public static int Declared() => typeof(RecoveryBlocker).GetNestedTypes().Count(type => type.IsSubclassOf(typeof(RecoveryBlocker)));
}
