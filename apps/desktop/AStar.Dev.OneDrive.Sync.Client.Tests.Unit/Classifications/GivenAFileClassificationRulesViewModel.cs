using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenAFileClassificationRulesViewModel
{
    private readonly IFileClassificationRuleRepository _repository;

    public GivenAFileClassificationRulesViewModel()
    {
        _repository = Substitute.For<IFileClassificationRuleRepository>();
        _repository.GetAllWithIdsAsync(Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult<IReadOnlyList<FileClassificationRuleEntry>>([]));
        _repository.AddAsync(Arg.Any<FileClassificationRule>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(1));
    }

    [Fact]
    public async Task when_load_async_called_then_rules_collection_populated()
    {
        IReadOnlyList<FileClassificationRuleEntry> entries =
        [
            new(1, FileClassificationRuleFactory.Create(["photos"], FileClassificationFactory.Create("Media", Option.None<string>(), Option.None<string>(), false))),
            new(2, FileClassificationRuleFactory.Create(["docs"], FileClassificationFactory.Create("Documents", Option.None<string>(), Option.None<string>(), false)))
        ];
        _repository.GetAllWithIdsAsync(Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(entries));
        FileClassificationRulesViewModel sut = new(_repository);

        await sut.LoadAsync(CancellationToken.None);

        sut.Rules.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_add_command_executed_then_rule_persisted_and_added_to_collection()
    {
        FileClassificationRulesViewModel sut = new(_repository);
        sut.NewKeywords = "photos, photo";
        sut.NewLevel1 = "Media";

        await sut.AddCommand.ExecuteAsync(null);

        await _repository.Received(1).AddAsync(Arg.Any<FileClassificationRule>(), Arg.Any<CancellationToken>());
        sut.Rules.Count.ShouldBe(1);
    }

    [Fact]
    public void when_keywords_empty_then_add_command_disabled()
    {
        FileClassificationRulesViewModel sut = new(_repository);
        sut.NewKeywords = string.Empty;
        sut.NewLevel1 = "Media";

        sut.AddCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void when_level1_empty_then_add_command_disabled()
    {
        FileClassificationRulesViewModel sut = new(_repository);
        sut.NewKeywords = "photos";
        sut.NewLevel1 = string.Empty;

        sut.AddCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task when_add_succeeds_then_form_inputs_cleared()
    {
        FileClassificationRulesViewModel sut = new(_repository);
        sut.NewKeywords = "photos, photo";
        sut.NewLevel1 = "Media";
        sut.NewLevel2 = "Photos";
        sut.NewLevel3 = "Personal";
        sut.NewIsSpecial = true;

        await sut.AddCommand.ExecuteAsync(null);

        sut.NewKeywords.ShouldBeEmpty();
        sut.NewLevel1.ShouldBeEmpty();
        sut.NewLevel2.ShouldBeEmpty();
        sut.NewLevel3.ShouldBeEmpty();
        sut.NewIsSpecial.ShouldBeFalse();
    }

    [Fact]
    public async Task when_delete_command_executed_then_rule_deleted_and_removed_from_collection()
    {
        IReadOnlyList<FileClassificationRuleEntry> entries =
        [
            new(42, FileClassificationRuleFactory.Create(["archive"], FileClassificationFactory.Create("Archives", Option.None<string>(), Option.None<string>(), false)))
        ];
        _repository.GetAllWithIdsAsync(Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(entries));
        FileClassificationRulesViewModel sut = new(_repository);
        await sut.LoadAsync(CancellationToken.None);

        await sut.Rules[0].DeleteCommand.ExecuteAsync(null);

        await _repository.Received(1).DeleteAsync(42, Arg.Any<CancellationToken>());
        sut.Rules.ShouldBeEmpty();
    }
}
