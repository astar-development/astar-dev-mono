using System.Reflection;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Logging;

public sealed class GivenTheOneDriveSyncClientMessages
{
    private const string PersonalValue = "/home/ada.lovelace/OneDrive/Secret Plans/plan.docx";

    private static readonly HashSet<string> PersonalParameterNames = ["path", "folderPath", "rootPath", "candidatePath", "location", "accountId", "url"];

    public static TheoryData<string> TemplatesWithPersonalParameters()
    {
        var names = new TheoryData<string>();
        foreach (var method in PublicTemplates().Where(method => method.GetParameters().Any(parameter => PersonalParameterNames.Contains(parameter.Name!))))
            names.Add(method.Name);

        return names;
    }

    [Theory]
    [MemberData(nameof(TemplatesWithPersonalParameters))]
    public void when_a_template_is_given_a_path_or_account_id_then_no_logged_value_contains_it(string templateName)
    {
        foreach (var method in PublicTemplates().Where(method => method.Name == templateName))
        {
            var logger = CreateLogger(method.GetParameters()[0].ParameterType);

            _ = method.Invoke(null, [logger, .. method.GetParameters().Skip(1).Select(ArgumentFor)]);

            var values = ((IValueRecorder)logger).Values;
            values.ShouldNotBeEmpty();
            values.ShouldAllBe(value => !value.Contains("ada.lovelace", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void when_two_templates_are_given_the_same_path_then_both_log_the_same_path_hash()
    {
        var first = new ValueRecordingLogger<GivenTheOneDriveSyncClientMessages>();
        var second = new ValueRecordingLogger<GivenTheOneDriveSyncClientMessages>();

        OneDriveSyncClientMessages.SyncWorkerJobCancelledRequeued(first, 1, PersonalValue);
        OneDriveSyncClientMessages.SyncWorkerJobCancelledRequeued(second, 2, PersonalValue);

        first.ValuesByKey["PathHash"].ShouldBe(second.ValuesByKey["PathHash"]);
        first.ValuesByKey["PathHash"].ShouldMatch("^[0-9a-f]{16}$");
    }

    private static IEnumerable<MethodInfo> PublicTemplates() => typeof(OneDriveSyncClientMessages).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method => method.GetParameters().FirstOrDefault()?.ParameterType is { } type && typeof(ILogger).IsAssignableFrom(type));

    private static object CreateLogger(Type loggerType)
    {
        var category = loggerType.IsGenericType ? loggerType.GetGenericArguments()[0] : typeof(GivenTheOneDriveSyncClientMessages);

        return Activator.CreateInstance(typeof(ValueRecordingLogger<>).MakeGenericType(category))!;
    }

    private static object? ArgumentFor(ParameterInfo parameter)
    {
        if (parameter.ParameterType == typeof(string))
            return PersonalParameterNames.Contains(parameter.Name!) ? PersonalValue : "a non-personal value";

        return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
    }

    private interface IValueRecorder
    {
        IReadOnlyList<string> Values { get; }
    }

    private sealed class ValueRecordingLogger<T> : ILogger<T>, IValueRecorder
    {
        private readonly List<string> values = [];

        public IReadOnlyList<string> Values => values;

        public Dictionary<string, string> ValuesByKey { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            values.Add(formatter(state, exception));
            if (state is not IEnumerable<KeyValuePair<string, object?>> pairs)
                return;

            foreach (var pair in pairs)
            {
                values.Add(pair.Value?.ToString() ?? string.Empty);
                ValuesByKey[pair.Key] = pair.Value?.ToString() ?? string.Empty;
            }
        }
    }
}
