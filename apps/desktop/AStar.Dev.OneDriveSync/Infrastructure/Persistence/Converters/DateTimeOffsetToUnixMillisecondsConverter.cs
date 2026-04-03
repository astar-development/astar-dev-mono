using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Converters;

/// <summary>
///     EF Core value converter that stores <see cref="DateTimeOffset" /> properties as
///     Unix milliseconds (<see langword="long" />) in the database (AC DB-01).
///
///     Implemented once here and reused across all entity configurations via
///     <c>ApplyConfigurationsFromAssembly</c> — never duplicated per entity.
/// </summary>
public sealed class DateTimeOffsetToUnixMillisecondsConverter()
    : ValueConverter<DateTimeOffset, long>(
        dto => dto.ToUnixTimeMilliseconds(),
        ms  => DateTimeOffset.FromUnixTimeMilliseconds(ms));
