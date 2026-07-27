namespace Fab4Kids.Web.Homepage;

/// <summary>The subject cards shown on the homepage, ported from the previous Astro <c>index.astro</c> content.</summary>
public static class HomeSubjectCards
{
    public static IReadOnlyList<HomeSubjectCard> All { get; } =
    [
        HomeSubjectCardFactory.Create("/maths", "\U0001F522", "Maths", "Number, algebra, geometry, statistics and problem-solving resources."),
        HomeSubjectCardFactory.Create("/english", "\U0001F4DA", "English", "Reading, writing, grammar, punctuation and comprehension activities."),
        HomeSubjectCardFactory.Create("/science", "\U0001F52C", "Science", "Biology, chemistry, physics and working scientifically resources."),
        HomeSubjectCardFactory.Create("/history", "\U0001F3DB️", "History", "Primary and secondary sources, timelines and in-depth topic studies."),
        HomeSubjectCardFactory.Create("/geography", "\U0001F30D", "Geography", "Maps, fieldwork, physical and human geography investigations."),
    ];
}
