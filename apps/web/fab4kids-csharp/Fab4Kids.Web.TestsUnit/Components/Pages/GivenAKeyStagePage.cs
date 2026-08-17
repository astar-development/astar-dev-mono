using AStar.Dev.FunctionalParadigm;
using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Checkout;
using Fab4Kids.Web.Components.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Components.Pages;

public class GivenAKeyStagePage : Bunit.BunitContext
{
    private readonly ICatalogueService catalogueService = Substitute.For<ICatalogueService>();

    public GivenAKeyStagePage()
    {
        Services.AddSingleton(catalogueService);
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<KeyStage>>(NullLogger<KeyStage>.Instance);
        Services.AddSingleton(new CartState(Substitute.For<ILocalStorageService>()));
        Services.AddSingleton(Substitute.For<ICheckoutSessionService>());
    }

    [Fact]
    public void when_the_subject_and_key_stage_slugs_are_known_then_the_files_are_shown()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);
        var subcategory = PdfSubcategoryFactory.Create(1, "KS1", [file]);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategory]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));
        catalogueService.GetSubcategoryBySlug("maths", "ks1").Returns(Option.Some(subcategory));

        var cut = Render<KeyStage>(parameters => parameters
            .Add(p => p.subject, "maths")
            .Add(p => p.ks, "ks1"));

        cut.Find("h1.hero__title").TextContent.ShouldBe("Maths — KS1 Resources");
        cut.FindAll("article.pdf-card").Count.ShouldBe(1);
    }

    [Fact]
    public void when_the_key_stage_has_no_files_then_the_empty_state_is_shown()
    {
        var subcategory = PdfSubcategoryFactory.Create(1, "KS1", []);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategory]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));
        catalogueService.GetSubcategoryBySlug("maths", "ks1").Returns(Option.Some(subcategory));

        var cut = Render<KeyStage>(parameters => parameters
            .Add(p => p.subject, "maths")
            .Add(p => p.ks, "ks1"));

        cut.FindAll("div.empty-state").Count.ShouldBe(1);
    }

    [Fact]
    public void when_the_key_stage_slug_is_unknown_then_the_response_status_code_is_set_to_not_found()
    {
        var category = PdfCategoryFactory.Create(1, "Maths", []);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));
        catalogueService.GetSubcategoryBySlug("maths", "ks9").Returns(Option.None<PdfSubcategory>());
        var httpContext = new DefaultHttpContext();

        Render<KeyStage>(parameters => parameters
            .Add(p => p.subject, "maths")
            .Add(p => p.ks, "ks9")
            .AddCascadingValue(httpContext));

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
