using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using MacWinUI.Core.Dock;
using MacWinUI.Core.Interfaces;
using MacWinUI.Core.Models;
using MacWinUI.Core.System;
using MacWinUI.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace MacWinUI.App.ViewModels;

public sealed class ControlCenterViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IAudioService _audioService;
    private readonly DockAppearanceSettings _appearanceSettings;
    private readonly DockViewModel _dockViewModel;
    private readonly ILogger<ControlCenterViewModel> _logger;
    private readonly ISystemSettingsLauncher _settingsLauncher;
    private readonly ISystemStatusService _systemStatusService;
    private bool _audioAvailable;
    private int? _batteryPercentage;
    private bool _isApplyingAudioState;
    private bool _isCharging;
    private bool _isMuted;
    private bool _isNetworkAvailable;
    private bool _isRefreshing;
    private double _volumePercent;
    private string _dockApplicationsStatusText = "Choose an application or file to keep it in the Dock.";
    private CancellationTokenSource? _volumeUpdateCancellation;
    private Task? _volumeUpdateTask;

    public ControlCenterViewModel(
        IAudioService audioService,
        ISystemSettingsLauncher settingsLauncher,
        ISystemStatusService systemStatusService,
        DockAppearanceSettings appearanceSettings,
        DockViewModel dockViewModel,
        ILogger<ControlCenterViewModel> logger)
    {
        _audioService = audioService;
        _settingsLauncher = settingsLauncher;
        _systemStatusService = systemStatusService;
        _appearanceSettings = appearanceSettings;
        _dockViewModel = dockViewModel;
        _logger = logger;

        ThemeOptions = Enum.GetValues<DockTheme>();
        DisplayModes =
        [
            new DockDisplayModeOption(DockDisplayMode.FollowCursor, "Follow cursor"),
            new DockDisplayModeOption(DockDisplayMode.Primary, "Primary")
        ];
        OpenSettingsCommand = new AsyncRelayCommand<SystemSettingsPage>(
            settingsLauncher.OpenAsync,
            exception => logger.LogWarning(exception, "Could not open Windows Settings."));
        ToggleMuteCommand = new AsyncRelayCommand<bool>(
            ToggleMuteAsync,
            exception => logger.LogWarning(exception, "Could not change the audio mute state."));
        RemoveDockApplicationCommand = new AsyncRelayCommand<DockItem>(
            RemoveDockApplicationAsync,
            exception => logger.LogWarning(exception, "Could not remove a custom Dock application."),
            item => item.IsCustom);
    }

    public IReadOnlyList<DockTheme> ThemeOptions { get; }

    public IReadOnlyList<DockDisplayModeOption> DisplayModes { get; }

    public ICommand OpenSettingsCommand { get; }

    public ICommand ToggleMuteCommand { get; }

    public ICommand RemoveDockApplicationCommand { get; }

    public ObservableCollection<DockItem> CustomDockItems => _dockViewModel.CustomItems;

    public string DockApplicationsStatusText
    {
        get => _dockApplicationsStatusText;
        private set => SetProperty(ref _dockApplicationsStatusText, value);
    }

    public async Task AddDockApplicationAsync(
        string executablePath,
        CancellationToken cancellationToken = default)
    {
        var result = await _dockViewModel.AddCustomApplicationAsync(
            executablePath,
            cancellationToken);
        DockApplicationsStatusText = result switch
        {
            AddDockApplicationResult.Added =>
                $"{Path.GetFileName(executablePath)} was added to the Dock.",
            AddDockApplicationResult.AlreadyPinned =>
                "That application is already in the Dock.",
            _ => "Choose an existing application or file."
        };
    }

    public bool IsNetworkAvailable
    {
        get => _isNetworkAvailable;
        private set
        {
            if (SetProperty(ref _isNetworkAvailable, value))
            {
                OnPropertyChanged(nameof(NetworkStatusText));
            }
        }
    }

    public string NetworkStatusText => IsNetworkAvailable ? "Connected" : "Not connected";

    public int? BatteryPercentage
    {
        get => _batteryPercentage;
        private set
        {
            if (SetProperty(ref _batteryPercentage, value))
            {
                OnPropertyChanged(nameof(HasBattery));
                OnPropertyChanged(nameof(BatteryStatusText));
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
                OnPropertyChanged(nameof(BatteryStatusText));
            }
        }
    }

    public bool HasBattery => BatteryPercentage.HasValue;

    public string BatteryStatusText => BatteryPercentage is { } percentage
        ? $"{percentage}%{(IsCharging ? " · Charging" : string.Empty)}"
        : "No battery";

    public bool AudioAvailable
    {
        get => _audioAvailable;
        private set => SetProperty(ref _audioAvailable, value);
    }

    public double VolumePercent
    {
        get => _volumePercent;
        set
        {
            var normalized = AudioVolume.NormalizePercent(value);
            if (!SetProperty(ref _volumePercent, normalized))
            {
                return;
            }

            OnPropertyChanged(nameof(VolumeText));
            if (!_isApplyingAudioState && AudioAvailable)
            {
                QueueVolumeUpdate(normalized);
            }
        }
    }

    public string VolumeText => AudioAvailable
        ? $"{Math.Round(VolumePercent):0}%"
        : "Unavailable";

    public bool IsMuted
    {
        get => _isMuted;
        private set
        {
            if (SetProperty(ref _isMuted, value))
            {
                OnPropertyChanged(nameof(MuteGlyph));
            }
        }
    }

    public string MuteGlyph => IsMuted ? "\uE74F" : "\uE767";

    public DockTheme SelectedTheme
    {
        get => _appearanceSettings.Theme;
        set
        {
            if (_appearanceSettings.Theme == value)
            {
                return;
            }

            _appearanceSettings.Theme = value;
            OnPropertyChanged();
        }
    }

    public double DockIconSize
    {
        get => _appearanceSettings.IconSize;
        set
        {
            if (Math.Abs(_appearanceSettings.IconSize - value) < 0.01)
            {
                return;
            }

            _appearanceSettings.IconSize = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DockIconSizeText));
        }
    }

    public string DockIconSizeText => $"{Math.Round(DockIconSize):0} pt";

    public double DockOpacityPercent
    {
        get => _appearanceSettings.Opacity * 100;
        set
        {
            var opacity = value / 100;
            if (Math.Abs(_appearanceSettings.Opacity - opacity) < 0.001)
            {
                return;
            }

            _appearanceSettings.Opacity = opacity;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DockOpacityText));
        }
    }

    public string DockOpacityText => $"{Math.Round(DockOpacityPercent):0}%";

    public bool EnableMagnification
    {
        get => _appearanceSettings.EnableMagnification;
        set
        {
            if (_appearanceSettings.EnableMagnification == value)
            {
                return;
            }

            _appearanceSettings.EnableMagnification = value;
            OnPropertyChanged();
        }
    }

    public bool AutoHideDock
    {
        get => _appearanceSettings.AutoHideDock;
        set
        {
            if (_appearanceSettings.AutoHideDock == value)
            {
                return;
            }

            _appearanceSettings.AutoHideDock = value;
            OnPropertyChanged();
        }
    }

    public bool EnableBlur
    {
        get => _appearanceSettings.EnableBlur;
        set
        {
            if (_appearanceSettings.EnableBlur == value)
            {
                return;
            }

            _appearanceSettings.EnableBlur = value;
            OnPropertyChanged();
        }
    }

    public double MaterialIntensityPercent
    {
        get => _appearanceSettings.MaterialIntensity * 100;
        set
        {
            var intensity = value / 100;
            if (Math.Abs(_appearanceSettings.MaterialIntensity - intensity) < 0.001)
            {
                return;
            }

            _appearanceSettings.MaterialIntensity = intensity;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MaterialIntensityText));
        }
    }

    public string MaterialIntensityText =>
        $"{Math.Round(MaterialIntensityPercent):0}%";

    public bool ReduceMotion
    {
        get => _appearanceSettings.ReduceMotion;
        set
        {
            if (_appearanceSettings.ReduceMotion == value)
            {
                return;
            }

            _appearanceSettings.ReduceMotion = value;
            OnPropertyChanged();
        }
    }

    public DockDisplayModeOption SelectedDisplayMode
    {
        get => DisplayModes.First(option => option.Value == _appearanceSettings.DisplayMode);
        set
        {
            if (value is null || _appearanceSettings.DisplayMode == value.Value)
            {
                return;
            }

            _appearanceSettings.DisplayMode = value.Value;
            OnPropertyChanged();
        }
    }

    public bool Use24HourClock
    {
        get => _appearanceSettings.Use24HourClock;
        set
        {
            if (_appearanceSettings.Use24HourClock == value)
            {
                return;
            }

            _appearanceSettings.Use24HourClock = value;
            OnPropertyChanged();
        }
    }

    public bool ShowNetworkStatus
    {
        get => _appearanceSettings.ShowNetworkStatus;
        set
        {
            if (_appearanceSettings.ShowNetworkStatus == value)
            {
                return;
            }

            _appearanceSettings.ShowNetworkStatus = value;
            OnPropertyChanged();
        }
    }

    public bool ShowVolumeStatus
    {
        get => _appearanceSettings.ShowVolumeStatus;
        set
        {
            if (_appearanceSettings.ShowVolumeStatus == value)
            {
                return;
            }

            _appearanceSettings.ShowVolumeStatus = value;
            OnPropertyChanged();
        }
    }

    public bool ShowBatteryStatus
    {
        get => _appearanceSettings.ShowBatteryStatus;
        set
        {
            if (_appearanceSettings.ShowBatteryStatus == value)
            {
                return;
            }

            _appearanceSettings.ShowBatteryStatus = value;
            OnPropertyChanged();
        }
    }

    public bool ReserveMenuBarSpace
    {
        get => _appearanceSettings.ReserveMenuBarSpace;
        set
        {
            if (_appearanceSettings.ReserveMenuBarSpace == value)
            {
                return;
            }

            _appearanceSettings.ReserveMenuBarSpace = value;
            OnPropertyChanged();
        }
    }

    public void NotifyAppearancePreferencesChanged()
    {
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(DockIconSize));
        OnPropertyChanged(nameof(DockIconSizeText));
        OnPropertyChanged(nameof(DockOpacityPercent));
        OnPropertyChanged(nameof(DockOpacityText));
        OnPropertyChanged(nameof(EnableMagnification));
        OnPropertyChanged(nameof(AutoHideDock));
        OnPropertyChanged(nameof(EnableBlur));
        OnPropertyChanged(nameof(MaterialIntensityPercent));
        OnPropertyChanged(nameof(MaterialIntensityText));
        OnPropertyChanged(nameof(ReduceMotion));
        OnPropertyChanged(nameof(SelectedDisplayMode));
        OnPropertyChanged(nameof(Use24HourClock));
        OnPropertyChanged(nameof(ShowNetworkStatus));
        OnPropertyChanged(nameof(ShowVolumeStatus));
        OnPropertyChanged(nameof(ShowBatteryStatus));
        OnPropertyChanged(nameof(ReserveMenuBarSpace));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            var systemStatusTask = _systemStatusService.GetStatusAsync(cancellationToken);
            var audioStateTask = _audioService.GetStateAsync(cancellationToken);
            await Task.WhenAll(systemStatusTask, audioStateTask);

            var status = await systemStatusTask;
            IsNetworkAvailable = status.IsNetworkAvailable;
            BatteryPercentage = status.BatteryPercentage;
            IsCharging = status.IsCharging;

            ApplyAudioState(await audioStateTask);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not refresh Control Center state.");
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _volumeUpdateCancellation?.Cancel();
        if (_volumeUpdateTask is not null)
        {
            try
            {
                await _volumeUpdateTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when a newer slider value supersedes an older one.
            }
        }

        _volumeUpdateCancellation?.Dispose();
        _volumeUpdateCancellation = null;
        _volumeUpdateTask = null;
    }

    private void ApplyAudioState(AudioState audioState)
    {
        _isApplyingAudioState = true;
        try
        {
            AudioAvailable = audioState.IsAvailable;
            VolumePercent = audioState.VolumePercent;
            IsMuted = audioState.IsMuted;
        }
        finally
        {
            _isApplyingAudioState = false;
        }
    }

    private async Task RemoveDockApplicationAsync(
        DockItem item,
        CancellationToken cancellationToken)
    {
        await _dockViewModel.RemoveCustomApplicationAsync(item, cancellationToken);
        DockApplicationsStatusText = $"{item.DisplayName} was removed from the Dock.";
    }

    private void QueueVolumeUpdate(double volumePercent)
    {
        _volumeUpdateCancellation?.Cancel();

        var cancellation = new CancellationTokenSource();
        _volumeUpdateCancellation = cancellation;
        _volumeUpdateTask = ApplyVolumeAsync(
            volumePercent,
            cancellation,
            cancellation.Token);
    }

    private async Task ApplyVolumeAsync(
        double volumePercent,
        CancellationTokenSource cancellation,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(45, cancellationToken).ConfigureAwait(false);
            await _audioService
                .SetVolumeAsync(volumePercent, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected while the user is moving the slider.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not apply the Control Center volume value.");
        }
        finally
        {
            cancellation.Dispose();
            if (ReferenceEquals(_volumeUpdateCancellation, cancellation))
            {
                _volumeUpdateCancellation = null;
            }
        }
    }

    private async Task ToggleMuteAsync(
        bool currentlyMuted,
        CancellationToken cancellationToken)
    {
        await _audioService
            .SetMutedAsync(!currentlyMuted, cancellationToken);
        ApplyAudioState(
            await _audioService
                .GetStateAsync(cancellationToken));
    }
}

public sealed record DockDisplayModeOption(
    DockDisplayMode Value,
    string Label)
{
    public override string ToString() => Label;
}
