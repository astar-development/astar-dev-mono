namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAnAccountProfile
{
    private const string DisplayName = "Jason Smith";
    private const string Email = "jason@outlook.com";

    [Fact]
    public void when_created_then_display_name_is_set_correctly()
    {
        var profile = AccountProfileFactory.Create(DisplayName, Email);

        profile.DisplayName.ShouldBe(DisplayName);
    }

    [Fact]
    public void when_created_then_email_is_set_correctly()
    {
        var profile = AccountProfileFactory.Create(DisplayName, Email);

        profile.Email.ShouldBe(Email);
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var first = AccountProfileFactory.Create(DisplayName, Email);
        var second = AccountProfileFactory.Create(DisplayName, Email);

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_display_names_then_they_are_not_equal()
    {
        var first = AccountProfileFactory.Create(DisplayName, Email);
        var second = AccountProfileFactory.Create("Other Name", Email);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_emails_then_they_are_not_equal()
    {
        var first = AccountProfileFactory.Create(DisplayName, Email);
        var second = AccountProfileFactory.Create(DisplayName, "other@outlook.com");

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_empty_is_requested_then_both_fields_are_empty_strings()
    {
        var empty = AccountProfileFactory.Empty;

        empty.DisplayName.ShouldBe(string.Empty);
        empty.Email.ShouldBe(string.Empty);
    }
}
