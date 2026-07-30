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

namespace Fab4Kids.Web.Tests.Unit.Components.Pages;

public class GivenASubjectPage : Bunit.BunitContext
{
    private readonly ICatalogueService catalogueService = Substitute.For<ICatalogueService>();

    public GivenASubjectPage()
    {
        Services.AddSingleton(catalogueService);
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<Subject>>(NullLogger<Subject>.Instance);
        Services.AddSingleton(new CartState(Substitute.For<ILocalStorageService>()));
        Services.AddSingleton(Substitute.For<ICheckoutSessionService>());
    }

    [Fact]
    public void when_the_subject_slug_is_known_then_the_heading_and_resource_cards_are_shown()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);
        var subcategory = PdfSubcategoryFactory.Create(1, "KS1", [file]);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategory]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));

        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "maths"));

        cut.Find("h1.subject-hero__title").TextContent.ShouldBe("Maths");
        cut.FindAll("article.pdf-card").Count.ShouldBe(1);
    }

    [Fact]
    public void when_a_category_has_no_files_at_all_then_the_empty_state_is_shown()
    {
        var category = PdfCategoryFactory.Create(2, "English", [PdfSubcategoryFactory.Create(2, "KS2", [])]);
        catalogueService.GetCategoryBySlug("english").Returns(Option.Some(category));

        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "english"));

        cut.FindAll("div.empty-state").Count.ShouldBe(1);
        cut.FindAll("button.filter-pill").Count.ShouldBe(0);
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

    [Fact]
    public void when_rendered_then_a_filter_pill_is_shown_for_each_key_stage_and_format()
    {
        var category = PdfCategoryFactory.Create(1, "Maths", [
            PdfSubcategoryFactory.Create(1, "KS1", [PdfFileFactory.Create(1, "File 1", "pdfs/f1.pdf", 1m)]),
            PdfSubcategoryFactory.Create(2, "KS2", [PdfFileFactory.Create(2, "File 2", "pdfs/f2.pdf", 1m)]),
        ]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));

        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "maths"));

        cut.FindAll("button.filter-pill").Select(pill => pill.TextContent.Trim()).ShouldBe(
            ["All", "KS1", "KS2", "PDF", "Word", "Physical"]);
    }

    [Fact]
    public void when_a_key_stage_filter_pill_is_clicked_then_only_that_key_stages_resources_are_shown()
    {
        var category = PdfCategoryFactory.Create(1, "Maths", [
            PdfSubcategoryFactory.Create(1, "KS1", [PdfFileFactory.Create(1, "File 1", "pdfs/f1.pdf", 1m)]),
            PdfSubcategoryFactory.Create(2, "KS2", [PdfFileFactory.Create(2, "File 2", "pdfs/f2.pdf", 1m)]),
        ]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));
        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "maths"));

        cut.FindAll("button.filter-pill").Single(pill => pill.TextContent.Trim() == "KS1").Click();

        cut.FindAll("article.pdf-card").Count.ShouldBe(1);
    }

    [Fact]
    public void when_the_word_format_filter_pill_is_clicked_then_no_resources_match()
    {
        var category = PdfCategoryFactory.Create(1, "Maths", [
            PdfSubcategoryFactory.Create(1, "KS1", [PdfFileFactory.Create(1, "File 1", "pdfs/f1.pdf", 1m)]),
        ]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));
        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "maths"));

        cut.FindAll("button.filter-pill").Single(pill => pill.TextContent.Trim() == "Word").Click();

        cut.FindAll("article.pdf-card").Count.ShouldBe(0);
        cut.FindAll("div.empty-state").Count.ShouldBe(1);
    }

    [Fact]
    public void when_more_than_eight_resources_match_then_only_the_first_page_is_shown_with_a_load_more_button()
    {
        var files = Enumerable.Range(1, 10).Select(number => PdfFileFactory.Create(number, $"File {number}", $"pdfs/f{number}.pdf", 1m)).ToList();
        var category = PdfCategoryFactory.Create(1, "Maths", [PdfSubcategoryFactory.Create(1, "KS1", files)]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));

        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "maths"));

        cut.FindAll("article.pdf-card").Count.ShouldBe(8);
        cut.FindAll("button.load-more__btn").Count.ShouldBe(1);
    }

    [Fact]
    public void when_load_more_is_clicked_then_the_remaining_resources_are_shown()
    {
        var files = Enumerable.Range(1, 10).Select(number => PdfFileFactory.Create(number, $"File {number}", $"pdfs/f{number}.pdf", 1m)).ToList();
        var category = PdfCategoryFactory.Create(1, "Maths", [PdfSubcategoryFactory.Create(1, "KS1", files)]);
        catalogueService.GetCategoryBySlug("maths").Returns(Option.Some(category));
        var cut = Render<Subject>(parameters => parameters.Add(p => p.subject, "maths"));

        cut.Find("button.load-more__btn").Click();

        cut.FindAll("article.pdf-card").Count.ShouldBe(10);
        cut.FindAll("button.load-more__btn").Count.ShouldBe(0);
    }
}
