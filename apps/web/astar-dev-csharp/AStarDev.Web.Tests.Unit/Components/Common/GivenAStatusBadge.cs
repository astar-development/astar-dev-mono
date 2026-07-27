using AStarDev.Web.Components.Common;
using Bunit;

namespace AStarDev.Web.Tests.Unit.Components.Common;

public class GivenAStatusBadge : Bunit.BunitContext
{
    [Fact]
    public void when_the_variant_is_available_then_the_available_label_is_shown()
    {
        var cut = Render<StatusBadge>(parameters => parameters.Add(p => p.Variant, StatusBadgeVariant.Available));

        cut.Find(".badge").TextContent.ShouldContain("Available for contracts");
    }

    [Fact]
    public void when_the_variant_is_contributor_then_the_contributor_label_is_shown()
    {
        var cut = Render<StatusBadge>(parameters => parameters.Add(p => p.Variant, StatusBadgeVariant.Contributor));

        cut.Find(".badge").TextContent.ShouldContain("Open source contributor");
    }
}
