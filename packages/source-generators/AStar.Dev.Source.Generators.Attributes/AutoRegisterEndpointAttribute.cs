using System;
using System.Net.Http;

namespace AStar.Dev.Source.Generators.Attributes;

/// <summary>
/// An attribute to automatically register a minimal APU endpoint.
/// </summary>
/// <remarks>
/// This attribute is used to mark classes for automatic endpoint registration in the system.
/// It supports specifying an HTTP method type and an optional method group name for categorization or grouping.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AutoRegisterEndpointAttribute(HttpMethod? methodType, string? methodGroupName = null) : Attribute
{
    /// <summary>
    /// Gets the HTTP method type associated with the endpoint.
    /// </summary>
    /// <remarks>
    /// If no specific HTTP method is provided during initialization, the default value is <see cref="HttpMethod.Get"/>.
    /// </remarks>
    /// <value>
    /// Represents the HTTP method used for the endpoint, as an instance of the <see cref="System.Net.Http.HttpMethod"/> class.
    /// </value>
    public HttpMethod MethodType { get; } = methodType ?? HttpMethod.Get;

    /// <summary>
    /// Gets the group name associated with the method for organizational purposes.
    /// </summary>
    /// <remarks>
    /// This property provides a way to categorize or group methods logically within a larger structure.
    /// If no group name is specified during initialization, the value will be <see langword="null"/>.
    /// </remarks>
    /// <value>
    /// Represents the name of the method group as a <see cref="string"/>. Can be <see langword="null"/> if not explicitly set.
    /// </value>
    public string? MethodGroupName { get; } = methodGroupName;
}
