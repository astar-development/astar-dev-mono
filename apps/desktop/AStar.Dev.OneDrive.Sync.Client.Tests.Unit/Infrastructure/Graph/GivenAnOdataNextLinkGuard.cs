using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Graph;

public sealed class GivenAnOdataNextLinkGuard
{
    [Fact]
    public void when_next_link_is_null_then_is_safe_returns_false() =>
        OdataNextLinkGuard.IsSafe(null).ShouldBeFalse();

    [Fact]
    public void when_next_link_is_empty_then_is_safe_returns_false() =>
        OdataNextLinkGuard.IsSafe(string.Empty).ShouldBeFalse();

    [Fact]
    public void when_next_link_is_not_a_valid_uri_then_is_safe_returns_false() =>
        OdataNextLinkGuard.IsSafe("not a valid uri !!!").ShouldBeFalse();

    [Fact]
    public void when_next_link_uses_http_scheme_then_is_safe_returns_false() =>
        OdataNextLinkGuard.IsSafe("http://graph.microsoft.com/v1.0/drives/abc/items/def/children?$skiptoken=xyz").ShouldBeFalse();

    [Fact]
    public void when_next_link_host_is_not_graph_microsoft_com_then_is_safe_returns_false() =>
        OdataNextLinkGuard.IsSafe("https://evil.example.com/v1.0/drives/abc/items/def/children?$skiptoken=xyz").ShouldBeFalse();

    [Fact]
    public void when_next_link_is_valid_https_graph_microsoft_com_url_then_is_safe_returns_true() =>
        OdataNextLinkGuard.IsSafe("https://graph.microsoft.com/v1.0/drives/abc/items/def/children?$skiptoken=xyz").ShouldBeTrue();

    [Fact]
    public void when_next_link_has_graph_microsoft_com_as_subdomain_then_is_safe_returns_false() =>
        OdataNextLinkGuard.IsSafe("https://graph.microsoft.com.evil.example.com/v1.0/items?$skiptoken=xyz").ShouldBeFalse();

    [Fact]
    public void when_next_link_has_mixed_case_host_then_is_safe_returns_true() =>
        OdataNextLinkGuard.IsSafe("https://Graph.Microsoft.Com/v1.0/drives/abc/items/def/children?$skiptoken=xyz").ShouldBeTrue();
}
