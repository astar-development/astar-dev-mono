using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using AStar.Dev.Wallpaper.Scraper.Tests.Unit.TestData;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenAnImagePersistence
{
    private const string ImageNameWithPath = "/save/dir/12345.jpg";
    private const string Filename = "12345.jpg";
    private static readonly DirectoryName SaveDirectory = new("/save/dir");
    private static readonly byte[] ImageBytes = [1, 2, 3,];

    private static IImageSaver BuildSucceedingImageSaver()
    {
        var imageSaver = Substitute.For<IImageSaver>();
        imageSaver.SaveAsync(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value)));

        return imageSaver;
    }

    private static IImageDimensionReader BuildFailingDimensionReader()
    {
        var imageDimensionReader = Substitute.For<IImageDimensionReader>();
        imageDimensionReader.Read(Arg.Any<byte[]>(), Arg.Any<string>())
                            .Returns(Result.Failure<ImageDimensions, ScrapeError>(ScrapeErrorFactory.CreateImageDimensionReadFailed(ImageNameWithPath, "invalid image")));

        return imageDimensionReader;
    }

    [Fact]
    public async Task when_the_save_succeeds_then_the_file_detail_is_persisted_and_returned()
    {
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository();
        var sut = new ImagePersistence(BuildSucceedingImageSaver(), BuildFailingDimensionReader(), fileDetailRepository, new(), new LoggerConfiguration().CreateLogger());

        var result = await sut.SaveAndPersistAsync(ImageBytes, ImageNameWithPath, Filename, SaveDirectory, TestContext.Current.CancellationToken);

        var fileDetail = result.ShouldBeOfType<Ok<FileDetailEntity, ScrapeError>>().Value;
        fileDetail.FileName.Value.ShouldBe(Filename);
        fileDetail.FileSize.ShouldBe(ImageBytes.Length);
        await fileDetailRepository.Received(1).AddAsync(Arg.Any<FileDetailEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_save_succeeds_then_the_saved_image_is_broadcast()
    {
        var imageBroadcaster = new ImageBroadcaster();
        string? broadcastPath = null;
        imageBroadcaster.ImageSaved += path => broadcastPath = path;
        var sut = new ImagePersistence(BuildSucceedingImageSaver(), BuildFailingDimensionReader(), RepositoryTestDoubles.BuildFileDetailRepository(), imageBroadcaster, new LoggerConfiguration().CreateLogger());

        await sut.SaveAndPersistAsync(ImageBytes, ImageNameWithPath, Filename, SaveDirectory, TestContext.Current.CancellationToken);

        broadcastPath.ShouldBe(ImageNameWithPath);
    }

    [Fact]
    public async Task when_the_dimension_reader_succeeds_then_the_persisted_file_detail_carries_the_dimensions()
    {
        var imageDimensionReader = Substitute.For<IImageDimensionReader>();
        imageDimensionReader.Read(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Result.Success<ImageDimensions, ScrapeError>(new ImageDimensions(40, 20)));
        var sut = new ImagePersistence(BuildSucceedingImageSaver(), imageDimensionReader, RepositoryTestDoubles.BuildFileDetailRepository(), new(), new LoggerConfiguration().CreateLogger());

        var result = await sut.SaveAndPersistAsync(ImageBytes, ImageNameWithPath, Filename, SaveDirectory, TestContext.Current.CancellationToken);

        var fileDetail = result.ShouldBeOfType<Ok<FileDetailEntity, ScrapeError>>().Value;
        fileDetail.ImageDetail.ShouldNotBeNull();
        fileDetail.ImageDetail.Width.ShouldBe(40);
        fileDetail.ImageDetail.Height.ShouldBe(20);
    }

    [Fact]
    public async Task when_the_save_fails_then_the_error_is_returned_and_nothing_is_persisted()
    {
        var imageSaver = Substitute.For<IImageSaver>();
        imageSaver.SaveAsync(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Task.FromResult(Result.Failure<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(ScrapeErrorFactory.CreateImageSaveFailed(ImageNameWithPath, "disk full"))));
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository();
        var sut = new ImagePersistence(imageSaver, BuildFailingDimensionReader(), fileDetailRepository, new(), new LoggerConfiguration().CreateLogger());

        var result = await sut.SaveAndPersistAsync(ImageBytes, ImageNameWithPath, Filename, SaveDirectory, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<FileDetailEntity, ScrapeError>>().Error.ShouldBeOfType<ImageSaveFailed>();
        await fileDetailRepository.DidNotReceive().AddAsync(Arg.Any<FileDetailEntity>(), Arg.Any<CancellationToken>());
    }
}
