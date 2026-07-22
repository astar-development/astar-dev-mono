using Avalonia.Controls.Documents;
using AStar.Dev.Wallpaper.Scraper.Home;
using DocumentSpan = Avalonia.Controls.Documents.Span;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Home;

public sealed class GivenStatusMarkupParser
{
    [Fact]
    public void when_text_has_no_markup_then_a_single_run_with_the_original_text_is_returned()
    {
        var inlines = StatusMarkupParser.Parse("Visiting category: Landscapes");

        var run = inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>();
        run.Text.ShouldBe("Visiting category: Landscapes");
    }

    [Fact]
    public void when_text_contains_a_run_tag_with_font_size_then_the_run_font_size_is_set()
    {
        var inlines = StatusMarkupParser.Parse("<Run FontSize=\"24\">Visiting category:</Run> Landscapes");

        var run = inlines[0].ShouldBeOfType<Run>();
        run.Text.ShouldBe("Visiting category:");
        run.FontSize.ShouldBe(24);

        var trailing = inlines[1].ShouldBeOfType<Run>();
        trailing.Text.ShouldBe(" Landscapes");
    }

    [Fact]
    public void when_text_contains_a_bold_tag_then_a_bold_span_wraps_the_content()
    {
        var inlines = StatusMarkupParser.Parse("<Bold>important</Bold>");

        var bold = inlines.ShouldHaveSingleItem().ShouldBeOfType<Bold>();
        var run = bold.Inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>();
        run.Text.ShouldBe("important");
    }

    [Fact]
    public void when_text_contains_an_italic_tag_then_an_italic_span_wraps_the_content()
    {
        var inlines = StatusMarkupParser.Parse("<Italic>italic sections</Italic>");

        var italic = inlines.ShouldHaveSingleItem().ShouldBeOfType<Italic>();
        var run = italic.Inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>();
        run.Text.ShouldBe("italic sections");
    }

    [Fact]
    public void when_text_contains_a_span_with_a_foreground_then_the_foreground_brush_is_set()
    {
        var inlines = StatusMarkupParser.Parse("<Span Foreground=\"Green\">green text</Span>");

        var span = inlines.ShouldHaveSingleItem().ShouldBeOfType<DocumentSpan>();
        span.Foreground.ShouldNotBeNull();
        var run = span.Inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>();
        run.Text.ShouldBe("green text");
    }

    [Fact]
    public void when_tags_are_nested_then_the_inline_tree_reflects_the_nesting()
    {
        var inlines = StatusMarkupParser.Parse("<Span Foreground=\"Green\">green with <Bold>bold sections</Bold> after</Span>");

        var span = inlines.ShouldHaveSingleItem().ShouldBeOfType<DocumentSpan>();
        span.Inlines.Count.ShouldBe(3);
        span.Inlines[0].ShouldBeOfType<Run>().Text.ShouldBe("green with ");
        var bold = span.Inlines[1].ShouldBeOfType<Bold>();
        bold.Inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>().Text.ShouldBe("bold sections");
        span.Inlines[2].ShouldBeOfType<Run>().Text.ShouldBe(" after");
    }

    [Fact]
    public void when_a_tag_is_unterminated_then_the_original_text_is_returned_as_a_single_run()
    {
        string original = "Visiting category: <Bold>Landscapes";

        var inlines = StatusMarkupParser.Parse(original);

        var run = inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>();
        run.Text.ShouldBe(original);
    }

    [Fact]
    public void when_a_closing_tag_has_no_matching_opening_tag_then_the_original_text_is_returned_as_a_single_run()
    {
        string original = "Visiting category: Landscapes</Bold>";

        var inlines = StatusMarkupParser.Parse(original);

        var run = inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>();
        run.Text.ShouldBe(original);
    }

    [Fact]
    public void when_text_contains_an_unescaped_ampersand_then_it_is_treated_as_literal_text()
    {
        string original = "Visiting category: Landscapes, page 1 with searchString: /search?q=x&page=2";

        var inlines = StatusMarkupParser.Parse(original);

        var run = inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>();
        run.Text.ShouldBe(original);
    }

    [Fact]
    public void when_a_run_attribute_value_is_not_a_valid_number_then_the_original_text_is_returned_as_a_single_run()
    {
        string original = "<Run FontSize=\"huge\">Visiting category:</Run> Landscapes";

        var inlines = StatusMarkupParser.Parse(original);

        var run = inlines.ShouldHaveSingleItem().ShouldBeOfType<Run>();
        run.Text.ShouldBe(original);
    }
}
