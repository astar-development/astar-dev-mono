using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

public sealed class GivenASyncRuleEvaluator
{
    private static readonly AccountId AnyAccount = new("acc-1");

    private static SyncRuleEntity Rule(string path, RuleType type) => new()
    {
        AccountId  = AnyAccount,
        RemotePath = path,
        RuleType   = type
    };

    [Fact]
    public void when_no_rules_then_path_is_excluded()
    {
        var result = SyncRuleEvaluator.IsIncluded("/Documents", []);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_path_matches_include_rule_then_is_included()
    {
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var result = SyncRuleEvaluator.IsIncluded("/Documents", rules);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_path_matches_exclude_rule_then_is_excluded()
    {
        var rules = new[] { Rule("/Documents", RuleType.Exclude) };

        var result = SyncRuleEvaluator.IsIncluded("/Documents", rules);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_path_does_not_match_any_rule_then_is_excluded()
    {
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var result = SyncRuleEvaluator.IsIncluded("/Photos", rules);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_child_path_matches_parent_include_rule_then_is_included()
    {
        var rules = new[] { Rule("/Documents", RuleType.Include) };

        var result = SyncRuleEvaluator.IsIncluded("/Documents/Reports/Q1.pdf", rules);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_more_specific_exclude_overrides_parent_include_then_is_excluded()
    {
        var rules = new[]
        {
            Rule("/Documents",         RuleType.Include),
            Rule("/Documents/Private", RuleType.Exclude)
        };

        var result = SyncRuleEvaluator.IsIncluded("/Documents/Private/secret.txt", rules);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_more_specific_include_overrides_parent_exclude_then_is_included()
    {
        var rules = new[]
        {
            Rule("/Documents",                  RuleType.Exclude),
            Rule("/Documents/Reports/Approved", RuleType.Include)
        };

        var result = SyncRuleEvaluator.IsIncluded("/Documents/Reports/Approved/final.docx", rules);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_two_rules_same_length_and_one_is_exclude_then_exclude_wins()
    {
        var rules = new[]
        {
            Rule("/Docs", RuleType.Include),
            Rule("/Docs", RuleType.Exclude)
        };

        var result = SyncRuleEvaluator.IsIncluded("/Docs/file.txt", rules);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_path_exactly_equals_include_rule_path_then_is_included()
    {
        var rules = new[] { Rule("/Photos", RuleType.Include) };

        var result = SyncRuleEvaluator.IsIncluded("/Photos", rules);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_path_is_partial_prefix_match_but_not_a_real_prefix_then_is_excluded()
    {
        var rules = new[] { Rule("/Doc", RuleType.Include) };

        var result = SyncRuleEvaluator.IsIncluded("/Documents/file.txt", rules);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_multiple_include_rules_most_specific_wins()
    {
        var rules = new[]
        {
            Rule("/Documents",         RuleType.Include),
            Rule("/Documents/Reports", RuleType.Include)
        };

        var result = SyncRuleEvaluator.IsIncluded("/Documents/Reports/q1.pdf", rules);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_rule_matching_is_case_insensitive_then_path_is_included()
    {
        var rules = new[] { Rule("/documents", RuleType.Include) };

        var result = SyncRuleEvaluator.IsIncluded("/Documents/file.txt", rules);

        result.ShouldBeTrue();
    }
}
