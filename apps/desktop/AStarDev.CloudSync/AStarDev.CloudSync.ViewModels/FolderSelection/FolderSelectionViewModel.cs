using System.Collections.ObjectModel;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>The dialog where the user ticks which folders of an account sync (#1397). Ticks and unticks are staged and only saved when the user presses Apply.</summary>
public sealed class FolderSelectionViewModel : LocalizedObservableObject, IFolderSelectionDialog
{
    private readonly IFolderTreeSource tree;
    private readonly ISelectionRuleReader rules;
    private readonly ISelectionRuleWriter writer;
    private readonly ISyncSelectionEvaluator evaluator;
    private readonly ISyncPairReader pairReader;
    private readonly IAccountStore accounts;
    private readonly List<SelectionChange> staged = [];
    private TaskCompletionSource closed = new();
    private CancellationToken callerToken = CancellationToken.None;
    private AccountId account;
    private string accountName = string.Empty;
    private IReadOnlyList<SelectionRule> stored = [];
    private IReadOnlyList<SyncPair> pairs = [];
    private bool isLoadingRoot;
    private string rootErrorText = string.Empty;
    private string messageText = string.Empty;

    /// <summary>Creates the dialog view model.</summary>
    /// <param name="tree">Lists one level of the merged folder tree.</param>
    /// <param name="rules">Reads the stored selection rules.</param>
    /// <param name="writer">Saves the staged changes.</param>
    /// <param name="evaluator">Judges folders against the rules.</param>
    /// <param name="pairReader">Tells which pairs have synced.</param>
    /// <param name="accounts">Lists the accounts to choose from and names the chosen one.</param>
    /// <param name="localization">Supplies the text.</param>
    public FolderSelectionViewModel(IFolderTreeSource tree, ISelectionRuleReader rules, ISelectionRuleWriter writer, ISyncSelectionEvaluator evaluator, ISyncPairReader pairReader, IAccountStore accounts, ILocalizationService localization) : base(localization)
    {
        this.tree = tree;
        this.rules = rules;
        this.writer = writer;
        this.evaluator = evaluator;
        this.pairReader = pairReader;
        this.accounts = accounts;
        ApplyCommand = new AsyncRelayCommand(ApplyAsync, () => Step is FolderSelectionStep.Tree && HasChanges);
        ConfirmCommand = new AsyncRelayCommand(ConfirmAsync);
        BackCommand = new RelayCommand(Back);
        CancelCommand = new RelayCommand(Close);
        RetryCommand = new AsyncRelayCommand(RetryAsync);
    }

    /// <summary>Where the dialog is.</summary>
    public FolderSelectionStep Step { get; private set; } = FolderSelectionStepFactory.CreateClosed();

    /// <summary>Whether the dialog is showing.</summary>
    public bool IsOpen => Step is not FolderSelectionStep.Closed;

    /// <summary>Whether the account list is showing.</summary>
    public bool IsChoosingAccount => Step is FolderSelectionStep.ChoosingAccount;

    /// <summary>Whether the folder tree is showing.</summary>
    public bool IsShowingTree => Step is FolderSelectionStep.Tree or FolderSelectionStep.Saving;

    /// <summary>Whether the untick confirmation is showing.</summary>
    public bool IsConfirming => Step is FolderSelectionStep.ConfirmingUntick;

    /// <summary>Whether the changes are being saved.</summary>
    public bool IsSaving => Step is FolderSelectionStep.Saving;

    /// <summary>Whether the first level of the tree is loading.</summary>
    public bool IsLoadingRoot => isLoadingRoot;

    /// <summary>Why the first level or the saved choices could not be read, or empty.</summary>
    public string RootErrorText => rootErrorText;

    /// <summary>Whether the first level failed to load.</summary>
    public bool HasRootError => rootErrorText.Length > 0;

    /// <summary>The last failure message, or empty.</summary>
    public string MessageText => messageText;

    /// <summary>Whether a failure message shows.</summary>
    public bool HasMessage => messageText.Length > 0;

    /// <summary>Whether the staged changes differ from the saved rules.</summary>
    public bool HasChanges => !Planned().SequenceEqual(stored);

    /// <summary>The first level of the tree.</summary>
    public ObservableCollection<FolderNodeViewModel> Roots { get; } = [];

    /// <summary>The accounts to choose from.</summary>
    public ObservableCollection<AccountChoiceViewModel> Accounts { get; } = [];

    /// <summary>The dialog heading.</summary>
    public string TitleText => Localization.GetLocal("FolderSelection.Title");

    /// <summary>The name of the account whose folders are shown.</summary>
    public string AccountNameText => accountName;

    /// <summary>The instruction above the account list.</summary>
    public string ChooseAccountText => Localization.GetLocal("FolderSelection.ChooseAccount");

    /// <summary>The text shown while the first level loads.</summary>
    public string LoadingText => Localization.GetLocal("FolderSelection.Loading");

    /// <summary>The Apply button's text.</summary>
    public string ApplyText => Localization.GetLocal("FolderSelection.Apply");

    /// <summary>The Cancel button's text.</summary>
    public string CancelText => Localization.GetLocal("FolderSelection.Cancel");

    /// <summary>The Retry button's text.</summary>
    public string RetryText => Localization.GetLocal("FolderSelection.Retry");

    /// <summary>The Back button's text.</summary>
    public string BackText => Localization.GetLocal("FolderSelection.Back");

    /// <summary>The confirmation heading.</summary>
    public string ConfirmTitleText => Localization.GetLocal("FolderSelection.Confirm.Title");

    /// <summary>The confirmation question: the folders stop syncing and the local copies are kept.</summary>
    public string ConfirmText => Step is FolderSelectionStep.ConfirmingUntick confirming ? Localization.GetLocal(confirming.Count == 1 ? "FolderSelection.Confirm.Prompt.One" : "FolderSelection.Confirm.Prompt.Many", confirming.Count.ToString("N0", Localization.CurrentCulture)) : string.Empty;

    /// <summary>The confirm button's text.</summary>
    public string ConfirmYesText => Localization.GetLocal("FolderSelection.Confirm.Yes");

    /// <summary>Saves the staged changes, after a confirmation when synced folders are unticked.</summary>
    public IAsyncRelayCommand ApplyCommand { get; }

    /// <summary>Saves the staged changes after the user confirmed.</summary>
    public IAsyncRelayCommand ConfirmCommand { get; }

    /// <summary>Goes back from the confirmation to the tree, keeping the staged changes.</summary>
    public IRelayCommand BackCommand { get; }

    /// <summary>Throws the staged changes away and closes the dialog.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Loads the first level again after a failure.</summary>
    public IAsyncRelayCommand RetryCommand { get; }

    /// <inheritdoc />
    public async Task ShowAsync(Option<AccountId> accountId, CancellationToken cancellationToken)
    {
        if (IsOpen)
            return;

        callerToken = cancellationToken;
        closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Reset();
        await accountId.Match(id => OpenTreeAsync(id, cancellationToken), () => ChooseAccountAsync(cancellationToken));
        await closed.Task.WaitAsync(cancellationToken);
    }

    internal Task<Result<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>> ListAsync(FolderTreePosition position, CancellationToken cancellationToken) => tree.GetChildrenAsync(account, position, cancellationToken);

    internal FolderNodeViewModel CreateNode(FolderTreeNode node)
    {
        var created = new FolderNodeViewModel(node, this, Localization);
        RefreshNode(created, Planned());

        return created;
    }

    internal void RefreshNode(FolderNodeViewModel node, IReadOnlyList<SelectionRule> planned) => node.Refresh(evaluator.Evaluate(node.Path, planned), SyncPairIdFactory.CreateForRule(account, node.Path), planned);

    internal string FailureText(FolderTreeFailure failure) => Localization.GetLocal(failure switch
    {
        FolderTreeFailure.RemoteFailed => "FolderSelection.Level.Failed.Remote",
        FolderTreeFailure.LocalFailed => "FolderSelection.Level.Failed.Local",
        FolderTreeFailure.BothFailed => "FolderSelection.Level.Failed.Both",
        _ => "FolderSelection.Level.Failed.Rules",
    });

    internal void Toggle(FolderNodeViewModel node)
    {
        if (!node.CanToggle)
            return;

        var wasTicked = evaluator.Evaluate(node.Path, Planned()) is SelectionState.Included;
        _ = staged.RemoveAll(change => IsInclusionAt(change, node.Path));
        var planned = Planned();
        var isTicked = evaluator.Evaluate(node.Path, planned) is SelectionState.Included;

        if (wasTicked && isTicked)
            staged.Add(SelectionChangeFactory.CreateExcludeFolder(node.Path, node.RemoteId));
        else if (!wasTicked && !isTicked)
            staged.Add(SelectionChangeFactory.CreateIncludeFolder(node.Path, node.RemoteId, evaluator.EnclosingDirection(node.Path, planned).Match(direction => direction, SyncDirectionFactory.CreateTwoWay)));

        StagedChanged();
    }

    internal void ChangeDirection(FolderNodeViewModel node)
    {
        if (evaluator.Evaluate(node.Path, Planned()) is not SelectionState.Included included || included.Pair != SyncPairIdFactory.CreateForRule(account, node.Path))
            return;

        staged.Add(SelectionChangeFactory.CreateChangeDirection(included.Pair, included.Direction switch
        {
            SyncDirection.TwoWay => SyncDirectionFactory.CreateUploadOnly(),
            SyncDirection.UploadOnly => SyncDirectionFactory.CreateDownloadOnly(),
            _ => SyncDirectionFactory.CreateTwoWay(),
        }));
        StagedChanged();
    }

    private static bool IsInclusionAt(SelectionChange change, LocalRelativePath path) => change switch
    {
        SelectionChange.IncludeFolder include => string.Equals(include.Path.Value, path.Value, StringComparison.OrdinalIgnoreCase),
        SelectionChange.ExcludeFolder exclude => string.Equals(exclude.Path.Value, path.Value, StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    private static bool IsInside(LocalRelativePath path, LocalRelativePath folder)
    {
        var inner = path.Value ?? string.Empty;
        var outer = folder.Value ?? string.Empty;

        return outer.Length == 0 || string.Equals(inner, outer, StringComparison.OrdinalIgnoreCase) || inner.StartsWith(outer + "/", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<SelectionRule> Planned() => SelectionRulePlanner.Apply(account, stored, staged);

    private void StagedChanged()
    {
        var planned = Planned();
        foreach (var root in Roots)
            RefreshNode(root, planned);

        OnPropertyChanged(nameof(HasChanges));
        ApplyCommand.NotifyCanExecuteChanged();
    }

    private void Reset()
    {
        staged.Clear();
        stored = [];
        pairs = [];
        Roots.Clear();
        Accounts.Clear();
        accountName = string.Empty;
        SetMessage(string.Empty);
        SetRootError(string.Empty);
    }

    private void SetStep(FolderSelectionStep step)
    {
        Step = step;
        OnPropertyChanged(nameof(Step));
        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(IsChoosingAccount));
        OnPropertyChanged(nameof(IsShowingTree));
        OnPropertyChanged(nameof(IsConfirming));
        OnPropertyChanged(nameof(IsSaving));
        OnPropertyChanged(nameof(ConfirmText));
        ApplyCommand.NotifyCanExecuteChanged();
    }

    private void SetMessage(string text)
    {
        messageText = text;
        OnPropertyChanged(nameof(MessageText));
        OnPropertyChanged(nameof(HasMessage));
    }

    private void SetRootError(string text)
    {
        rootErrorText = text;
        OnPropertyChanged(nameof(RootErrorText));
        OnPropertyChanged(nameof(HasRootError));
    }

    private void Close()
    {
        staged.Clear();
        Roots.Clear();
        Accounts.Clear();
        SetStep(FolderSelectionStepFactory.CreateClosed());
        _ = closed.TrySetResult();
    }

    private void Back()
    {
        if (Step is FolderSelectionStep.ConfirmingUntick)
            SetStep(FolderSelectionStepFactory.CreateTree());
    }

    private async Task ChooseAccountAsync(CancellationToken cancellationToken)
    {
        SetStep(FolderSelectionStepFactory.CreateChoosingAccount());
        var listed = await accounts.GetAllAsync(cancellationToken);
        _ = listed.Match(
            all =>
            {
                AddChoices(all);

                return Unit.Instance;
            },
            _ =>
            {
                SetMessage(Localization.GetLocal("FolderSelection.Failure.Accounts"));

                return Unit.Instance;
            });
    }

    private void AddChoices(IReadOnlyList<StoredAccount> all)
    {
        foreach (var choice in all)
            Accounts.Add(new AccountChoiceViewModel(choice.Registration.DisplayName, choice.Registration.Email, new AsyncRelayCommand(() => OpenTreeAsync(choice.Id, callerToken))));
    }

    private async Task OpenTreeAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        account = accountId;
        staged.Clear();
        Roots.Clear();
        SetMessage(string.Empty);
        SetRootError(string.Empty);
        SetStep(FolderSelectionStepFactory.CreateTree());
        accountName = (await accounts.GetAllAsync(cancellationToken)).Match(all => all.FirstOrNone(candidate => candidate.Id == accountId).Match(found => found.Registration.DisplayName, () => string.Empty), _ => string.Empty);
        OnPropertyChanged(nameof(AccountNameText));
        await LoadRootAsync(cancellationToken);
    }

    private async Task LoadRootAsync(CancellationToken cancellationToken)
    {
        isLoadingRoot = true;
        OnPropertyChanged(nameof(IsLoadingRoot));
        var readRules = await rules.GetAsync(account, cancellationToken);
        var readPairs = await pairReader.GetAsync(account, cancellationToken);
        var listed = await tree.GetChildrenAsync(account, FolderTreePositionFactory.CreateRoot(), cancellationToken);
        isLoadingRoot = false;
        OnPropertyChanged(nameof(IsLoadingRoot));

        SetRootError(readRules.Match(
            found =>
            {
                stored = found;

                return readPairs.Match(
                    foundPairs =>
                    {
                        pairs = foundPairs;

                        return listed.Match(AddRoots, FailureText);
                    },
                    error => FailureText(FolderTreeFailureFactory.CreateRulesFailed(error)));
            },
            error => FailureText(FolderTreeFailureFactory.CreateRulesFailed(error))));
        OnPropertyChanged(nameof(HasChanges));
    }

    private string AddRoots(IReadOnlyList<FolderTreeNode> nodes)
    {
        Roots.Clear();
        foreach (var node in nodes)
            Roots.Add(CreateNode(node));

        return string.Empty;
    }

    private Task RetryAsync(CancellationToken cancellationToken) => Step is FolderSelectionStep.Tree ? LoadRootAsync(cancellationToken) : Task.CompletedTask;

    private async Task ApplyAsync(CancellationToken cancellationToken)
    {
        if (Step is not FolderSelectionStep.Tree)
            return;

        var count = SyncedUnticks();
        if (count > 0)
        {
            SetStep(FolderSelectionStepFactory.CreateConfirmingUntick(count));

            return;
        }

        await SaveAsync(cancellationToken);
    }

    private async Task ConfirmAsync(CancellationToken cancellationToken)
    {
        if (Step is FolderSelectionStep.ConfirmingUntick)
            await SaveAsync(cancellationToken);
    }

    private int SyncedUnticks() =>
        staged.OfType<SelectionChange.ExcludeFolder>().Count(change => evaluator.Evaluate(change.Path, stored) is SelectionState.Included && pairs.Any(pair => pair.LastSync.Match(_ => true, () => false) && IsInside(change.Path, pair.LocalFolder)));

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        SetMessage(string.Empty);
        SetStep(FolderSelectionStepFactory.CreateSaving());
        var saved = await writer.ApplyAsync(account, [.. staged], cancellationToken);
        _ = saved.Match(
            _ =>
            {
                Close();

                return Unit.Instance;
            },
            _ =>
            {
                SetMessage(Localization.GetLocal("FolderSelection.Failure.Save"));
                SetStep(FolderSelectionStepFactory.CreateTree());

                return Unit.Instance;
            });
    }
}
