using AStarDev.FunctionalParadigm;
using CsCheck;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenHeldLocalDeletionsForAnyPaths
{
    private static readonly SyncPairId Pair = SyncPairIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static readonly Gen<RelativeSyncPath> AnyPath = Gen.Char["aAbB"].Array[1, 3].Select(letters => new string(letters)).Array[1, 3].Select(segments => RelativeSyncPathFactory.Create(string.Join('/', segments)).Match(path => path, _ => throw new InvalidOperationException("The generated path was rejected.")));

    private static readonly Gen<RelativeSyncPath[]> AnyPaths = AnyPath.Array[0, 12];

    private static HeldLocalDeletions Held(IEnumerable<RelativeSyncPath> paths) => HeldLocalDeletionsFactory.Create(Pair, paths, 100);

    [Fact]
    public void when_any_paths_are_held_then_no_two_held_paths_differ_only_by_case()
    {
        AnyPaths.Sample(paths => Held(paths).Paths.Select(path => path.Value.ToUpperInvariant()).ToList().Count == Held(paths).Paths.Select(path => path.Value.ToUpperInvariant()).Distinct().Count());
    }

    [Fact]
    public void when_any_paths_are_held_then_every_path_is_still_held_in_some_casing()
    {
        AnyPaths.Sample(paths => paths.All(path => Held(paths).Paths.Any(held => string.Equals(held.Value, path.Value, StringComparison.OrdinalIgnoreCase))));
    }

    [Fact]
    public void when_the_held_paths_are_held_again_then_nothing_changes()
    {
        AnyPaths.Sample(paths => Held(Held(paths).Paths).Paths.SequenceEqual(Held(paths).Paths));
    }

    [Fact]
    public void when_any_paths_are_held_then_there_are_never_more_than_were_given()
    {
        AnyPaths.Sample(paths => Held(paths).Paths.Count <= paths.Length);
    }
}
