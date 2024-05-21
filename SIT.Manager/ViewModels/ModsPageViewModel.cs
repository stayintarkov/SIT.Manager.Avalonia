using CommunityToolkit.Mvvm.ComponentModel;
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

    public ObservableCollection<ModInfo> ModList { get; } = [];

    protected override void OnActivated()
    {
        base.OnActivated();

        ModList.Clear();
        ModList.AddRange(_modService.GetInstalledMods());

        IsModCompatibilityLayerInstalled = _modService.CheckModCompatibilityLayerInstalled();
    }
}
