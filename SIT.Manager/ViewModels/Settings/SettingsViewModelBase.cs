using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Config;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Settings;

public partial class SettingsViewModelBase : ObservableRecipient
{
    protected readonly IManagerConfigService _configsService;
    protected readonly IPickerDialogService _pickerDialogService;

    [ObservableProperty]
    private ManagerConfig _config = new();

    protected SettingsViewModelBase(IManagerConfigService configService, IPickerDialogService pickerDialogService)
    {
        _configsService = configService;
        _pickerDialogService = pickerDialogService;
    }

    /// <summary>
    /// Gets the path containing the required filename based on the folder picker selection from a user
    /// </summary>
    /// <param name="filename">The filename to look for in the user specified directory</param>
    /// <returns>The path if the file exists, otherwise an empty string</returns>
    protected async Task<string> GetPathLocation(string filename)
    {
        IStorageFolder? directorySelected = await _pickerDialogService.GetDirectoryFromPickerAsync();
        if (directorySelected != null)
        {
            if (File.Exists(Path.Combine(directorySelected.Path.LocalPath, filename)))
            {
                return directorySelected.Path.LocalPath;
            }
        }
        return string.Empty;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        Config = _configsService.Config;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
    }
}
