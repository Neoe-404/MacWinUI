using System.Globalization;
using System.Windows.Threading;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace MacWinUI.App.ViewModels;

public sealed class MenuBarViewModel(
    ISystemStatusService systemStatusService,
    IAudioService audioService,
    IActiveApplicationService activeApplicationService,
    DockAppearanceSettings appearanceSettings,
    ILogger<MenuBarViewModel> logger) : ObservableObject, IAsyncDisposable
{
    private bool _audioAvailable;
    private int? _batteryPercentage;
    private bool _isCharging;
    private bool _isNetworkAvailable;
    private bool _isMuted;
    private string _timeText = string.Empty;
    private double _volumePercent;
    private CancellationTokenSource? _cancellation;
    private Task? _updateTask;
    private string _activeApplicationName = "MacWinUI";

    public string ActiveApplicationName
    {
        get => _activeApplicationName;
        private set => SetProperty(ref _activeApplicationName, value);
    }

    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    public bool IsNetworkAvailable
    {
        get => _isNetworkAvailable;
        private set => SetProperty(ref _isNetworkAvailable, value);
    }

    public int? BatteryPercentage
    {
        get => _batteryPercentage;
        private set
        {
            if (SetProperty(ref _batteryPercentage, value))
            {
                OnPropertyChanged(nameof(HasBattery));
                OnPropertyChanged(nameof(BatteryText));
                OnPropertyChanged(nameof(BatteryFillWidth));
                OnPropertyChanged(nameof(BatteryVisible));
            }
        }
    }

    public bool IsCharging
    {
        get => _isCharging;
        private set
        {
            if (SetProperty(ref _isCharging, value))
            {
                OnPropertyChanged(nameof(BatteryText));
            }
        }
    }

    public bool HasBattery => BatteryPercentage.HasValue;

    public bool BatteryVisible => HasBattery && appearanceSettings.ShowBatteryStatus;

    public bool ShowNetworkStatus => appearanceSettings.ShowNetworkStatus;

    public bool ShowVolumeStatus => appearanceSettings.ShowVolumeStatus;

    public string BatteryText => BatteryPercentage is { } percentage
        ? $"{(IsCharging ? "⚡ " : string.Empty)}{percentage}%"
        : string.Empty;

    public double BatteryFillWidth => BatteryPercentage is { } percentage
        ? Math.Clamp(percentage, 0, 100) * 0.13
        : 0;

    public string VolumeGlyph => !AudioAvailable || IsMuted || VolumePercent <= 0
        ? "\uE74F"
        : VolumePercent switch
        {
            <= 33 => "\uE992",
            <= 66 => "\uE993",
            _ => "\uE994"
        };

    public string VolumeToolTip => AudioAvailable
        ? IsMuted
            ? "Sound · Muted"
            : $"Sound · {Math.Round(VolumePercent):0}%"
        : "Sound unavailable";

    private bool AudioAvailable
    {
        get => _audioAvailable;
        set
        {
            if (SetProperty(ref _audioAvailable, value))
            {
                OnPropertyChanged(nameof(VolumeGlyph));
                OnPropertyChanged(nameof(VolumeToolTip));
            }
        }
    }

    private bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (SetProperty(ref _isMuted, value))
            {
                OnPropertyChanged(nameof(VolumeGlyph));
                OnPropertyChanged(nameof(VolumeToolTip));
            }
        }
    }

    private double VolumePercent
    {
        get => _volumePercent;
        set
        {
            if (SetProperty(ref _volumePercent, value))
            {
                OnPropertyChanged(nameof(VolumeGlyph));
                OnPropertyChanged(nameof(VolumeToolTip));
            }
        }
    }

    public void Start(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (_cancellation is not null)
        {
            return;
        }

        _cancellation = new CancellationTokenSource();
        appearanceSettings.PropertyChanged += OnAppearanceSettingsChanged;
        _updateTask = RunAsync(dispatcher, _cancellation.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_cancellation is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        if (_updateTask is not null)
        {
            try
            {
                await _updateTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the application is closing.
            }
        }

        _cancellation.Dispose();
        appearanceSettings.PropertyChanged -= OnAppearanceSettingsChanged;
        _cancellation = null;
        _updateTask = null;
    }

    private async Task RunAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        await UpdateClockAsync(dispatcher, cancellationToken).ConfigureAwait(false);
        await RefreshAllStatusAsync(dispatcher, cancellationToken).ConfigureAwait(false);
        await RefreshActiveApplicationAsync(dispatcher, cancellationToken).ConfigureAwait(false);

        var elapsedSeconds = 0;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            elapsedSeconds++;
            await UpdateClockAsync(dispatcher, cancellationToken).ConfigureAwait(false);
            await RefreshActiveApplicationAsync(dispatcher, cancellationToken).ConfigureAwait(false);

            if (elapsedSeconds % 5 == 0)
            {
                await RefreshAllStatusAsync(dispatcher, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task UpdateClockAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var formattedTime = FormatCurrentTime();
        await dispatcher.InvokeAsync(
            () => TimeText = formattedTime,
            DispatcherPriority.DataBind,
            cancellationToken);
    }

    private string FormatCurrentTime() => DateTimeOffset.Now.ToString(
        appearanceSettings.Use24HourClock
            ? "ddd MMM d  HH:mm"
            : "ddd MMM d  h:mm tt",
        CultureInfo.CurrentCulture);

    private void OnAppearanceSettingsChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DockAppearanceSettings.Use24HourClock):
                TimeText = FormatCurrentTime();
                break;
            case nameof(DockAppearanceSettings.ShowNetworkStatus):
                OnPropertyChanged(nameof(ShowNetworkStatus));
                break;
            case nameof(DockAppearanceSettings.ShowVolumeStatus):
                OnPropertyChanged(nameof(ShowVolumeStatus));
                break;
            case nameof(DockAppearanceSettings.ShowBatteryStatus):
                OnPropertyChanged(nameof(BatteryVisible));
                break;
        }
    }

    private Task RefreshAllStatusAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken) =>
        Task.WhenAll(
            RefreshSystemStatusAsync(dispatcher, cancellationToken),
            RefreshAudioStatusAsync(dispatcher, cancellationToken));

    private async Task RefreshSystemStatusAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        SystemStatusSnapshot status;
        try
        {
            status = await systemStatusService
                .GetStatusAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not refresh MenuBar system status.");
            return;
        }

        await dispatcher.InvokeAsync(
            () =>
            {
                IsNetworkAvailable = status.IsNetworkAvailable;
                BatteryPercentage = status.BatteryPercentage;
                IsCharging = status.IsCharging;
            },
            DispatcherPriority.DataBind,
            cancellationToken);
    }

    private async Task RefreshAudioStatusAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        AudioState audioState;
        try
        {
            audioState = await audioService
                .GetStateAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not refresh MenuBar audio status.");
            return;
        }

        await dispatcher.InvokeAsync(
            () =>
            {
                AudioAvailable = audioState.IsAvailable;
                IsMuted = audioState.IsMuted;
                VolumePercent = audioState.VolumePercent;
            },
            DispatcherPriority.DataBind,
            cancellationToken);
    }

    private async Task RefreshActiveApplicationAsync(
        Dispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var applicationName = await activeApplicationService
            .GetActiveApplicationNameAsync(cancellationToken)
            .ConfigureAwait(false);
        await dispatcher.InvokeAsync(
            () => ActiveApplicationName = applicationName,
            DispatcherPriority.DataBind,
            cancellationToken);
    }
}
