using System.Globalization;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Classifications;

public sealed class GivenAClassificationText
{
    private readonly ILocalizationService loc = Substitute.For<ILocalizationService>();
    private readonly ClassificationText sut;

    public GivenAClassificationText()
    {
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));
        sut = ClassificationTextFactory.Create(loc);
    }

    public static TheoryData<string, string> TextProperties => new()
    {
        { "TitleText", "Classifications.Title" },
        { "DescriptionText", "Classifications.Description" },
        { "FilterPlaceholderText", "Classifications.FilterPlaceholder" },
        { "ExportText", "Classifications.Export" },
        { "ImportText", "Classifications.Import" },
        { "EmptyText", "Classifications.Empty" },
        { "FamousText", "Classifications.Famous" },
        { "InternetText", "Classifications.Internet" },
        { "EditText", "Classifications.Edit" },
        { "DeleteText", "Classifications.Delete" },
        { "NewSubCategoryPlaceholderText", "Classifications.NewSubCategoryPlaceholder" },
        { "AddSubCategoryText", "Classifications.AddSubCategory" },
        { "AddCategoryText", "Classifications.AddCategory" },
        { "CategoryNamePlaceholderText", "Classifications.CategoryNamePlaceholder" },
        { "AddText", "Classifications.Add" },
        { "ExportDialogTitleText", "Classifications.ExportDialogTitle" },
        { "ImportDialogTitleText", "Classifications.ImportDialogTitle" },
        { "ImportConfirmMessageText", "Classifications.ImportConfirmMessage" },
        { "EditDialogTitleText", "Classifications.EditDialog.Title" },
        { "EditDialogNameText", "Classifications.EditDialog.Name" },
        { "IncludeInSearchText", "Classifications.EditDialog.IncludeInSearch" },
        { "ParentText", "Classifications.EditDialog.Parent" },
        { "CancelText", "Common.Cancel" },
        { "SaveText", "Common.Save" },
        { "ReclassifyText", "Classifications.Reclassify" },
        { "ReclassifyDialogTitleText", "Classifications.ReclassifyDialogTitle" },
        { "ReclassifyConfirmMessageText", "Classifications.ReclassifyConfirmMessage" },
        { "ReclassifyCancelledText", "Classifications.ReclassifyCancelled" },
    };

    [Theory]
    [MemberData(nameof(TextProperties))]
    public void when_text_property_is_read_then_it_comes_from_the_localisation_service(string propertyName, string key) => ((string?)typeof(ClassificationText).GetProperty(propertyName)?.GetValue(sut)).ShouldBe(key);

    [Theory]
    [MemberData(nameof(TextProperties))]
    public void when_culture_changes_then_text_property_change_is_raised(string propertyName, string key)
    {
        var raised = new List<string?>();
        sut.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(loc, CultureInfo.GetCultureInfo("en-US"));

        CultureChangeProbe.Refreshes(raised, propertyName).ShouldBeTrue();
        key.ShouldNotBeEmpty();
    }

    [Fact]
    public void when_reclassify_progress_is_read_then_it_uses_the_progress_key_with_both_counts()
    {
        sut.ReclassifyProgress(2, 7);

        loc.Received(1).GetLocal("Classifications.ReclassifyProgress", Arg.Is<object[]>(args => args.SequenceEqual(new object[] { 2, 7 })));
    }

    [Fact]
    public void when_reclassify_complete_is_read_then_it_uses_the_complete_key_with_both_counts()
    {
        sut.ReclassifyComplete(3, 5);

        loc.Received(1).GetLocal("Classifications.ReclassifyComplete", Arg.Is<object[]>(args => args.SequenceEqual(new object[] { 3, 5 })));
    }

    [Fact]
    public void when_reclassify_failed_is_read_then_it_uses_the_failed_key_with_the_error()
    {
        sut.ReclassifyFailed("boom");

        loc.Received(1).GetLocal("Classifications.ReclassifyFailed", Arg.Is<object[]>(args => args.SequenceEqual(new object[] { "boom" })));
    }
}
