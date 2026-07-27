using Blazored.LocalStorage;
using Bunit;
using Fab4Kids.Web.Cart;
using Fab4Kids.Web.Catalogue;
using Fab4Kids.Web.Components.Common;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Fab4Kids.Web.Tests.Unit.Components.Common;

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
        cut.Find("p.pdf-card__price").TextContent.ShouldBe("£2.50");
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

        cut.Find("button.btn--primary").Click();

        cartState.Items.ShouldHaveSingleItem();
        cartState.Items[0].ProductId.ShouldBe(1);
        cartState.Items[0].Name.ShouldBe("Times Tables Pack");
    }
}
