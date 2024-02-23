using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SIT.Manager.Avalonia.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services
{
    public class PickerDialogService : IPickerDialogService
    {
        private readonly Window _target;

        public PickerDialogService(Window target) {
            _target = target;
        }

        public async Task<IStorageFolder?> GetDirectoryFromPickerAsync() {
            IReadOnlyList<IStorageFolder> folders = await _target.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions() {
                AllowMultiple = false
            });

            if (folders.Count != 0) {
                return folders[0];
            }
            return null;
        }

        public async Task<IStorageFile?> GetFileFromPickerAsync() {
            IReadOnlyList<IStorageFile> files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions() {
                AllowMultiple = false
            });

            if (files.Count != 0) {
                return files[0];
            }
            return null;
        }

        public async Task<IStorageFile?> GetFileSaveFromPickerAsync(string defaultFileExtension = "", string suggestedFileName = "") {
            IStorageFile? file = await _target.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions() {
                DefaultExtension = defaultFileExtension,
                SuggestedFileName = suggestedFileName
            });
            return file;
        }


    }
}
