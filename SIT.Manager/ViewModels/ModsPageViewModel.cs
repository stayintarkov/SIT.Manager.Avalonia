using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using System.Collections.ObjectModel;

namespace SIT.Manager.ViewModels;

public partial class ModsPageViewModel(IModService modService) : ObservableRecipient
{
    private readonly IModService _modService = modService;

    [ObservableProperty]
    private bool _isModCompatibilityLayerInstalled = false;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<ModInfo> ModList { get; } = [];

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

    [RelayCommand]
    private void InstallModCompatibilityLayer()
    {
        // TODO
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
        ModList.AddRange(_modService.GetInstalledMods());

        IsModCompatibilityLayerInstalled = _modService.CheckModCompatibilityLayerInstalled();
    }
}
