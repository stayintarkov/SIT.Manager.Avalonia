using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly IManagerConfigService _configService;
    private readonly IModService _modService;

    private ModInfo[] _unfilteredModList = [];

    [ObservableProperty]
    private bool _isModCompatibilityLayerInstalled = false;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    public ObservableCollection<ModInfo> _modList = [];

    public IAsyncRelayCommand InstallModCompatibilityLayerCommand { get; }

    public ModsPageViewModel(IManagerConfigService configService, IModService modService)
    {
        _configService = configService;
        _modService = modService;

        InstallModCompatibilityLayerCommand = new AsyncRelayCommand(InstallModCompatibilityLayer);
    }

    [RelayCommand]
    private void DisableMod(ModInfo mod)
    {
        // TODO
    }

    [RelayCommand]
    private void EnableMod(ModInfo mod)
    {
        // TODO
    }

    private async Task InstallModCompatibilityLayer()
    {
        await _modService.InstallModCompatLayer(_configService.Config.SitEftInstallPath);

        // Now that we have supposedly installed the mod compat layer check if it is right.
        IsModCompatibilityLayerInstalled = _modService.CheckModCompatibilityLayerInstalled(_configService.Config.SitEftInstallPath);
        if (IsModCompatibilityLayerInstalled)
        {
            // TODO show an alert that this is done so we aren't just hiding the notification
        }
        else
        {
            // TODO alert the user that the install has failed and to consult the log for details or just try again.
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
