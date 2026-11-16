using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Reclassifies the whole baseline in batches.</summary>
public sealed class ClassificationReclassifier(IClassifiableFileSource fileSource, IFileClassifier classifier, IFileClassificationStore store, ReclassifyOptions options, ILogger<ClassificationReclassifier> logger) : IClassificationReclassifier
{
    /// <inheritdoc />
    public Task<ReclassifyOutcome> StartAsync(IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{fileSource}{classifier}{store}{options}{logger}{progress}{cancellationToken}");
}
