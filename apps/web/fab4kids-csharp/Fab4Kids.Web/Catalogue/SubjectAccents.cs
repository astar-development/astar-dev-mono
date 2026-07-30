using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Utilities;

namespace Fab4Kids.Web.Catalogue;

/// <summary>The five subject brand accents, per the design handoff's "Browse by subject" grid.</summary>
public static class SubjectAccents
{
    public static IReadOnlyList<SubjectAccent> All { get; } =
    [
        SubjectAccentFactory.Create("Maths", "M", "#3B8FE0", "Number, algebra, geometry & problem-solving.", "/maths"),
        SubjectAccentFactory.Create("English", "E", "#E8483A", "Reading, writing, grammar & comprehension.", "/english"),
        SubjectAccentFactory.Create("Science", "S", "#4CAF6D", "Biology, chemistry, physics & experiments.", "/science"),
        SubjectAccentFactory.Create("History", "H", "#F5A623", "Sources, timelines & topic deep-dives.", "/history"),
        SubjectAccentFactory.Create("Geography", "G", "#2BB6A3", "Maps, fieldwork & world investigations.", "/geography"),
    ];

    public static Option<SubjectAccent> Find(string name) =>
        All.FirstOrDefault(subject => subject.Name.CaseInsensitiveEquals(name)) is { } found
            ? Option.Some(found)
            : Option.None<SubjectAccent>();
}
