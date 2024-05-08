using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SIT.Manager.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Services;

public class PickerDialogService(Window target) : IPickerDialogService
{
    private readonly Window _target = target;

    public async Task<IStorageFolder?> GetDirectoryFromPickerAsync()
    {
        try
        {
            IReadOnlyList<IStorageFolder> folders = await _target.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = false
            });

            if (folders.Count != 0)
            {
                return folders[0];
            }
        }
        catch (ArgumentException)
        {
            // The likely reason is the folder selected doesn't exist so we should just be able to return null as with other things.
        }
        return null;
    }

    public async Task<IStorageFile?> GetFileFromPickerAsync()
    {
        IReadOnlyList<IStorageFile> files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = false
        });

        if (files.Count != 0)
        {
            return files[0];
        }
        return null;
    }

    public async Task<IStorageFile?> GetFileSaveFromPickerAsync(string defaultFileExtension = "", string suggestedFileName = "")
    {
        IStorageFile? file = await _target.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            DefaultExtension = defaultFileExtension,
            SuggestedFileName = suggestedFileName
        });
        return file;
    }


}
