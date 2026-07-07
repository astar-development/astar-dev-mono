using NSubstitute.ExceptionExtensions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;
using System.IO.Abstractions;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheImageSaver
{
    private readonly MockFileSystem fileSystem = new();
    private readonly IImageSaver sut;

    public GivenTheImageSaver() => sut = new ImageSaver(fileSystem);

    [Fact]
    public async Task when_the_image_is_empty_then_no_file_is_written()
    {
        fileSystem.Directory.CreateDirectory("/save/dir");

        await sut.SaveAsync([], "/save/dir/image.jpg");

        fileSystem.File.Exists("/save/dir/image.jpg").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_image_is_empty_then_the_result_is_ok()
    {
        fileSystem.Directory.CreateDirectory("/save/dir");

        var result = await sut.SaveAsync([], "/save/dir/image.jpg");

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_the_image_is_not_empty_then_the_file_is_written_with_the_supplied_bytes()
    {
        fileSystem.Directory.CreateDirectory("/save/dir");
        byte[] image = [1, 2, 3,];

        await sut.SaveAsync(image, "/save/dir/image.jpg");

        fileSystem.File.ReadAllBytes("/save/dir/image.jpg").ShouldBe(image);
    }

    [Fact]
    public async Task when_the_image_is_not_empty_then_the_result_is_ok()
    {
        fileSystem.Directory.CreateDirectory("/save/dir");
        byte[] image = [1, 2, 3,];

        var result = await sut.SaveAsync(image, "/save/dir/image.jpg");

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_the_path_contains_a_colon_after_the_second_character_then_it_is_replaced_with_an_underscore()
    {
        fileSystem.Directory.CreateDirectory("/tmp");
        byte[] image = [1,];

        await sut.SaveAsync(image, "/tmp/save:name.jpg");

        fileSystem.File.Exists("/tmp/save_name.jpg").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_path_contains_a_quote_then_it_is_replaced_with_a_single_quote()
    {
        fileSystem.Directory.CreateDirectory("/save/dir");
        byte[] image = [1,];

        await sut.SaveAsync(image, "/save/dir/file\"name.jpg");

        fileSystem.File.Exists("/save/dir/file'name.jpg").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_file_system_throws_then_an_image_save_failed_error_is_returned()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                          .ThrowsAsync(new IOException("disk full"));
        var throwingSut = new ImageSaver(throwingFileSystem);

        var error = (await throwingSut.SaveAsync([1, 2,], "/save/dir/image.jpg")).ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<ImageSaveFailed>();

        error.Path.ShouldBe("/save/dir/image.jpg");
    }

    [Fact]
    public async Task when_the_file_system_throws_then_no_exception_is_thrown()
    {
        var throwingFileSystem = Substitute.For<IFileSystem>();
        throwingFileSystem.File.WriteAllBytesAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
                          .ThrowsAsync(new IOException("disk full"));
        var throwingSut = new ImageSaver(throwingFileSystem);

        var exception = await Record.ExceptionAsync(() => throwingSut.SaveAsync([1, 2,], "/save/dir/image.jpg"));

        exception.ShouldBeNull();
    }
}
