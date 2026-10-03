namespace AStarDev.ControlDb.TagDetail;

public static class TagIdFactory
{
    extension(TagId)
    {
        public static TagId Create() => new(Guid.CreateVersion7());
        public static TagId Empty => new(Guid.Empty);
    }
}
