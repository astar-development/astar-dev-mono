using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenALevel1Deriver
{
    [Fact]
    public void when_folder_type_map_accessed_then_people_maps_to_person() =>
        Level1Deriver.FolderTypeMap["people"].ShouldBe("Person");

    [Fact]
    public void when_folder_type_map_accessed_then_places_maps_to_place() =>
        Level1Deriver.FolderTypeMap["places"].ShouldBe("Place");

    [Fact]
    public void when_folder_type_map_accessed_then_events_maps_to_event() =>
        Level1Deriver.FolderTypeMap["events"].ShouldBe("Event");

    [Fact]
    public void when_folder_type_map_accessed_then_photos_maps_to_object() =>
        Level1Deriver.FolderTypeMap["photos"].ShouldBe("Object");

    [Fact]
    public void when_folder_type_map_accessed_then_portraits_maps_to_person() =>
        Level1Deriver.FolderTypeMap["portraits"].ShouldBe("Person");

    [Fact]
    public void when_folder_type_map_accessed_then_landscapes_maps_to_place() =>
        Level1Deriver.FolderTypeMap["landscapes"].ShouldBe("Place");

    [Fact]
    public void when_derive_called_with_people_folder_segment_then_person_is_returned()
    {
        IReadOnlyList<string> folderSegments = ["People"];
        IReadOnlyList<string> filenameTokens = ["birthday", "party"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Person");
    }

    [Fact]
    public void when_derive_called_with_places_folder_segment_then_place_is_returned()
    {
        IReadOnlyList<string> folderSegments = ["Places"];
        IReadOnlyList<string> filenameTokens = ["mountain", "view"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Place");
    }

    [Fact]
    public void when_derive_called_with_events_folder_segment_then_event_is_returned()
    {
        IReadOnlyList<string> folderSegments = ["Events"];
        IReadOnlyList<string> filenameTokens = ["summer", "concert"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Event");
    }

    [Fact]
    public void when_derive_called_with_photos_folder_segment_then_object_is_returned()
    {
        IReadOnlyList<string> folderSegments = ["Photos"];
        IReadOnlyList<string> filenameTokens = ["misc", "shot"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Object");
    }

    [Fact]
    public void when_derive_called_with_folder_match_and_person_name_tokens_then_folder_wins()
    {
        IReadOnlyList<string> folderSegments = ["Events"];
        IReadOnlyList<string> filenameTokens = ["john", "smith"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Event");
    }

    [Fact]
    public void when_derive_called_with_no_folder_match_and_person_name_in_tokens_then_person_is_returned()
    {
        IReadOnlyList<string> folderSegments = ["Archive"];
        IReadOnlyList<string> filenameTokens = ["john", "smith"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Person");
    }

    [Fact]
    public void when_derive_called_with_no_folder_match_and_colour_in_tokens_then_color_is_returned()
    {
        IReadOnlyList<string> folderSegments = ["Archive"];
        IReadOnlyList<string> filenameTokens = ["red", "car", "road"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Color");
    }

    [Fact]
    public void when_derive_called_with_no_folder_match_and_no_person_and_no_colour_then_object_is_returned()
    {
        IReadOnlyList<string> folderSegments = ["Archive"];
        IReadOnlyList<string> filenameTokens = ["misc", "shot", "img"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Object");
    }

    [Fact]
    public void when_derive_called_with_empty_folder_segments_and_no_person_and_no_colour_then_object_is_returned()
    {
        IReadOnlyList<string> folderSegments = [];
        IReadOnlyList<string> filenameTokens = ["misc", "img"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Object");
    }

    [Fact]
    public void when_derive_called_with_people_folder_and_colour_filename_then_folder_wins_over_colour()
    {
        IReadOnlyList<string> folderSegments = ["People"];
        IReadOnlyList<string> filenameTokens = ["red", "car"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Person");
    }

    [Fact]
    public void when_derive_called_with_folder_segment_in_mixed_case_then_match_is_case_insensitive()
    {
        IReadOnlyList<string> folderSegments = ["PLACES"];
        IReadOnlyList<string> filenameTokens = ["some", "image"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Place");
    }

    [Fact]
    public void when_derive_called_with_nested_folder_segments_and_recognised_segment_then_mapped_value_is_returned()
    {
        IReadOnlyList<string> folderSegments = ["2024", "Events", "Summer"];
        IReadOnlyList<string> filenameTokens = ["concert", "night"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Event");
    }

    [Fact]
    public void when_derive_called_with_empty_folder_segments_and_person_name_tokens_then_person_is_returned()
    {
        IReadOnlyList<string> folderSegments = [];
        IReadOnlyList<string> filenameTokens = ["jane", "doe"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Person");
    }

    [Fact]
    public void when_folder_type_map_return_type_is_readonly_dictionary()
    {
        var map = Level1Deriver.FolderTypeMap;

        _ = map.ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
    }

    [Fact]
    public void when_derive_called_with_both_collections_empty_then_object_is_returned()
    {
        IReadOnlyList<string> folderSegments = [];
        IReadOnlyList<string> filenameTokens = [];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Object");
    }

    [Fact]
    public void when_derive_called_with_no_folder_match_and_person_name_and_colour_in_tokens_then_person_takes_priority()
    {
        IReadOnlyList<string> folderSegments = ["Archive"];
        IReadOnlyList<string> filenameTokens = ["john", "smith", "red"];

        string result = Level1Deriver.Derive(folderSegments, filenameTokens);

        result.ShouldBe("Person");
    }
}
