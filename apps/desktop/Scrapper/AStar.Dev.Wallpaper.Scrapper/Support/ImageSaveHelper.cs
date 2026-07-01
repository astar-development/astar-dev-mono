using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

internal sealed class ImageSaveHelper
{
    public static async Task SaveImage(byte[] image, string imageNameWithPath)
    {
        imageNameWithPath = imageNameWithPath.CleanPath();

        if(imageNameWithPath.LastIndexOf(':') > 2) imageNameWithPath = imageNameWithPath[..2] + imageNameWithPath[2..].Replace(":", "_");

        if(image.Length > 0) await File.WriteAllBytesAsync(imageNameWithPath, image);
    }
}
