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
    private void DisableMod(ModInfo mod)
    {
        int modIndex = ModList.IndexOf(mod);
        ModInfo updatedModInfo = _modService.DisableMod(mod, _configService.Config.SitEftInstallPath);
        ModList[modIndex] = updatedModInfo;
    }

    [RelayCommand]
    private void EnableMod(ModInfo mod)
    {
        int modIndex = ModList.IndexOf(mod);
        ModInfo updatedModInfo = _modService.EnableMod(mod, _configService.Config.SitEftInstallPath);
        ModList[modIndex] = updatedModInfo;
    }

    private async Task InstallModCompatibilityLayer()
    {
        await _modService.InstallModCompatLayer(_configService.Config.SitEftInstallPath);

        // Now that we have supposedly installed the mod compat layer check if it is right.
        IsModCompatibilityLayerInstalled = _modService.CheckModCompatibilityLayerInstalled(_configService.Config.SitEftInstallPath);
        if (IsModCompatibilityLayerInstalled)
        {
            _barNotificationService.ShowError(_localizationService.TranslateSource("ModsPageViewModelModCompatInstallSuccessTitle"), _localizationService.TranslateSource("ModsPageViewModelModCompatInstallSuccessMessage"));
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

    protected override void OnActivated()
    {
        base.OnActivated();

        ModList.Clear();
        ModList.AddRange(_modService.GetInstalledMods(_configService.Config.SitEftInstallPath));

        IsModCompatibilityLayerInstalled = _modService.CheckModCompatibilityLayerInstalled(_configService.Config.SitEftInstallPath);
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchMods(value);
    }
}
