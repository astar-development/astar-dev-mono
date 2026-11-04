using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.Localization;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Dashboard;

public sealed class GivenADashboardViewModelUpdatingFileCount
{
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();

    private DashboardViewModel CreateSut() => new(_localizationService, _syncEventAggregator, new DashboardAccountViewModelFactory(_scheduler, _accountRepository, _localizationService, new ActivityItemViewModelFactory(_localizationService), Substitute.For<ILogger<DashboardAccountViewModel>>()), new ActivityItemViewModelFactory(_localizationService), Substitute.For<IUiTimer>());

    private static OneDriveAccount CreateAccount(string id) => new() { Id = new AccountId(id) };

    [Fact]
    public void when_update_file_count_called_for_known_account_then_section_file_count_reflects_new_count()
    {
        var sut = CreateSut();
        sut.AddAccount(CreateAccount("acc-1"));

        sut.UpdateFileCount("acc-1", 42);

        sut.AccountSections.Single().FileCount.ShouldBe(42);
    }

    [Fact]
    public void when_update_file_count_called_for_unknown_account_then_section_file_count_is_unchanged()
    {
        var sut = CreateSut();
        sut.AddAccount(CreateAccount("acc-1"));

        sut.UpdateFileCount("acc-unknown", 42);

        sut.AccountSections.Single().FileCount.ShouldBe(0);
    }

    [Fact]
    public void when_localisation_is_requested_then_files_label_uses_the_dashboard_files_key()
    {
        _localizationService.GetLocal("Dashboard.Files").Returns("files");
        var sut = CreateSut();
        sut.AddAccount(CreateAccount("acc-1"));

        sut.AccountSections.Single().FilesLabelText.ShouldBe("files");
    }
}
