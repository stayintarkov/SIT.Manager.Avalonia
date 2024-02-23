using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Interfaces
{
    public interface IPickerDialogService
    {
        /// <summary>
        /// Get a single directory from the user using the directory picker dialog
        /// </summary>
        /// <returns>IStorageFolder the user selected or null</returns>
        Task<IStorageFolder?> GetDirectoryFromPickerAsync();
        /// <summary>
        /// Get a single file from the user using the file open picker dialog
        /// </summary>
        /// <returns>IStorageFile the user selected or null</returns>
        Task<IStorageFile?> GetFileFromPickerAsync();
        /// <summary>
        /// Get a file from the user using the save file picker dialog
        /// </summary>
        /// <returns>IStorageFile the user selected or null</returns>
        Task<IStorageFile?> GetFileSaveFromPickerAsync(string defaultFileExtension = "", string suggestedFileName = "");
    }
}
