using AStar.Dev.FunctionalParadigm;
using Bunit;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Components.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Pages;

public class GivenASubjectPage : Bunit.BunitContext
{
    private readonly ICatalogueService catalogueService = Substitute.For<ICatalogueService>();

    public GivenASubjectPage()
    {
        Services.AddSingleton(catalogueService);
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<Subject>>(NullLogger<Subject>.Instance);
    }

    [Fact]
    public void when_the_subject_slug_is_known_then_the_category_heading_and_pdf_cards_are_shown()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);
        var subcategory = PdfSubcategoryFactory.Create(1, "KS1", [file]);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategory]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));

        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "maths"));

        cut.Find("h1.hero__title").TextContent.ShouldBe("Maths Resources");
        cut.FindAll("article.pdf-card").Count.ShouldBe(1);
    }

    [Fact]
    public void when_a_subcategory_has_no_files_then_it_is_omitted_from_the_catalogue()
    {
        var subcategoryWithFiles = PdfSubcategoryFactory.Create(1, "KS1", [PdfFileFactory.Create(1, "File", "pdfs/file.pdf", 1m)]);
        var emptySubcategory = PdfSubcategoryFactory.Create(2, "KS2", []);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategoryWithFiles, emptySubcategory]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));

        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "maths"));

        cut.FindAll("h2.subcategory-title").Count.ShouldBe(1);
        cut.Find("h2.subcategory-title").TextContent.ShouldBe("KS1");
    }

    [Fact]
    public void when_a_category_has_no_files_at_all_then_the_empty_state_is_shown()
    {
        var category = PdfCategoryFactory.Create(2, "English", [PdfSubcategoryFactory.Create(2, "KS2", [])]);
        catalogueService.GetCategoryBySlug("english").Returns(Option.Some(category));

        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "english"));

        cut.FindAll("div.empty-state").Count.ShouldBe(1);
    }

    [Fact]
    public void when_the_subject_slug_is_unknown_then_the_response_status_code_is_set_to_not_found()
    {
        catalogueService.GetCategoryBySlug("unknown").Returns(Option.None<PdfCategory>());
        var httpContext = new DefaultHttpContext();

        Render<Subject>(parameters => parameters
            .Add(p => p.subject, "unknown")
            .AddCascadingValue(httpContext));

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
