namespace AStar.Dev.Database.Compare;

public interface INameReader
{
    IReadOnlyList<(string Name, bool IncludeInSearch)> ReadNames(string connectionString, string tableName, string columnNames);
}
