using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseDateTimeOffsetForTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: SyncedFileMetadata.LastModifiedUtc and CreatedUtc changed
            // from `long` to `DateTimeOffset` at the CLR level. The globally-registered
            // DateTimeOffsetToUnixMillisecondsConverter (ConfigureConventions) maps DateTimeOffset
            // to long, keeping the SQLite column type as INTEGER — no DDL change is needed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: no DDL was changed in Up().
        }
    }
}
