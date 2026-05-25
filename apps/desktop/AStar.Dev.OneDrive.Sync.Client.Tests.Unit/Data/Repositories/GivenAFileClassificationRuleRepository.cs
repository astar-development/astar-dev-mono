using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Repositories;

public sealed class GivenAFileClassificationRuleRepository : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _seedingContext;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public GivenAFileClassificationRuleRepository()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _seedingContext = new AppDbContext(options);
        _seedingContext.Database.EnsureCreated();

        _factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        _factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(new AppDbContext(options)));
    }

    public void Dispose()
    {
        _seedingContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task when_no_rules_exist_then_empty_list_is_returned()
    {
        var repository = new FileClassificationRuleRepository(_factory);

        var rules = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        rules.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_rule_has_level1_only_then_tag_name_equals_level1()
    {
        _seedingContext.FileClassificationRules.Add(new FileClassificationRuleEntity { Keywords = "archive|zip|tar", Level1 = "Archives" });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new FileClassificationRuleRepository(_factory);

        var rules = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        rules.Count.ShouldBe(1);
        rules[0].Classification.Level1.ShouldBe("Archives");
        rules[0].Classification.TagName.ShouldBe("Archives");
    }

    [Fact]
    public async Task when_rule_has_level1_and_level2_then_tag_name_equals_level2()
    {
        _seedingContext.FileClassificationRules.Add(new FileClassificationRuleEntity { Keywords = "photos|photo|jpg", Level1 = "Media", Level2 = "Photos" });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new FileClassificationRuleRepository(_factory);

        var rules = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        rules.Count.ShouldBe(1);
        rules[0].Classification.Level1.ShouldBe("Media");
        rules[0].Classification.TagName.ShouldBe("Photos");
    }

    [Fact]
    public async Task when_rule_has_all_three_levels_then_tag_name_equals_level3()
    {
        _seedingContext.FileClassificationRules.Add(new FileClassificationRuleEntity { Keywords = "photos|photo|jpg", Level1 = "Media", Level2 = "Photos", Level3 = "Personal" });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new FileClassificationRuleRepository(_factory);

        var rules = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        rules.Count.ShouldBe(1);
        rules[0].Classification.Level1.ShouldBe("Media");
        rules[0].Classification.TagName.ShouldBe("Personal");
    }

    [Fact]
    public async Task when_rule_has_pipe_delimited_keywords_then_all_keywords_are_returned()
    {
        _seedingContext.FileClassificationRules.Add(new FileClassificationRuleEntity { Keywords = "photos|photo|img|image", Level1 = "Media", Level2 = "Photos" });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new FileClassificationRuleRepository(_factory);

        var rules = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        rules[0].Keywords.Count.ShouldBe(4);
        rules[0].Keywords.ShouldContain("photos");
        rules[0].Keywords.ShouldContain("photo");
        rules[0].Keywords.ShouldContain("img");
        rules[0].Keywords.ShouldContain("image");
    }

    [Fact]
    public async Task when_rule_is_special_then_is_special_flag_is_preserved()
    {
        _seedingContext.FileClassificationRules.Add(new FileClassificationRuleEntity { Keywords = "system|windows", Level1 = "System", IsSpecial = true });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new FileClassificationRuleRepository(_factory);

        var rules = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        rules[0].Classification.IsSpecial.ShouldBeTrue();
    }

    [Fact]
    public async Task when_multiple_rules_exist_then_all_are_returned()
    {
        _seedingContext.FileClassificationRules.AddRange(
            new FileClassificationRuleEntity { Keywords = "photos|jpg", Level1 = "Media", Level2 = "Photos" },
            new FileClassificationRuleEntity { Keywords = "video|mp4", Level1 = "Media", Level2 = "Videos" },
            new FileClassificationRuleEntity { Keywords = "invoice|receipt", Level1 = "Documents", Level2 = "Finance" });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new FileClassificationRuleRepository(_factory);

        var rules = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        rules.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_add_async_called_then_rule_is_persisted_and_id_returned()
    {
        var repository = new FileClassificationRuleRepository(_factory);
        var rule = FileClassificationRuleFactory.Create(["photos", "photo"], FileClassificationFactory.Create("Media", Option.None<string>(), Option.None<string>(), false));

        var id = await repository.AddAsync(rule, TestContext.Current.CancellationToken);

        id.ShouldBeGreaterThan(0);
        _seedingContext.FileClassificationRules.Count().ShouldBe(1);
        _seedingContext.FileClassificationRules.First().Id.ShouldBe(id);
    }

    [Fact]
    public async Task when_delete_async_called_with_existing_id_then_rule_is_removed()
    {
        _seedingContext.FileClassificationRules.Add(new FileClassificationRuleEntity { Keywords = "archive|zip", Level1 = "Archives" });
        await _seedingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        int existingId = _seedingContext.FileClassificationRules.First().Id;
        var repository = new FileClassificationRuleRepository(_factory);

        await repository.DeleteAsync(existingId, TestContext.Current.CancellationToken);

        _seedingContext.ChangeTracker.Clear();
        _seedingContext.FileClassificationRules.Count().ShouldBe(0);
    }

    [Fact]
    public async Task when_delete_async_called_with_missing_id_then_no_exception()
    {
        var repository = new FileClassificationRuleRepository(_factory);

        await Should.NotThrowAsync(() => repository.DeleteAsync(99999, TestContext.Current.CancellationToken));
    }
}
