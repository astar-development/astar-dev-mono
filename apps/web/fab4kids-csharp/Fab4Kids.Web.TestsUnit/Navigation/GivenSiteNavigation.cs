using Fab4Kids.Web.Navigation;

namespace Fab4Kids.Web.TestsUnit.Navigation;

public class GivenSiteNavigation
{
    [Fact]
    public void when_subject_links_are_read_then_the_five_subjects_are_present_in_order()
    {
        SiteNavigation.SubjectLinks.Select(l => l.Href).ShouldBe(["/maths", "/english", "/science", "/history", "/geography"]);
    }

    [Fact]
    public void when_subject_links_are_read_then_labels_match_the_subjects()
    {
        SiteNavigation.SubjectLinks.Select(l => l.Label).ShouldBe(["Maths", "English", "Science", "History", "Geography"]);
    }
}
