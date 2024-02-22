using System;

namespace SIT.Manager.Avalonia.Models
{
    public record PageNavigation(Type TargetPage, bool SuppressTransition = false);
}
