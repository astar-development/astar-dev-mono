using System.Reflection;
using AStar.Dev.FunctionalParadigm;
using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Components.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Pages;

public class GivenAResourcePage : Bunit.BunitContext
{
    private readonly ICatalogueService catalogueService = Substitute.For<ICatalogueService>();
    private readonly CartState cartState = new(Substitute.For<ILocalStorageService>());

    public GivenAResourcePage()
    {
        Services.AddSingleton(catalogueService);
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<Resource>>(NullLogger<Resource>.Instance);
        Services.AddSingleton(cartState);
    }

    [Fact]
    public void when_the_file_id_is_known_then_the_title_price_and_badges_are_shown()
    {
        var file = PdfFileFactory.Create(1, "Fractions Fun Pack", "pdfs/fractions.pdf", 3.50m);
        var subcategory = PdfSubcategoryFactory.Create(1, "KS2", [file]);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategory]);
        catalogueService.GetFileById("maths", 1).Returns(Option.Some(PdfFileLookupFactory.Create(category, subcategory, file)));

        var cut = Render<Resource>(parameters => parameters
            .Add(p => p.subject, "maths")
            .Add(p => p.fileId, 1));

        cut.Find("h1.detail__title").TextContent.ShouldBe("Fractions Fun Pack");
        cut.Find("div.detail__price").TextContent.ShouldBe("£3.50");
        cut.Find("span.detail__badge--subject").TextContent.ShouldBe("Maths");
        cut.Find("span.detail__badge--stage").TextContent.ShouldBe("KS2");
    }

    [Fact]
    public void when_the_add_to_basket_button_is_clicked_then_the_item_is_added_to_the_cart()
    {
        var file = PdfFileFactory.Create(1, "Fractions Fun Pack", "pdfs/fractions.pdf", 3.50m);
        var subcategory = PdfSubcategoryFactory.Create(1, "KS2", [file]);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategory]);
        catalogueService.GetFileById("maths", 1).Returns(Option.Some(PdfFileLookupFactory.Create(category, subcategory, file)));
        var cut = Render<Resource>(parameters => parameters
            .Add(p => p.subject, "maths")
            .Add(p => p.fileId, 1));

        cut.Find("button.detail__add-btn").Click();

        cartState.Items.ShouldHaveSingleItem();
        cartState.Items[0].ProductId.ShouldBe(1);
    }

    [Fact]
    public void when_other_files_exist_in_the_same_subject_then_related_resources_are_shown_excluding_the_current_one()
    {
        var current = PdfFileFactory.Create(1, "Fractions Fun Pack", "pdfs/fractions.pdf", 3.50m);
        var other = PdfFileFactory.Create(2, "Times Tables Race Cards", "pdfs/times-tables.pdf", 2.50m);
        var subcategory = PdfSubcategoryFactory.Create(1, "KS2", [current, other]);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategory]);
        catalogueService.GetFileById("maths", 1).Returns(Option.Some(PdfFileLookupFactory.Create(category, subcategory, current)));

        var cut = Render<Resource>(parameters => parameters
            .Add(p => p.subject, "maths")
            .Add(p => p.fileId, 1));

        cut.FindAll("article.pdf-card").Count.ShouldBe(1);
        cut.Find("h2.related__title").TextContent.ShouldBe("You might also like");
    }

    [Fact]
    public void when_the_file_id_is_unknown_then_the_response_status_code_is_set_to_not_found()
    {
        catalogueService.GetFileById("maths", 999).Returns(Option.None<PdfFileLookup>());
        var httpContext = new DefaultHttpContext();

        Render<Resource>(parameters => parameters
            .Add(p => p.subject, "maths")
            .Add(p => p.fileId, 999)
            .AddCascadingValue(httpContext));

        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task when_the_request_is_aborted_then_add_to_basket_does_not_wait_out_the_feedback_delay()
    {
        var file = PdfFileFactory.Create(1, "Fractions Fun Pack", "pdfs/fractions.pdf", 3.50m);
        var subcategory = PdfSubcategoryFactory.Create(1, "KS2", [file]);
        var category = PdfCategoryFactory.Create(1, "Maths", [subcategory]);
        catalogueService.GetFileById("maths", 1).Returns(Option.Some(PdfFileLookupFactory.Create(category, subcategory, file)));
        var httpContext = new DefaultHttpContext();
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Xunit.TestContext.Current.CancellationToken);
        await cancellationTokenSource.CancelAsync();
        httpContext.RequestAborted = cancellationTokenSource.Token;

        var cut = Render<Resource>(parameters => parameters
            .Add(p => p.subject, "maths")
            .Add(p => p.fileId, 1)
            .AddCascadingValue(httpContext));

        var addToBasketAsync = typeof(Resource).GetMethod("AddToBasketAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        await Should.ThrowAsync<OperationCanceledException>(() => cut.InvokeAsync(() => (Task)addToBasketAsync.Invoke(cut.Instance, [])!));
    }
}
