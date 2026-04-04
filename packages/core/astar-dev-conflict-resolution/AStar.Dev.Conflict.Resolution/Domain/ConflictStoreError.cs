using System;

namespace AStar.Dev.Conflict.Resolution.Domain;

/// <summary>Base discriminated union for conflict store failures.</summary>
public abstract record ConflictStoreError(string Message);

/// <summary>Returned when the requested conflict record cannot be found.</summary>
public sealed record ConflictNotFoundError(Guid ConflictId) : ConflictStoreError($"Conflict {ConflictId} was not found in the store.");

/// <summary>Returned when a database write fails.</summary>
public sealed record ConflictStorePersistenceError(string Detail) : ConflictStoreError($"Persistence failed: {Detail}");

/// <summary>Factory for <see cref="ConflictStoreError"/> subtypes.</summary>
public static class ConflictStoreErrorFactory
{
    /// <summary>Creates a <see cref="ConflictNotFoundError"/>.</summary>
    public static ConflictNotFoundError NotFound(Guid conflictId) => new(conflictId);

    /// <summary>Creates a <see cref="ConflictStorePersistenceError"/>.</summary>
    public static ConflictStorePersistenceError Persistence(string detail) => new(detail);
}
