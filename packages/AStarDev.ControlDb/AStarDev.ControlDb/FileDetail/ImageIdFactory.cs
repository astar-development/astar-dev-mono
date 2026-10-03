namespace AStarDev.ControlDb.FileDetail;

public static class ImageIdFactory
{
    extension(ImageId)
    {
        public static ImageId Create() => new(Guid.CreateVersion7());
        public static ImageId Empty => new(Guid.Empty);
    }
}
