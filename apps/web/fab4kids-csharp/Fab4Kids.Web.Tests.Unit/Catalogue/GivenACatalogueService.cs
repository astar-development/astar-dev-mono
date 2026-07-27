using AStar.Dev.FunctionalParadigm;
using Fab4Kids.Web.Catalogue;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Catalogue;

public class GivenACatalogueService
{
    private readonly ICatalogueService sut;

    public GivenACatalogueService()
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(Path.Combine(AppContext.BaseDirectory, "Catalogue", "Fixtures"));
        sut = new CatalogueService(hostEnvironment);
    }

    [Fact]
    public void when_getting_all_categories_then_both_fixture_categories_are_returned()
    {
        var categories = sut.GetAllCategories();

        categories.Select(category => category.Name).ShouldBe(["Maths", "English"]);
    }

    [Fact]
    public void when_looking_up_a_known_category_slug_then_it_is_returned()
    {
        sut.GetCategoryBySlug("maths").TryGetValue(out var category).ShouldBeTrue();

        category.Name.ShouldBe("Maths");
    }

    [Fact]
    public void when_looking_up_an_unknown_category_slug_then_none_is_returned()
    {
        sut.GetCategoryBySlug("geography").TryGetValue(out _).ShouldBeFalse();
    }

    [Fact]
    public void when_looking_up_a_known_subcategory_slug_then_it_is_returned()
    {
        sut.GetSubcategoryBySlug("maths", "ks1").TryGetValue(out var subcategory).ShouldBeTrue();

        subcategory.Name.ShouldBe("KS1");
        subcategory.Files.ShouldHaveSingleItem();
    }

    [Fact]
    public void when_looking_up_a_subcategory_slug_under_an_unknown_category_then_none_is_returned()
    {
        sut.GetSubcategoryBySlug("geography", "ks1").TryGetValue(out _).ShouldBeFalse();
    }

    [Fact]
    public void when_looking_up_an_unknown_subcategory_slug_then_none_is_returned()
    {
        sut.GetSubcategoryBySlug("maths", "ks9").TryGetValue(out _).ShouldBeFalse();
    }

    [Fact]
    public void when_searching_a_blank_query_then_no_results_are_returned()
    {
        sut.Search(string.Empty).ShouldBeEmpty();
        sut.Search("   ").ShouldBeEmpty();
    }

    [Fact]
    public void when_searching_a_matching_file_name_then_the_matching_result_is_returned()
    {
        var results = sut.Search("times tables");

        results.ShouldHaveSingleItem();
        results[0].File.Name.ShouldBe("Times Tables Pack");
        results[0].CategorySlug.ShouldBe("maths");
        results[0].SubcategorySlug.ShouldBe("ks1");
    }

    [Fact]
    public void when_searching_a_matching_category_name_then_files_under_that_category_are_returned()
    {
        var results = sut.Search("maths");

        results.ShouldHaveSingleItem();
        results[0].CategoryName.ShouldBe("Maths");
    }

    [Fact]
    public void when_searching_a_non_matching_query_then_no_results_are_returned()
    {
        sut.Search("geography").ShouldBeEmpty();
    }
}
