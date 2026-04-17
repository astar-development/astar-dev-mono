using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Graph;

public sealed class GivenADeltaItemExclusionFilter
{
    private static DeltaItem File(string id, string relativePath, string? parentId = null)
        => new(id, "drive-1", Path.GetFileName(relativePath), parentId, IsFolder: false, IsDeleted: false, 0L, null, null, relativePath);

    private static DeltaItem Folder(string id, string relativePath, string? parentId = null)
        => new(id, "drive-1", Path.GetFileName(relativePath), parentId, IsFolder: true, IsDeleted: false, 0L, null, null, relativePath);

    [Fact]
    public void when_no_exclusions_then_all_items_are_returned()
    {
        List<DeltaItem> items = [File("f1", "Documents/readme.txt"), File("f2", "Documents/Photos/image.jpg")];

        var result = DeltaItemExclusionFilter.Filter(items, new HashSet<string>());

        result.ShouldBe(items);
    }

    [Fact]
    public void when_excluded_folder_id_not_in_items_then_all_items_are_returned()
    {
        List<DeltaItem> items = [File("f1", "Documents/readme.txt"), Folder("folder-photos", "Documents/Photos")];
        var exclusions = new HashSet<string> { "unknown-id" };

        var result = DeltaItemExclusionFilter.Filter(items, exclusions);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void when_excluded_folder_is_in_items_then_folder_and_its_files_are_removed()
    {
        List<DeltaItem> items =
        [
            File("f1",           "Documents/readme.txt"),
            Folder("photos-id",  "Documents/Photos"),
            File("f2",           "Documents/Photos/image.jpg"),
            File("f3",           "Documents/Photos/holiday.jpg")
        ];
        var exclusions = new HashSet<string> { "photos-id" };

        var result = DeltaItemExclusionFilter.Filter(items, exclusions);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("f1");
    }

    [Fact]
    public void when_excluded_folder_has_nested_sub_folders_then_all_descendants_are_removed()
    {
        List<DeltaItem> items =
        [
            File("f1",            "Documents/readme.txt"),
            Folder("photos-id",   "Documents/Photos"),
            Folder("summer-id",   "Documents/Photos/Summer"),
            File("f2",            "Documents/Photos/Summer/beach.jpg"),
            File("f3",            "Documents/Photos/winter.jpg")
        ];
        var exclusions = new HashSet<string> { "photos-id" };

        var result = DeltaItemExclusionFilter.Filter(items, exclusions);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("f1");
    }

    [Fact]
    public void when_nested_sub_folder_is_excluded_then_only_that_sub_folder_and_its_children_are_removed()
    {
        List<DeltaItem> items =
        [
            File("f1",           "Documents/readme.txt"),
            Folder("photos-id",  "Documents/Photos"),
            File("f2",           "Documents/Photos/image.jpg"),
            Folder("summer-id",  "Documents/Photos/Summer"),
            File("f3",           "Documents/Photos/Summer/beach.jpg")
        ];
        var exclusions = new HashSet<string> { "summer-id" };

        var result = DeltaItemExclusionFilter.Filter(items, exclusions);

        result.Count.ShouldBe(3);
        result.ShouldNotContain(i => i.Id == "summer-id");
        result.ShouldNotContain(i => i.Id == "f3");
    }

    [Fact]
    public void when_multiple_folders_are_excluded_then_all_their_contents_are_removed()
    {
        List<DeltaItem> items =
        [
            File("f1",         "Documents/readme.txt"),
            Folder("photos",   "Documents/Photos"),
            File("f2",         "Documents/Photos/image.jpg"),
            Folder("work",     "Documents/Work"),
            File("f3",         "Documents/Work/report.docx")
        ];
        var exclusions = new HashSet<string> { "photos", "work" };

        var result = DeltaItemExclusionFilter.Filter(items, exclusions);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("f1");
    }

    [Fact]
    public void when_path_matching_is_case_insensitive_then_mixed_case_paths_are_filtered()
    {
        List<DeltaItem> items =
        [
            Folder("photos-id", "Documents/Photos"),
            File("f1",          "documents/photos/image.jpg")
        ];
        var exclusions = new HashSet<string> { "photos-id" };

        var result = DeltaItemExclusionFilter.Filter(items, exclusions);

        result.ShouldBeEmpty();
    }
}
