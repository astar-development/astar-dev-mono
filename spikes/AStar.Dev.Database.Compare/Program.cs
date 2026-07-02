using AStar.Dev.Database.Compare;
using Microsoft.Extensions.DependencyInjection;

const string oneDriveSyncConnectionString = "Data Source=/home/jbarden/.config/astar-dev-onedrive-sync/astar-dev-onedrive-sync.db";
const string scrapperConnectionString = "Data Source=/home/jbarden/Documents/Scrapper/scrapper-files.db";

var services = new ServiceCollection();
services.AddSingleton<INameReader, SqliteNameReader>();
var serviceProvider = services.BuildServiceProvider();

var nameReader = serviceProvider.GetRequiredService<INameReader>();

var categoryNames = nameReader.ReadNames(oneDriveSyncConnectionString, "FileClassificationCategories", "Name");
var fileClassificationNames = nameReader.ReadNames(scrapperConnectionString, "FileClassification", "Name");

var missingNames = MissingCategoryFinder.FindMissing(fileClassificationNames, categoryNames);

foreach (var missingName in missingNames)
{
    Console.WriteLine(missingName);
}
