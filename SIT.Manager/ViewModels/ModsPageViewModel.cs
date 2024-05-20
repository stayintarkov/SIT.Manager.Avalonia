using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SIT.Manager.Interfaces;

namespace SIT.Manager.ViewModels;

public partial class ModsPageViewModel(ILogger<ModsPageViewModel> logger,
                                       IModService modService) : ObservableRecipient
{
    private readonly ILogger _logger = logger;
    private readonly IModService _modService = modService;
}
