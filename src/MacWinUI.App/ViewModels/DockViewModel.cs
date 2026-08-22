using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using MacWinUI.Core.Accessibility;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace MacWinUI.App.ViewModels;

public sealed class DockViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IAccessibilityPreferencesService _accessibilityPreferencesService;
    private readonly IApplicationLauncher _applicationLauncher;
    private readonly IApplicationActivityService _applicationActivityService;
    private readonly IIconService _iconService;
    private readonly ILogger<DockViewModel> _logger;
    private readonly IPinnedDockApplicationsStore _pinnedApplicationsStore;
    private readonly IReadOnlyList<DockItem> _defaultItems;
    private readonly Dictionary<string, DockItem> _defaultItemsById;
    private readonly HashSet<string> _hiddenDefaultItemIds = new(StringComparer.Ordinal);
    private IReadOnlyList<string> _savedItemOrder = [];
    private readonly Dictionary<string, PinnedDockApplication> _pinnedApplications =
        new(StringComparer.Ordinal);
    private Dispatcher? _dispatcher;
    private Task? _iconLoadingTask;
    private Task? _pinnedApplicationsLoadingTask;
    private CancellationTokenSource? _monitoringCancellation;
    private Task? _monitoringTask;

    public DockViewModel(
        IApplicationLauncher applicationLauncher,
        IApplicationActivityService applicationActivityService,
        IIconService iconService,
        IDockItemProvider dockItemProvider,
        IPinnedDockApplicationsStore pinnedApplicationsStore,
        IAccessibilityPreferencesService accessibilityPreferencesService,
        DockAppearanceSettings appearance,
        ILogger<DockViewModel> logger)
    {
        _applicationActivityService = applicationActivityService;
        _applicationLauncher = applicationLauncher;
        _iconService = iconService;
        _pinnedApplicationsStore = pinnedApplicationsStore;
        _accessibilityPreferencesService = accessibilityPreferencesService;
        _logger = logger;

        Items = [];
        ApplicationItems = [];
        SystemItems = [];
        CustomItems = [];
        _defaultItems = dockItemProvider.GetDefaultItems().ToArray();
        _defaultItemsById = _defaultItems.ToDictionary(
            item => item.Id,
            StringComparer.Ordinal);
        foreach (var item in _defaultItems)
        {
            Items.Add(item);
            if (item.Id is "windows-settings")
            {
                SystemItems.Add(item);
            }
            else
            {
                ApplicationItems.Add(item);
            }
        }
        Appearance = appearance;
        LaunchItemCommand = new AsyncRelayCommand<DockItem>(
            applicationLauncher.ActivateOrLaunchAsync,
            exception => logger.LogError(
                exception,
                "Unexpected error while executing a dock launch command."));
    }

    public ObservableCollection<DockItem> Items { get; }

    public ObservableCollection<DockItem> ApplicationItems { get; }

    public ObservableCollection<DockItem> SystemItems { get; }

    public ObservableCollection<DockItem> CustomItems { get; }

    public DockAppearanceSettings Appearance { get; }

    public ICommand LaunchItemCommand { get; }

    public bool EffectiveReduceMotion =>
        AccessibilityBehavior.ShouldReduceMotion(
            Appearance.ReduceMotion,
            _accessibilityPreferencesService.GetCurrent());

    public bool HasHiddenDefaultItems => _hiddenDefaultItemIds.Count > 0;

    public void Start(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (_monitoringCancellation is not null)
        {
            return;
        }

        _monitoringCancellation = new CancellationTokenSource();
        _dispatcher = dispatcher;
        Appearance.PropertyChanged += OnAppearancePropertyChanged;
        _accessibilityPreferencesService.PreferencesChanged += OnAccessibilityPreferencesChanged;
        _iconLoadingTask = LoadIconsAsync(
            dispatcher,
            _monitoringCancellation.Token);
        _pinnedApplicationsLoadingTask = LoadPinnedApplicationsAsync(
            dispatcher,
            _monitoringCancellation.Token);
        _monitoringTask = MonitorActivityAsync(
            dispatcher,
            _monitoringCancellation.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_monitoringCancellation is null)
        {
            return;
        }

        await _monitoringCancellation.CancelAsync();

        var backgroundTasks = new[]
            {
                _iconLoadingTask,
                _pinnedApplicationsLoadingTask,
                _monitoringTask
            }
            .OfType<Task>()
            .ToArray();
        if (backgroundTasks.Length > 0)
        {
            try
            {
                await Task.WhenAll(backgroundTasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the application is closing.
            }
        }

        _monitoringCancellation.Dispose();
        Appearance.PropertyChanged -= OnAppearancePropertyChanged;
        _accessibilityPreferencesService.PreferencesChanged -= OnAccessibilityPreferencesChanged;
        _monitoringCancellation = null;
        _iconLoadingTask = null;
        _pinnedApplicationsLoadingTask = null;
        _monitoringTask = null;
        _dispatcher = null;
    }

    public async Task<AddDockApplicationResult> AddCustomApplicationAsync(
        string executablePath,
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null
            || string.IsNullOrWhiteSpace(executablePath)
            || !File.Exists(executablePath))
        {
            return AddDockApplicationResult.Invalid;
        }

        if (_pinnedApplicationsLoadingTask is not null)
        {
            await _pinnedApplicationsLoadingTask.WaitAsync(cancellationToken);
        }

        PinnedDockApplication application;
        try
        {
            application = PinnedDockApplication.Create(executablePath);
        }
        catch (ArgumentException)
        {
            return AddDockApplicationResult.Invalid;
        }

        var item = application.CreateDockItem();
        var hiddenDefaultItem = _defaultItems.FirstOrDefault(defaultItem =>
            PathsEqual(defaultItem.LaunchTarget, application.ExecutablePath)
            || PathsEqual(defaultItem.IconSourcePath, application.ExecutablePath));
        if (hiddenDefaultItem is not null
            && _hiddenDefaultItemIds.Contains(hiddenDefaultItem.Id))
        {
            await _dispatcher.InvokeAsync(
                () =>
                {
                    _hiddenDefaultItemIds.Remove(hiddenDefaultItem.Id);
                    RebuildVisibleCollections();
                },
                DispatcherPriority.DataBind,
                cancellationToken);
            await SavePinnedApplicationsAsync(cancellationToken).ConfigureAwait(false);
            return AddDockApplicationResult.Added;
        }

        var added = await _dispatcher.InvokeAsync(
            () =>
            {
                if (Items.Any(existing =>
                        PathsEqual(existing.LaunchTarget, application.ExecutablePath)
                        || PathsEqual(existing.IconSourcePath, application.ExecutablePath)))
                {
                    return false;
                }

                _pinnedApplications[item.Id] = application;
                Items.Add(item);
                ApplicationItems.Add(item);
                CustomItems.Add(item);
                return true;
            },
            DispatcherPriority.DataBind,
            cancellationToken);

        if (!added)
        {
            return AddDockApplicationResult.AlreadyPinned;
        }

        await Task.WhenAll(
            LoadIconAsync(item, _dispatcher, cancellationToken),
            SavePinnedApplicationsAsync(cancellationToken));
        return AddDockApplicationResult.Added;
    }

    public async Task<int> AddDroppedItemsAsync(
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var addedCount = 0;
        foreach (var path in paths
                     .Where(File.Exists)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await AddCustomApplicationAsync(path, cancellationToken)
                is AddDockApplicationResult.Added)
            {
                addedCount++;
            }
        }

        return addedCount;
    }

    public Task OpenDroppedFilesAsync(
        DockItem application,
        IReadOnlyList<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(filePaths);

        var existingFiles = filePaths
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return application.AcceptsFileDrops && existingFiles.Length > 0
            ? _applicationLauncher.LaunchWithFilesAsync(
                application,
                existingFiles,
                cancellationToken)
            : Task.CompletedTask;
    }

    public Task LaunchItemAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _applicationLauncher.ActivateOrLaunchAsync(item, cancellationToken);
    }

    public Task OpenContainingFolderAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _applicationLauncher.OpenContainingFolderAsync(item, cancellationToken);
    }

    public Task<IReadOnlyList<ApplicationWindowInfo>> GetOpenWindowsAsync(
        DockItem item,
        CancellationToken cancellationToken = default) =>
        _applicationLauncher.GetOpenWindowsAsync(item, cancellationToken);

    public Task ActivateWindowAsync(
        nint windowHandle,
        CancellationToken cancellationToken = default) =>
        _applicationLauncher.ActivateWindowAsync(windowHandle, cancellationToken);

    public async Task RemoveCustomApplicationAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.IsCustom)
        {
            return;
        }

        await RemoveDockItemAsync(item, cancellationToken);
    }

    public async Task RemoveDockItemAsync(
        DockItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_dispatcher is null)
        {
            return;
        }

        if (_pinnedApplicationsLoadingTask is not null)
        {
            await _pinnedApplicationsLoadingTask.WaitAsync(cancellationToken);
        }

        var removed = await _dispatcher.InvokeAsync(
            () =>
            {
                if (item.IsCustom)
                {
                    if (!_pinnedApplications.Remove(item.Id))
                    {
                        return false;
                    }

                    CustomItems.Remove(item);
                }
                else if (_defaultItemsById.ContainsKey(item.Id))
                {
                    _hiddenDefaultItemIds.Add(item.Id);
                }
                else
                {
                    return false;
                }

                Items.Remove(item);
                ApplicationItems.Remove(item);
                SystemItems.Remove(item);
                return true;
            },
            DispatcherPriority.DataBind,
            cancellationToken);

        if (removed)
        {
            await SavePinnedApplicationsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RestoreDefaultItemsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
        {
            return;
        }

        if (_pinnedApplicationsLoadingTask is not null)
        {
            await _pinnedApplicationsLoadingTask.WaitAsync(cancellationToken);
        }

        var restored = await _dispatcher.InvokeAsync(
            () =>
            {
                if (_hiddenDefaultItemIds.Count == 0)
                {
                    return false;
                }

                _hiddenDefaultItemIds.Clear();
                RebuildVisibleCollections();
                return true;
            },
            DispatcherPriority.DataBind,
            cancellationToken);
        if (restored)
        {
            await SavePinnedApplicationsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task MoveDockItemAsync(
        string sourceItemId,
        DockItem targetItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceItemId);
        ArgumentNullException.ThrowIfNull(targetItem);
        if (_dispatcher is null)
        {
            return;
        }

        var moved = await _dispatcher.InvokeAsync(
            () =>
            {
                var source = ApplicationItems.FirstOrDefault(item => item.Id == sourceItemId);
                var targetIndex = ApplicationItems.IndexOf(targetItem);
                if (source is null || targetIndex < 0 || ReferenceEquals(source, targetItem))
                {
                    return false;
                }

                var sourceIndex = ApplicationItems.IndexOf(source);
                ApplicationItems.Move(sourceIndex, targetIndex);
                RebuildActivityItems();
                return true;
            },
            DispatcherPriority.DataBind,
            cancellationToken);
        if (moved)
        {
            await SavePinnedApplicationsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<PinnedDockApplicationsSnapshot> CreatePinnedSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        if (_dispatcher is null)
        {
            return new PinnedDockApplicationsSnapshot();
        }

        return await _dispatcher.InvokeAsync(
            CreatePinnedSnapshot,
            DispatcherPriority.DataBind,
            cancellationToken);
    }

    public async Task ApplyPinnedSnapshotAsync(
        PinnedDockApplicationsSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (_dispatcher is null)
        {
            return;
        }

        var importedApplications = snapshot.Applications
            .Where(application => File.Exists(application.ExecutablePath))
            .Select(application => PinnedDockApplication.Create(
                application.ExecutablePath,
                application.DisplayName))
            .ToArray();
        var importedItems = importedApplications
            .Select(application => application.CreateDockItem())
            .ToArray();

        await _dispatcher.InvokeAsync(
            () =>
            {
                _pinnedApplications.Clear();
                CustomItems.Clear();
                _hiddenDefaultItemIds.Clear();
                foreach (var id in snapshot.HiddenDefaultItemIds)
                {
                    if (_defaultItemsById.ContainsKey(id))
                    {
                        _hiddenDefaultItemIds.Add(id);
                    }
                }

                foreach (var pair in importedApplications.Zip(importedItems))
                {
                    _pinnedApplications[pair.Second.Id] = pair.First;
                    CustomItems.Add(pair.Second);
                }

                _savedItemOrder = snapshot.ItemOrder;
                RebuildVisibleCollections();
            },
            DispatcherPriority.DataBind,
            cancellationToken);

        await Task.WhenAll(importedItems.Select(item =>
            LoadIconAsync(item, _dispatcher, cancellationToken)));
        await SavePinnedApplicationsAsync(cancellationToken).ConfigureAwait(false);
    }

    private void OnAppearancePropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DockAppearanceSettings.ReduceMotion))
        {
            OnPropertyChanged(nameof(EffectiveReduceMotion));
        }
    }

    private void OnAccessibilityPreferencesChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(EffectiveReduceMotion));
    }

    private Task LoadIconsAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            Items.Select(item => LoadIconAsync(item, dispatcher, cancellationToken)));
    }

    private async Task LoadPinnedApplicationsAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var snapshot = await _pinnedApplicationsStore
            .LoadAsync(cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            return;
        }

        await dispatcher.InvokeAsync(
            () =>
            {
                foreach (var itemId in snapshot.HiddenDefaultItemIds)
                {
                    if (_defaultItemsById.ContainsKey(itemId))
                    {
                        _hiddenDefaultItemIds.Add(itemId);
                    }
                }

                RebuildVisibleCollections();
            },
            DispatcherPriority.DataBind,
            cancellationToken);

        var itemsToLoad = new List<DockItem>();
        foreach (var savedApplication in snapshot.Applications)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var application = PinnedDockApplication.Create(
                    savedApplication.ExecutablePath,
                    savedApplication.DisplayName);
                if (!File.Exists(application.ExecutablePath))
                {
                    _logger.LogInformation(
                        "Pinned Dock application {ExecutablePath} no longer exists and was skipped.",
                        application.ExecutablePath);
                    continue;
                }

                var item = application.CreateDockItem();
                var added = await dispatcher.InvokeAsync(
                    () =>
                    {
                        if (Items.Any(existing =>
                                string.Equals(
                                    existing.LaunchTarget,
                                    application.ExecutablePath,
                                    StringComparison.OrdinalIgnoreCase)))
                        {
                            return false;
                        }

                        _pinnedApplications[item.Id] = application;
                        Items.Add(item);
                        ApplicationItems.Add(item);
                        CustomItems.Add(item);
                        return true;
                    },
                    DispatcherPriority.DataBind,
                    cancellationToken);
                if (added)
                {
                    itemsToLoad.Add(item);
                }
            }
            catch (Exception exception) when (
                exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
            {
                _logger.LogWarning(
                    exception,
                    "A saved pinned Dock application entry was invalid and was skipped.");
            }
        }

        await Task.WhenAll(
            itemsToLoad.Select(item => LoadIconAsync(item, dispatcher, cancellationToken)));

        _savedItemOrder = snapshot.ItemOrder;
        await dispatcher.InvokeAsync(
            ApplySavedItemOrder,
            DispatcherPriority.DataBind,
            cancellationToken);
    }

    private Task SavePinnedApplicationsAsync(CancellationToken cancellationToken)
    {
        _savedItemOrder = ApplicationItems
            .Concat(SystemItems)
            .Select(item => item.Id)
            .ToArray();
        return _pinnedApplicationsStore.SaveAsync(
            CreatePinnedSnapshot(),
            cancellationToken);
    }

    private PinnedDockApplicationsSnapshot CreatePinnedSnapshot()
    {
        return new PinnedDockApplicationsSnapshot
        {
            Applications = CustomItems
                .Select(item => _pinnedApplications[item.Id])
                .ToArray(),
            HiddenDefaultItemIds = _hiddenDefaultItemIds
                .Order(StringComparer.Ordinal)
                .ToArray(),
            ItemOrder = _savedItemOrder
        };
    }

    private void RebuildVisibleCollections()
    {
        Items.Clear();
        ApplicationItems.Clear();
        SystemItems.Clear();

        foreach (var defaultItem in _defaultItems)
        {
            if (_hiddenDefaultItemIds.Contains(defaultItem.Id))
            {
                continue;
            }

            Items.Add(defaultItem);
            if (defaultItem.Id is "windows-settings")
            {
                SystemItems.Add(defaultItem);
            }
            else
            {
                ApplicationItems.Add(defaultItem);
            }
        }

        foreach (var customItem in CustomItems)
        {
            Items.Add(customItem);
            ApplicationItems.Add(customItem);
        }

        ApplySavedItemOrder();
    }

    private void ApplySavedItemOrder()
    {
        if (_savedItemOrder.Count == 0)
        {
            RebuildActivityItems();
            return;
        }

        var orderedItems = _savedItemOrder
            .Select(id => ApplicationItems.FirstOrDefault(item => item.Id == id))
            .OfType<DockItem>()
            .Concat(ApplicationItems.Where(item => !_savedItemOrder.Contains(item.Id)))
            .Distinct()
            .ToArray();
        ApplicationItems.Clear();
        foreach (var item in orderedItems)
        {
            ApplicationItems.Add(item);
        }

        RebuildActivityItems();
    }

    private void RebuildActivityItems()
    {
        Items.Clear();
        foreach (var item in ApplicationItems.Concat(SystemItems))
        {
            Items.Add(item);
        }
    }

    private static bool PathsEqual(string? first, string? second)
    {
        return !string.IsNullOrWhiteSpace(first)
            && !string.IsNullOrWhiteSpace(second)
            && string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadIconAsync(
        DockItem item,
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        try
        {
            var iconPng = await _iconService
                .GetIconPngAsync(item, cancellationToken)
                .ConfigureAwait(false);
            if (iconPng is null)
            {
                return;
            }

            await dispatcher.InvokeAsync(
                () => item.IconPngData = iconPng,
                DispatcherPriority.DataBind,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not load the icon for dock item {DockItemId}.",
                item.Id);
        }
    }

    private async Task MonitorActivityAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        await RefreshActivityAsync(dispatcher, cancellationToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await RefreshActivityAsync(dispatcher, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshActivityAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, ApplicationActivityState> activity;

        try
        {
            var itemSnapshot = await dispatcher.InvokeAsync(
                () => (IReadOnlyList<DockItem>)Items.ToArray(),
                DispatcherPriority.DataBind,
                cancellationToken);
            activity = await _applicationActivityService
                .GetActivityAsync(itemSnapshot, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not refresh Dock application activity.");
            return;
        }

        await dispatcher.InvokeAsync(
            () => ApplyActivity(activity),
            DispatcherPriority.DataBind,
            cancellationToken);
    }

    private void ApplyActivity(
        IReadOnlyDictionary<string, ApplicationActivityState> activity)
    {
        foreach (var item in Items)
        {
            var state = activity.GetValueOrDefault(item.Id);
            item.RunningInstanceCount = state.RunningInstanceCount;
            item.IsRunning = state.RunningInstanceCount > 0;
            item.IsActive = state.IsActive;
        }
    }
}

public enum AddDockApplicationResult
{
    Added,
    AlreadyPinned,
    Invalid
}
