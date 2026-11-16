using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public static class SuggestionTestData
{
    public static ClassificationSuggestion Plain(string word, int fileCount) => ClassificationSuggestionFactory.Create(word, fileCount, SuggestionHintFactory.CreatePlain());

    public static ClassificationSuggestion FirstName(string word, int fileCount) => ClassificationSuggestionFactory.Create(word, fileCount, SuggestionHintFactory.CreateFirstName());
}
