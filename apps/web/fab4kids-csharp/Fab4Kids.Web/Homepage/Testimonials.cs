namespace Fab4Kids.Web.Homepage;

/// <summary>The customer quotes shown in the homepage testimonials section.</summary>
public static class Testimonials
{
    public static IReadOnlyList<Testimonial> All { get; } =
    [
        TestimonialFactory.Create("My son actually asks to do his maths sheets now. That's a first.", "Priya, parent"),
        TestimonialFactory.Create("Beautifully laid out and dead easy to adapt for my class.", "Mr. Okafor, teacher"),
        TestimonialFactory.Create("The Word docs saved me hours of planning every week.", "Sam, home educator"),
    ];
}
