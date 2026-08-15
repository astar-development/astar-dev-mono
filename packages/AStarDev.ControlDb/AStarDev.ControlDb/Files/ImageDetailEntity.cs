namespace AStarDev.ControlDb.Files;

/// <summary>
/// Represents the details of an image file entity, including its dimensions.
/// </summary>
/// <param name="Id">Primary key for the image detail entity.</param>
/// <param name="FileEntityId">Foreign key to the parent <see cref="FileEntity"/>.</param>
/// <param name="Width">The width of the image in pixels, or null if not an image.</param>
/// <param name="Height">The height of the image in pixels, or null if not an image.</param>
public sealed record ImageDetailEntity(ImageDetailId Id, FileId FileEntityId, int? Width, int? Height);
