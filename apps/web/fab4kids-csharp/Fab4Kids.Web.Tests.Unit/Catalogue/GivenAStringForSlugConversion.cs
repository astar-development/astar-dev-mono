using Fab4Kids.Web.Catalogue;

namespace Fab4Kids.Web.Tests.Unit.Catalogue;

public class GivenAStringForSlugConversion
{
    [Fact]
    public void when_the_value_is_a_single_word_then_it_is_lowercased()
    {
        "Maths".ToSlug().ShouldBe("maths");
    }

    [Fact]
    public void when_the_value_has_a_single_space_then_it_becomes_a_hyphen()
    {
        "Key Stage".ToSlug().ShouldBe("key-stage");
    }

    [Fact]
    public void when_the_value_has_multiple_consecutive_spaces_then_they_collapse_to_one_hyphen()
    {
        "Key   Stage  1".ToSlug().ShouldBe("key-stage-1");
    }

    [Fact]
    public void when_the_value_has_leading_and_trailing_whitespace_then_it_is_trimmed()
    {
        "  KS1  ".ToSlug().ShouldBe("ks1");
    }
}
