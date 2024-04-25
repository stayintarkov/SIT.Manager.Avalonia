using FluentAvalonia.UI.Controls;

namespace SIT.Manager.Models.Play;

public record CreateCharacterDialogResult(ContentDialogResult DialogResult,
    string Username,
    string Password,
    bool SaveLogin,
    TarkovEdition TarkovEdition);
