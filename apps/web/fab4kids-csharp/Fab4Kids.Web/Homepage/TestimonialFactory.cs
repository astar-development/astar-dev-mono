namespace Fab4Kids.Web.Homepage;

/// <summary>Factory for <see cref="Testimonial"/>.</summary>
public static class TestimonialFactory
{
    public static Testimonial Create(string quote, string name) => new(quote, name);
}
