namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

internal class ClassificationCategoryNodeComparer : IEqualityComparer<ClassificationCategoryNode>
{
    public bool Equals(ClassificationCategoryNode? x, ClassificationCategoryNode? y) => x?.Id == y?.Id;

    public int GetHashCode(ClassificationCategoryNode obj) => obj.Id.GetHashCode();
}
