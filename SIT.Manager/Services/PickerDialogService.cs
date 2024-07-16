using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SIT.Manager.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

public class PickerDialogService(Window target) : IPickerDialogService
{
    public async Task<IStorageFolder?> GetDirectoryFromPickerAsync()
    {
        IStorageFolder? ret = null;
        try
        {
            IReadOnlyList<IStorageFolder> folders = await target.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = false
            });

            ret = folders.FirstOrDefault();
        }
        catch (ArgumentException)
        {
            // The likely reason is the folder selected doesn't exist so we should just be able to return null as with other things.
        }

        return ret;
    }

    public async Task<IStorageFile?> GetFileFromPickerAsync()
    {
        IReadOnlyList<IStorageFile> files = await target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false
        });

        return files.Count != 0 ? files[0] : null;
    }

    public async Task<IStorageFile?> GetFileSaveFromPickerAsync(string defaultFileExtension = "", string suggestedFileName = "")
    {
        IStorageFile? file = await target.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = defaultFileExtension,
            SuggestedFileName = suggestedFileName
        });
        return file;
    }
}
