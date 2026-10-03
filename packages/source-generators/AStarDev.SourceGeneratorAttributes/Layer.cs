namespace AStarDev.SourceGeneratorAttributes;

/// <summary>Specifies the architectural layer a service registered via <see cref="AutoRegisterServiceAttribute"/> belongs to.</summary>
public enum Layer
{
    /// <summary>The service belongs to the application layer.</summary>
    Application,
    /// <summary>The service belongs to the domain layer.</summary>
    Domain,
    /// <summary>The service belongs to the infrastructure layer.</summary>
    Infrastructure,
    /// <summary>The service does not belong to a specific layer. This is the default when no layer is specified.</summary>
    Miscellaneous
}
