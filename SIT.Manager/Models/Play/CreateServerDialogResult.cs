using FluentAvalonia.UI.Controls;
using System;

namespace SIT.Manager.Models.Play;

public record CreateServerDialogResult(ContentDialogResult DialogResult,
    Uri ServerUri,
    string ServerNickname);
