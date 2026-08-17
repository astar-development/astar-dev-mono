using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Checkout;
using Fab4Kids.Web.Components.Pages;
using Fab4Kids.Web.Newsletter;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Components.Pages;

public class GivenAHomePage : Bunit.BunitContext
{
    private readonly ICatalogueService catalogueService = Substitute.For<ICatalogueService>();

    public GivenAHomePage()
    {
        Services.AddSingleton(Substitute.For<INewsletterSubscriptionService>());
        Services.AddSingleton(catalogueService);
        Services.AddSingleton(new CartState(Substitute.For<ILocalStorageService>()));
        Services.AddSingleton(Substitute.For<ICheckoutSessionService>());
        catalogueService.GetAllCategories().Returns([]);
    }

    [Fact]
    public void when_rendered_then_the_hero_headline_is_shown()
    {
        var cut = Render<Home>();

        cut.Find("h1.hero__headline").TextContent.ShouldBe("Learning that feels like play");
    }

    [Fact]
    public void when_rendered_then_all_five_subject_cards_are_shown()
    {
        var cut = Render<Home>();

        cut.FindAll("a.subject-card").Count.ShouldBe(5);
    }

    [Fact]
    public void when_the_catalogue_has_resources_then_the_featured_grid_shows_one_per_category()
    {
        var mathsFile = PdfFileFactory.Create(1, "Fractions Fun Pack", "pdfs/fractions.pdf", 3.50m);
        var englishFile = PdfFileFactory.Create(2, "Persuasive Writing Kit", "pdfs/persuasive.pdf", 2.75m);
        catalogueService.GetAllCategories().Returns(
        [
            PdfCategoryFactory.Create(1, "Maths", [PdfSubcategoryFactory.Create(1, "KS2", [mathsFile])]),
            PdfCategoryFactory.Create(2, "English", [PdfSubcategoryFactory.Create(2, "KS2", [englishFile])]),
        ]);

        var cut = Render<Home>();

        cut.FindAll("article.pdf-card").Count.ShouldBe(2);
    }

    [Fact]
    public void when_a_category_has_no_files_then_it_is_omitted_from_the_featured_grid()
    {
        catalogueService.GetAllCategories().Returns(
        [
            PdfCategoryFactory.Create(1, "Maths", [PdfSubcategoryFactory.Create(1, "KS2", [])]),
        ]);

        var cut = Render<Home>();

        cut.FindAll("article.pdf-card").Count.ShouldBe(0);
    }
}
