using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsUnit.FileDetail;

internal static class ImageDetailEntityFactory
{
    public static ImageDetailEntity CreateImageDetailEntity()
    {
        var imageDetail = new ImageDetailEntity
        {
            Id = ImageId.Empty,
            FileId = FileId.Empty,
            Width = 1920,
            Height = 1080
        };

        return imageDetail;
    }
}