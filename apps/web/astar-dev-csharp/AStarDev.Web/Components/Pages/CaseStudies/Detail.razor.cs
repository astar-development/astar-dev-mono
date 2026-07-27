using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.CaseStudies;
using Microsoft.AspNetCore.Components;

namespace AStarDev.Web.Components.Pages.CaseStudies;

public partial class Detail : ComponentBase
{
    [Parameter]
    public string Slug { get; set; } = string.Empty;

    private Option<CaseStudy> caseStudy = Option.None<CaseStudy>();

    protected override void OnParametersSet() => caseStudy = CaseStudyCatalog.FindBySlug(Slug);
}
