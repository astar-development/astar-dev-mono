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
        _repository.UpdateAsync(Arg.Any<int>(), Arg.Any<FileClassificationRule>(), Arg.Any<CancellationToken>())
                   .Returns(Task.CompletedTask);
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
        FileClassificationRulesViewModel sut = new(_repository)
        {
            NewKeywords = "photos, photo",
            NewLevel1 = "Media"
        };

        await sut.AddCommand.ExecuteAsync(null);

        await _repository.Received(1).AddAsync(Arg.Any<FileClassificationRule>(), Arg.Any<CancellationToken>());
        sut.Rules.Count.ShouldBe(1);
    }

    [Fact]
    public void when_keywords_empty_then_add_command_disabled()
    {
        FileClassificationRulesViewModel sut = new(_repository)
        {
            NewKeywords = string.Empty,
            NewLevel1 = "Media"
        };

        sut.AddCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void when_level1_empty_then_add_command_disabled()
    {
        FileClassificationRulesViewModel sut = new(_repository)
        {
            NewKeywords = "photos",
            NewLevel1 = string.Empty
        };

        sut.AddCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task when_add_succeeds_then_form_inputs_cleared()
    {
        FileClassificationRulesViewModel sut = new(_repository)
        {
            NewKeywords = "photos, photo",
            NewLevel1 = "Media",
            NewLevel2 = "Photos",
            NewLevel3 = "Personal",
            NewIsSpecial = true
        };

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

    [Fact]
    public async Task when_rule_updated_then_repository_is_called_and_row_values_refresh()
    {
        IReadOnlyList<FileClassificationRuleEntry> entries =
        [
            new(7, FileClassificationRuleFactory.Create(["budget"], FileClassificationFactory.Create("Finance", Option.None<string>(), Option.None<string>(), false)))
        ];
        _repository.GetAllWithIdsAsync(Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(entries));
        FileClassificationRulesViewModel sut = new(_repository);
        await sut.LoadAsync(CancellationToken.None);

        await sut.Rules[0].EditCommand.ExecuteAsync(null);
        sut.Rules[0].Keywords = "budget, finance";
        sut.Rules[0].Level1 = "Finances";
        sut.Rules[0].Level2 = "Budget";
        sut.Rules[0].Level3 = "Quarterly";
        sut.Rules[0].IsSpecial = true;

        await sut.Rules[0].SaveCommand.ExecuteAsync(null);

        await _repository.Received(1).UpdateAsync(
            7,
            Arg.Is<FileClassificationRule>(rule =>
                rule.Keywords.Count == 2 &&
                rule.Keywords[0] == "budget" &&
                rule.Keywords[1] == "finance" &&
                rule.Classification.Level1 == "Finances" &&
                rule.Classification.Level2 == Option.Some("Budget") &&
                rule.Classification.Level3 == Option.Some("Quarterly") &&
                rule.Classification.IsSpecial),
            Arg.Any<CancellationToken>());
        sut.Rules[0].Keywords.ShouldBe("budget, finance");
        sut.Rules[0].Level1.ShouldBe("Finances");
        sut.Rules[0].Level2.ShouldBe("Budget");
        sut.Rules[0].Level3.ShouldBe("Quarterly");
        sut.Rules[0].IsSpecial.ShouldBeTrue();
        sut.Rules[0].IsEditing.ShouldBeFalse();
    }
}
