using AStar.Dev.Database.Compare;
using Microsoft.Extensions.DependencyInjection;

const string oneDriveSyncConnectionString = "Data Source=/home/jbarden/.config/astar-dev-onedrive-sync/astar-dev-onedrive-sync.db";
const string scrapperConnectionString = "Data Source=/home/jbarden/Documents/Scrapper/scrapper-files.db";

var services = new ServiceCollection();
services.AddSingleton<INameReader, SqliteNameReader>();
var serviceProvider = services.BuildServiceProvider();

var nameReader = serviceProvider.GetRequiredService<INameReader>();

var categoryNames = nameReader.ReadNames(oneDriveSyncConnectionString, "FileClassificationCategories", "Name, IsFamous");
var fileClassificationNames = nameReader.ReadNames(scrapperConnectionString, "FileClassification", "Name, IncludeInSearch");

var missingNames = MissingCategoryFinder.FindMissing(fileClassificationNames, categoryNames);

const string ExportPath = "/home/jbarden/repos/astar-dev-mono/spikes/AStar.Dev.Database.Compare/categories.txt";

File.Delete(ExportPath);
foreach (var missingName in missingNames.OrderBy(i => i.Item1))
{
    Console.WriteLine(missingName);
    File.AppendAllText(ExportPath, $"{missingName.Item1}, {missingName.Item2}{Environment.NewLine}");
}
