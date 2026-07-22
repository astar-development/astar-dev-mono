using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperDirectoryResolver
{
    private static readonly DirectoryLayout layout = new("/root", "/regular", "/famous");
    private static readonly ScrapeCategory category = new("Landscapes", "https://example.com/landscapes", false, false);

    private readonly MockFileSystem fileSystem = new();

    [Fact]
    public void when_no_tags_are_famous_then_the_regular_base_directory_is_used()
    {
        string directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData("Nature", "outdoors")], category, [new FileClassificationCategoryEntity { Name = "Nature", IncludeInSearch = true, Level = 1 }], fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/Nature");
    }

    [Fact]
    public void when_any_tag_is_famous_then_the_famous_base_directory_is_used_instead()
    {
        List<TagData> tags = [new("Emma Stone", "people > actress")];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [new FileClassificationCategoryEntity { Name = "Emma Stone", IncludeInSearch = true, Level = 1 }], fileSystem);

        directory.ShouldStartWith("/root/famous/L/Landscapes");
    }

    [Fact]
    public void when_there_are_no_tags_then_only_the_root_base_and_category_directories_are_returned()
    {
        string directory = WallpaperDirectoryResolver.Resolve(layout, [], category, [], fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes");
    }

    [Fact]
    public void when_a_tag_has_no_category_then_it_contributes_no_path_segment()
    {
        string directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData("Untagged", null)], category, [], fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes");
    }

    [Fact]
    public void when_a_tag_matches_the_category_name_then_it_contributes_no_path_segment()
    {
        string directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData(category.Name, "outdoors")], category, [], fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes");
    }

    [Fact]
    public void when_a_tag_matches_the_category_name_with_different_casing_then_it_contributes_no_path_segment()
    {
        string directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData(category.Name.ToUpperInvariant(), "outdoors")], category, [], fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes");
    }

    [Fact]
    public void when_tags_are_a_mix_of_famous_internet_and_ordinary_then_famous_and_internet_segments_come_first_in_list_order()
    {
        TagData ordinary = new("Nature", "outdoors");
        TagData famous = new("Emma Stone", "people > actress");
        TagData internetModel = new("SomeModel", "people > model");
        List<TagData> tags = [ordinary, famous, internetModel];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [new FileClassificationCategoryEntity { Name = famous.Tag, IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = internetModel.Tag, IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = ordinary.Tag, IncludeInSearch = true, Level = 1 }], fileSystem);

        directory.ShouldBe("/root/famous/L/Landscapes/Emma Stone/SomeModel/Nature");
    }

    [Fact]
    public void when_multiple_ordinary_tags_have_categories_then_they_are_ordered_alphabetically_regardless_of_list_order()
    {
        List<TagData> tags = [new("Zebra", "animals"), new("Apple", "food"), new("Mango", "food")];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [ new FileClassificationCategoryEntity { Name = "Apple", IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = "Mango", IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = "Zebra", IncludeInSearch = true, Level = 1 }], fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/Apple/Mango/Zebra");
    }

    [Fact]
    public void when_famous_and_ordinary_tags_are_mixed_then_famous_tags_keep_list_order_ahead_of_the_alphabetically_ordered_ordinary_tags()
    {
        TagData zebra = new("Zebra", "animals");
        TagData apple = new("Apple", "food");
        TagData famous = new("Emma Stone", "people > actress");
        List<TagData> tags = [zebra, famous, apple];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [

            new FileClassificationCategoryEntity { Name = famous.Tag, IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = apple.Tag, IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = zebra.Tag, IncludeInSearch = true, Level = 1 }], fileSystem);

        directory.ShouldBe("/root/famous/L/Landscapes/Emma Stone/Apple/Zebra");
    }

    [Fact]
    public void when_a_tag_contains_a_colon_then_the_colon_is_removed_from_the_path()
    {
        string directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData("Marvel: Avengers", "movies")], category, [ new FileClassificationCategoryEntity { Name = "Marvel: Avengers", IncludeInSearch = true, Level = 1 }], fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/Marvel Avengers");
    }

    [Fact]
    public void when_the_root_directory_starts_with_a_drive_letter_then_the_drive_colon_is_preserved()
    {
        var layout = new DirectoryLayout("C:/w", "/regular", "/famous");

        string directory = WallpaperDirectoryResolver.Resolve(layout, [], category, [], fileSystem);

        directory.ShouldBe("C:/w/regular/L/Landscapes");
    }

    [Fact]
    public void when_a_tag_contains_an_invalid_path_character_then_the_character_is_removed()
    {
        string directory = WallpaperDirectoryResolver.Resolve(layout, [new TagData("Bad\0Tag", "outdoors")], category, [ new FileClassificationCategoryEntity { Name = "Bad\0Tag", IncludeInSearch = true, Level = 1 }], fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/Bad Tag");
    }

    [Fact]
    public void when_the_directory_layout_is_empty_and_the_category_name_is_short_then_the_short_path_is_still_resolved()
    {
        var layout = new DirectoryLayout(string.Empty, string.Empty, string.Empty);
        var category = new ScrapeCategory("A", "https://example.com/a", false, false);

        string directory = WallpaperDirectoryResolver.Resolve(layout, [], category, [], fileSystem);

        directory.ShouldBe("A/A");
    }

    [Fact]
    public void when_more_than_twelve_eligible_tags_are_present_then_only_the_first_twelve_contribute_path_segments()
    {
        List<TagData> tags =
        [
            new("Emma Stone", "people > actress"),
            new("SomeModel", "people > model"),
            new("Apple", "food"),
            new("Banana", "food"),
            new("Cherry", "food"),
            new("Damson", "food"),
            new("Elderberry", "food"),
            new("Fig", "food"),
            new("Grape", "food"),
            new("Honeydew", "food"),
            new("Iced Tea", "food"),
            new("Jackfruit", "food"),
            new("Kiwi", "food")
        ];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [ new FileClassificationCategoryEntity { Name = "Emma Stone", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "SomeModel", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Apple", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Banana", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Cherry", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Damson", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Elderberry", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Fig", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Grape", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Honeydew", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Iced Tea", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Jackfruit", IncludeInSearch = true, Level = 1 },
            new FileClassificationCategoryEntity { Name = "Kiwi", IncludeInSearch = true, Level = 1 }
        ], fileSystem);

        directory.ShouldBe("/root/famous/L/Landscapes/Emma Stone/SomeModel/Apple/Banana/Cherry/Damson/Elderberry/Fig/Grape/Honeydew/Iced Tea/Jackfruit");
    }

    [Fact]
    public void when_tags_include_duplicates_then_only_one_path_segment_is_created()
    {
        TagData ordinary = new("Nature", "outdoors");
        TagData famous = new("Emma Stone", "people > actress");
        TagData internetModel = new("SomeModel", "people > model");
        TagData ordinary2 = new("Nature", "outdoors");
        TagData famous2 = new("Emma Stone", "people > actress");
        TagData internetModel2 = new("SomeModel", "people > model");
        List<TagData> tags = [ordinary, internetModel2, internetModel, ordinary2, ordinary2, ordinary2, famous2, famous];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, [new FileClassificationCategoryEntity { Name = famous.Tag, IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = internetModel.Tag, IncludeInSearch = true, Level = 1 }, new FileClassificationCategoryEntity { Name = ordinary.Tag, IncludeInSearch = true, Level = 1 }], fileSystem);

        directory.ShouldBe("/root/famous/L/Landscapes/Emma Stone/SomeModel/Nature");
    }

    [Fact]
    public void when_an_existing_directory_shares_all_segments_in_a_different_order_then_the_existing_order_is_reused()
    {
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/folderb/folderc/folderd");
        List<TagData> tags = [new("folderd", "cat"), new("folderc", "cat"), new("folderb", "cat")];
        List<FileClassificationCategoryEntity> classifications =
        [
            new() { Name = "folderd", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderc", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderb", IncludeInSearch = true, Level = 1 }
        ];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, classifications, fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/folderb/folderc/folderd");
    }

    [Fact]
    public void when_a_newly_computed_segment_is_not_present_on_disk_then_it_is_appended_to_the_end_of_the_reused_path()
    {
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/folderb/folderc/folderd");
        List<TagData> tags = [new("folderd", "cat"), new("folderc", "cat"), new("folderf", "cat"), new("folderb", "cat")];
        List<FileClassificationCategoryEntity> classifications =
        [
            new() { Name = "folderd", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderc", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderf", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderb", IncludeInSearch = true, Level = 1 }
        ];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, classifications, fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/folderb/folderc/folderd/folderf");
    }

    [Fact]
    public void when_multiple_new_segments_are_appended_then_they_are_ordered_by_level_ascending_then_priority_ascending()
    {
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/folderb");
        List<TagData> tags = [new("folderb", "cat"), new("folderNew1", "cat"), new("folderNew2", "cat"), new("folderNew3", "cat")];
        List<FileClassificationCategoryEntity> classifications =
        [
            new() { Name = "folderb", IncludeInSearch = true, Level = 1, Priority = 1 },
            new() { Name = "folderNew1", IncludeInSearch = true, Level = 2, Priority = 5 },
            new() { Name = "folderNew2", IncludeInSearch = true, Level = 1, Priority = 9 },
            new() { Name = "folderNew3", IncludeInSearch = true, Level = 1, Priority = 2 }
        ];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, classifications, fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/folderb/folderNew3/folderNew2/folderNew1");
    }

    [Fact]
    public void when_a_disk_segment_is_no_longer_in_the_computed_set_then_it_is_dropped()
    {
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/folderb/folderOld/folderc");
        List<TagData> tags = [new("folderb", "cat"), new("folderc", "cat")];
        List<FileClassificationCategoryEntity> classifications =
        [
            new() { Name = "folderb", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderc", IncludeInSearch = true, Level = 1 }
        ];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, classifications, fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/folderb/folderc");
    }

    [Fact]
    public void when_segment_names_differ_only_by_case_then_they_are_still_treated_as_the_same_segment()
    {
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/FolderB");
        List<TagData> tags = [new("folderb", "cat")];
        List<FileClassificationCategoryEntity> classifications = [new() { Name = "folderb", IncludeInSearch = true, Level = 1 }];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, classifications, fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/FolderB");
    }

    [Fact]
    public void when_an_existing_directory_contains_a_segment_not_in_the_computed_set_then_it_is_excluded_in_favour_of_a_fully_compatible_directory()
    {
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/folderb/folderx/foldery");
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/folderb/folderc");
        List<TagData> tags = [new("folderb", "cat"), new("folderc", "cat"), new("foldery", "cat")];
        List<FileClassificationCategoryEntity> classifications =
        [
            new() { Name = "folderb", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderc", IncludeInSearch = true, Level = 1 },
            new() { Name = "foldery", IncludeInSearch = true, Level = 1 }
        ];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, classifications, fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/folderb/folderc/foldery");
    }

    [Fact]
    public void when_multiple_existing_directories_overlap_the_computed_segments_then_the_directory_with_the_greatest_overlap_is_chosen()
    {
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/folderb/folderx");
        fileSystem.Directory.CreateDirectory("/root/regular/L/Landscapes/folderb/folderc/folderd");
        List<TagData> tags = [new("folderb", "cat"), new("folderc", "cat"), new("folderd", "cat"), new("folderf", "cat")];
        List<FileClassificationCategoryEntity> classifications =
        [
            new() { Name = "folderb", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderc", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderd", IncludeInSearch = true, Level = 1 },
            new() { Name = "folderf", IncludeInSearch = true, Level = 1 }
        ];

        string directory = WallpaperDirectoryResolver.Resolve(layout, tags, category, classifications, fileSystem);

        directory.ShouldBe("/root/regular/L/Landscapes/folderb/folderc/folderd/folderf");
    }
}
