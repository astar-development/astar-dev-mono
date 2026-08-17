using Bunit;
using Fab4Kids.Web.Components.Common;

namespace Fab4Kids.Web.TestsUnit.Components.Common;

public class GivenASubjectCard : Bunit.BunitContext
{
    [Fact]
    public void when_rendered_then_the_label_is_shown()
    {
        var cut = Render<SubjectCard>(parameters => parameters
            .Add(p => p.Label, "Maths")
            .Add(p => p.Letter, "M")
            .Add(p => p.Color, "#3B8FE0")
            .Add(p => p.Description, "Number, algebra and geometry resources.")
            .Add(p => p.Href, "/maths"));

        cut.Find("h3.subject-card__label").TextContent.ShouldBe("Maths");
    }

    [Fact]
    public void when_rendered_then_the_letter_badge_is_shown_with_the_subject_color()
    {
        var cut = Render<SubjectCard>(parameters => parameters
            .Add(p => p.Label, "Maths")
            .Add(p => p.Letter, "M")
            .Add(p => p.Color, "#3B8FE0")
            .Add(p => p.Description, "Number, algebra and geometry resources.")
            .Add(p => p.Href, "/maths"));

        var badge = cut.Find("span.subject-card__badge");
        badge.TextContent.ShouldBe("M");
        badge.GetAttribute("style").ShouldNotBeNull().ShouldContain("#3B8FE0");
    }

    [Fact]
    public void when_rendered_then_the_description_is_shown()
    {
        var cut = Render<SubjectCard>(parameters => parameters
            .Add(p => p.Label, "Maths")
            .Add(p => p.Letter, "M")
            .Add(p => p.Color, "#3B8FE0")
            .Add(p => p.Description, "Number, algebra and geometry resources.")
            .Add(p => p.Href, "/maths"));

        cut.Find("p.subject-card__description").TextContent.ShouldBe("Number, algebra and geometry resources.");
    }

    [Fact]
    public void when_rendered_then_the_card_links_to_the_subject_href()
    {
        var cut = Render<SubjectCard>(parameters => parameters
            .Add(p => p.Label, "Maths")
            .Add(p => p.Letter, "M")
            .Add(p => p.Color, "#3B8FE0")
            .Add(p => p.Description, "Number, algebra and geometry resources.")
            .Add(p => p.Href, "/maths"));

        cut.Find("a.subject-card").GetAttribute("href").ShouldBe("/maths");
    }
}
