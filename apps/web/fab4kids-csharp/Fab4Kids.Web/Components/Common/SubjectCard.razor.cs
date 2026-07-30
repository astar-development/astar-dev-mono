using Microsoft.AspNetCore.Components;

namespace Fab4Kids.Web.Components.Common;

/// <summary>A colored subject tile linking to a subject's listing page, used on the homepage "Browse by subject" grid.</summary>
public sealed partial class SubjectCard : ComponentBase
{
    [Parameter, EditorRequired]
    public required string Label { get; set; }

    [Parameter, EditorRequired]
    public required string Letter { get; set; }

    [Parameter, EditorRequired]
    public required string Color { get; set; }

    [Parameter, EditorRequired]
    public required string Description { get; set; }

    [Parameter, EditorRequired]
    public required string Href { get; set; }
}
