using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models;
using System.Collections.ObjectModel;

namespace SIT.Manager.ViewModels;

public partial class ModsPageViewModel(ILogger<ModsPageViewModel> logger,
                                       IModService modService) : ObservableRecipient
{
    private readonly ILogger _logger = logger;
    private readonly IModService _modService = modService;

    public ObservableCollection<ModInfo> ModList { get; } = [];

    protected override void OnActivated()
    {
        base.OnActivated();

        ModList.Clear();
        ModList.AddRange(_modService.GetInstalledMods());
    }
}
