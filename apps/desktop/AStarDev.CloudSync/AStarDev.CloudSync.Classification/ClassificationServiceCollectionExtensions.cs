using AStarDev.CloudSync.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Classification;

/// <summary>Registers the classification module.</summary>
public static class ClassificationServiceCollectionExtensions
{
    /// <summary>Registers the classification engine, vocabulary cache, seeding, suggestions, reclassify and the sync hooks (baseline writer and upload recorder). Register the sync persistence and then the classification persistence first: the term and category stores, the baseline writer and the upload recorder registered before this call are wrapped, never replaced. Call it exactly once: a missing earlier registration, or a second call, throws <see cref="InvalidOperationException"/> while the container is composed. The wrapped stores stay resolvable under a private key, so the vocabulary provider reads the stores that do not invalidate it and no cycle forms. The reclassifier needs the <see cref="IAccountSyncLock"/> and the <c>IAccountStore</c>; it is wrapped by a <see cref="RunRecordingReclassifier"/>, which with the <see cref="IReclassifyFreshnessCheck"/> needs the <see cref="IReclassifyRunStore"/> and the <see cref="ISeedStateStore"/>. The reclassifier and the suggestions are singletons, because the reclassifier's single-run guard is per instance and the suggestions own a lock.</summary>
    /// <param name="services">The service collection to register the module with.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddClassification(this IServiceCollection services) =>
        services
            .MoveToUndecoratedKey<ITermStore>()
            .MoveToUndecoratedKey<ICategoryStore>()
            .MoveToUndecoratedKey<ISyncBaselineWriter>()
            .MoveToUndecoratedKey<IUploadRecorder>()
            .AddSingleton<IClassificationVocabularyProvider>(provider => new ClassificationVocabularyProvider(provider.GetRequiredKeyedService<ITermStore>(UndecoratedRegistration.Key), provider.GetRequiredKeyedService<ICategoryStore>(UndecoratedRegistration.Key)))
            .AddSingleton<ITermStore>(provider => new CachingTermStore(provider.GetRequiredKeyedService<ITermStore>(UndecoratedRegistration.Key), provider.GetRequiredService<IClassificationVocabularyProvider>()))
            .AddSingleton<ICategoryStore>(provider => new CachingCategoryStore(provider.GetRequiredKeyedService<ICategoryStore>(UndecoratedRegistration.Key), provider.GetRequiredService<IClassificationVocabularyProvider>()))
            .AddSingleton<ISyncBaselineWriter>(provider => ActivatorUtilities.CreateInstance<ClassifyingSyncBaselineWriter>(provider, provider.GetRequiredKeyedService<ISyncBaselineWriter>(UndecoratedRegistration.Key)))
            .AddSingleton<IUploadRecorder>(provider => ActivatorUtilities.CreateInstance<ClassifyingUploadRecorder>(provider, provider.GetRequiredKeyedService<IUploadRecorder>(UndecoratedRegistration.Key)))
            .AddSingleton<IClassificationSignal, ExtensionSignal>()
            .AddSingleton<IClassificationSignal, FolderSegmentSignal>()
            .AddSingleton<IClassificationSignal, DictionaryTermSignal>()
            .AddSingleton<IFileClassifier, FileClassifier>()
            .AddSingleton(EmbeddedWordListsFactory.CreateDefault())
            .AddSingleton<IKnownWordList>(provider => provider.GetRequiredService<EmbeddedWordLists>())
            .AddSingleton<IFirstNameList>(provider => provider.GetRequiredService<EmbeddedWordLists>())
            .AddSingleton<IClassificationSuggestions, ClassificationSuggestions>()
            .AddSingleton(ReclassifyOptionsFactory.CreateDefault())
            .AddSingleton<ReclassifyProgressSource>()
            .AddSingleton<IReclassifyProgressSource>(provider => provider.GetRequiredService<ReclassifyProgressSource>())
            .AddSingleton(provider => ActivatorUtilities.CreateInstance<RunRecordingReclassifier>(provider, ActivatorUtilities.CreateInstance<ClassificationReclassifier>(provider)))
            .AddSingleton<IClassificationReclassifier>(provider => provider.GetRequiredService<RunRecordingReclassifier>())
            .AddSingleton<IReclassifyRunRecordedSource>(provider => provider.GetRequiredService<RunRecordingReclassifier>())
            .AddSingleton<IReclassifyFreshnessCheck, ReclassifyFreshnessCheck>()
            .AddSingleton<IClassificationSeedSource>(EmbeddedClassificationSeedSourceFactory.CreateDefault())
            .AddSingleton<IClassificationSeeder, ClassificationSeeder>()
            .AddSingleton<IClassificationStartup, ClassificationStartup>();
}
