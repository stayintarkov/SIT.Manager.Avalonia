using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

public partial class ModsPageViewModel : ObservableRecipient
{
    private readonly IBarNotificationService _barNotificationService;
    private readonly IManagerConfigService _configService;
    private readonly ILocalizationService _localizationService;
    private readonly IModService _modService;

    private ModInfo[] _unfilteredModList = [];

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isModCompatibilityLayerInstalled = false;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    public ObservableCollection<ModInfo> _modList = [];

    public IAsyncRelayCommand InstallModCompatibilityLayerCommand { get; }

    public ModsPageViewModel(IBarNotificationService barNotificationService,
                             IManagerConfigService configService,
                             ILocalizationService localizationService,
                             IModService modService)
    {
        _barNotificationService = barNotificationService;
        _configService = configService;
        _localizationService = localizationService;
        _modService = modService;

        InstallModCompatibilityLayerCommand = new AsyncRelayCommand(InstallModCompatibilityLayer);
    }

    [RelayCommand]
    private void ToggleModEnabled(ModInfo mod)
    {
        int modIndex = ModList.IndexOf(mod);

        // The toggle button which calls this already update the IsEnabled value to be
        // the action we want to call so just follow whatever that value is.
        ModInfo updatedModInfo;
        if (mod.IsEnabled)
        {
            updatedModInfo = _modService.EnableMod(mod, _configService.Config.SitEftInstallPath);
        }
        else
        {
            updatedModInfo = _modService.DisableMod(mod, _configService.Config.SitEftInstallPath);
        }
        ModList[modIndex] = updatedModInfo;
    }

    private async Task InstallModCompatibilityLayer()
    {
        await _modService.InstallModCompatLayer(_configService.Config.SitEftInstallPath);

        ModList.Clear();
        ModList.AddRange(_modService.GetInstalledMods(_configService.Config.SitEftInstallPath));

        // Now that we have supposedly installed the mod compat layer check if it is right.
        IsModCompatibilityLayerInstalled = _modService.CheckModCompatibilityLayerInstalled(_configService.Config.SitEftInstallPath);
        if (IsModCompatibilityLayerInstalled)
        {
            _barNotificationService.ShowSuccess(_localizationService.TranslateSource("ModsPageViewModelModCompatInstallSuccessTitle"), _localizationService.TranslateSource("ModsPageViewModelModCompatInstallSuccessMessage"));
        }
        else
        {
            await new ContentDialog()
            {
                Title = _localizationService.TranslateSource("ModsPageViewModelModCompatInstallErrorTitle"),
                Content = _localizationService.TranslateSource("ModsPageViewModelModCompatInstallErrorMessage"),
                PrimaryButtonText = _localizationService.TranslateSource("ModsPageViewModelModCompatInstallErrorButtonOk"),
            }.ShowAsync();
        }
    }

    [RelayCommand]
    private void SearchMods(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            if (_unfilteredModList.Length > 0)
            {
                ModList = new ObservableCollection<ModInfo>(_unfilteredModList);
                _unfilteredModList = [];
            }
            return;
        }

        if (_unfilteredModList.Length <= 0)
        {
            _unfilteredModList = new ModInfo[ModList.Count];
            ModList.CopyTo(_unfilteredModList, 0);
        }

        ModList = new(ModList.Where(x => x.Name.Contains(searchText)));
    }

    protected override async void OnActivated()
    {
        base.OnActivated();

        IsLoading = true;

        Task loadModsTask = Task.Run(async () =>
        {
            List<ModInfo> installedModsList = _modService.GetInstalledMods(_configService.Config.SitEftInstallPath);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ModList.Clear();
                ModList.AddRange(installedModsList);
            });
        });
        Task checkModCompatibilityLayerTask = Task.Run(async () =>
        {
            bool modCompatibilityLayerInstalled = _modService.CheckModCompatibilityLayerInstalled(_configService.Config.SitEftInstallPath);
            await Dispatcher.UIThread.InvokeAsync(() => IsModCompatibilityLayerInstalled = modCompatibilityLayerInstalled);
        });

        await Task.WhenAll(loadModsTask, checkModCompatibilityLayerTask, Task.Delay(3000));

        IsLoading = false;
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchMods(value);
    }
}
