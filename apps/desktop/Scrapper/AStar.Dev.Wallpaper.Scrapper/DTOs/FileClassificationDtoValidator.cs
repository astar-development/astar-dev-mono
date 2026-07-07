using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Guard.Clauses;

namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

/// <summary>Validates an imported <see cref="FileClassification" /> row, accumulating every problem found instead of stopping at the first.</summary>
public static class FileClassificationDtoValidator
{
    private const int MinimumLevel = 1;
    private const int MaximumLevel = 3;

    /// <summary>Validates <paramref name="dto" />, tagging any errors with its <paramref name="rowIndex" /> for reporting.</summary>
    public static Validation<FileClassification> Validate(FileClassification dto, int rowIndex)
    {
        GuardAgainst.Null(dto);

        Validation<Func<string, Func<int, FileClassification>>> constructor = Validation.Valid<Func<string, Func<int, FileClassification>>>(_ => _ => dto);

        return constructor
            .Apply(ValidateName(dto, rowIndex))
            .Apply(ValidateLevel(dto, rowIndex));
    }

    /// <summary>Validates every row in <paramref name="dtos" />, accumulating every invalid row's errors, in row order.</summary>
    public static Validation<IReadOnlyList<FileClassification>> ValidateAll(IReadOnlyList<FileClassification> dtos)
    {
        GuardAgainst.Null(dtos);

        return dtos.Select(Validate).Combine();
    }

    private static Validation<string> ValidateName(FileClassification dto, int rowIndex)
        => string.IsNullOrWhiteSpace(dto.Name)
            ? Validation.Invalid<string>(ValidationErrorFactory.Create($"Categories[{rowIndex}].Name", "Name is required."))
            : Validation.Valid(dto.Name);

    private static Validation<int> ValidateLevel(FileClassification dto, int rowIndex)
        => dto.Level is < MinimumLevel or > MaximumLevel
            ? Validation.Invalid<int>(ValidationErrorFactory.Create($"Categories[{rowIndex}].Level", $"Level must be between {MinimumLevel} and {MaximumLevel}."))
            : Validation.Valid(dto.Level);
}
