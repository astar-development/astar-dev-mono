namespace AStar.Dev.Database.Compare;

public interface INameReader
{
    IReadOnlyList<string> ReadNames(string connectionString, string tableName, string columnName);
}
