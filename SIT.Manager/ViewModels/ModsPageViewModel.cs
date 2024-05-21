using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;

public partial class ModsPageViewModel : ObservableRecipient
{
    private readonly IManagerConfigService _configService;
    private readonly IModService _modService;

    [ObservableProperty]
    private bool _isModCompatibilityLayerInstalled = false;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public IAsyncRelayCommand InstallModCompatibilityLayerCommand { get; }

    public ObservableCollection<ModInfo> ModList { get; } = [];

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
    }

    [RelayCommand]
    private void SearchMods()
    {
        // TODO
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        ModList.Clear();
        ModList.AddRange(_modService.GetInstalledMods(_configService.Config.SitEftInstallPath));

        IsModCompatibilityLayerInstalled = _modService.CheckModCompatibilityLayerInstalled(_configService.Config.SitEftInstallPath);
    }
}
