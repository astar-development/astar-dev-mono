using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Components.Common;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.TestsUnit.Components.Common;

public class GivenAPdfCard : Bunit.BunitContext
{
    private readonly ILocalStorageService localStorage = Substitute.For<ILocalStorageService>();

    public GivenAPdfCard() => Services.AddSingleton(new CartState(localStorage));

    [Fact]
    public void when_rendered_then_the_name_and_price_are_shown()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters.Add(p => p.File, file));

        cut.Find("h3.pdf-card__name").TextContent.ShouldBe("Times Tables Pack");
        cut.Find("span.pdf-card__price").TextContent.ShouldBe("£2.50");
    }

    [Fact]
    public void when_rendered_then_the_view_link_points_at_the_file_url()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters.Add(p => p.File, file));

        var link = cut.Find("a.btn--secondary");
        link.GetAttribute("href").ShouldBe("pdfs/times-tables.pdf");
        link.GetAttribute("target").ShouldBe("_blank");
    }

    [Fact]
    public void when_the_add_to_basket_button_is_clicked_then_the_item_is_added_to_the_cart()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);
        var cartState = Services.GetRequiredService<CartState>();
        var cut = Render<PdfCard>(parameters => parameters.Add(p => p.File, file));

        cut.Find("button.pdf-card__add-btn").Click();

        cartState.Items.ShouldHaveSingleItem();
        cartState.Items[0].ProductId.ShouldBe(1);
        cartState.Items[0].Name.ShouldBe("Times Tables Pack");
    }

    [Fact]
    public void when_the_add_to_basket_button_is_clicked_then_the_blob_path_is_added_to_the_cart()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);
        var cartState = Services.GetRequiredService<CartState>();
        var cut = Render<PdfCard>(parameters => parameters.Add(p => p.File, file));

        cut.Find("button.pdf-card__add-btn").Click();

        cartState.Items[0].BlobPath.ShouldBe("pdfs/times-tables.pdf");
    }

    [Fact]
    public void when_href_is_not_set_then_the_view_link_is_shown()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters.Add(p => p.File, file));

        cut.FindAll("a.btn--secondary").Count.ShouldBe(1);
    }

    [Fact]
    public void when_href_is_set_then_the_title_links_to_it_and_the_view_link_is_hidden()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters
            .Add(p => p.File, file)
            .Add(p => p.Href, "/maths/resource/1"));

        cut.Find("a.pdf-card__name-link").GetAttribute("href").ShouldBe("/maths/resource/1");
        cut.FindAll("a.btn--secondary").Count.ShouldBe(0);
    }

    [Fact]
    public void when_subject_name_is_set_then_the_subject_badge_is_shown()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters
            .Add(p => p.File, file)
            .Add(p => p.SubjectName, "Maths")
            .Add(p => p.SubjectColor, "#3B8FE0"));

        var badge = cut.Find("span.pdf-card__badge--subject");
        badge.TextContent.ShouldBe("Maths");
        badge.GetAttribute("style").ShouldNotBeNull().ShouldContain("#3B8FE0");
    }

    [Fact]
    public void when_rendered_then_the_format_badge_defaults_to_pdf()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters.Add(p => p.File, file));

        cut.Find("span.pdf-card__badge--format").TextContent.ShouldBe("PDF");
    }

    [Fact]
    public void when_key_stage_label_is_set_then_it_is_shown()
    {
        var file = PdfFileFactory.Create(1, "Times Tables Pack", "pdfs/times-tables.pdf", 2.50m);

        var cut = Render<PdfCard>(parameters => parameters
            .Add(p => p.File, file)
            .Add(p => p.KeyStageLabel, "KS1"));

        cut.Find("p.pdf-card__stage").TextContent.ShouldBe("KS1");
    }
}
