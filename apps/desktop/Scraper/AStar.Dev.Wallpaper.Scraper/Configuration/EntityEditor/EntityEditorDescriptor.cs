using AStar.Dev.Infrastructure.AppDb;

namespace AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;

/// <summary>
///     Describes how an <see cref="EntityEditorViewModel{TEntity}" /> should present and persist a single
///     database table.
/// </summary>
/// <typeparam name="TEntity">The entity type backing the table being edited.</typeparam>
/// <param name="DisplayName">The window title and menu text for this editor.</param>
/// <param name="TableName">The base file name (without extension) used for JSON import/export.</param>
/// <param name="CreateNew">Creates a new, defaulted instance of <typeparamref name="TEntity" /> for the Add command.</param>
/// <param name="AllowAddRemove">Whether rows may be added or removed, as opposed to a fixed single-row configuration table.</param>
/// <param name="ExcludedColumns">Property names hidden from the grid (e.g. audit timestamps, navigation properties).</param>
/// <param name="ReadOnlyColumns">Property names shown in the grid but not editable (e.g. the primary key).</param>
/// <param name="OnBeforeAddAsync">
///     Optional hook invoked, for every newly added row, immediately before <c>SaveChangesAsync</c> runs. Used to
///     stamp foreign keys that are not exposed as editable columns.
/// </param>
/// <param name="CustomActionLabel">Optional button text for the editor-specific <paramref name="CustomActionAsync" /> action.</param>
/// <param name="CustomActionAsync">
///     Optional editor-specific action run against the editor's tracked <see cref="AppDbContext" />. The action is
///     responsible for persisting its own changes and returns the status message to display on success.
/// </param>
/// <param name="OrderItemsBy">
///     Optional sort key applied (case-insensitively) whenever rows are loaded or imported, so the grid displays
///     them in a stable order.
/// </param>
public sealed record EntityEditorDescriptor<TEntity>(
    string DisplayName,
    string TableName,
    Func<TEntity> CreateNew,
    bool AllowAddRemove,
    IReadOnlyList<string> ExcludedColumns,
    IReadOnlyList<string> ReadOnlyColumns,
    Func<AppDbContext, TEntity, CancellationToken, Task>? OnBeforeAddAsync = null,
    string? CustomActionLabel = null,
    Func<AppDbContext, CancellationToken, Task<string>>? CustomActionAsync = null,
    Func<TEntity, string>? OrderItemsBy = null)
    where TEntity : class;
