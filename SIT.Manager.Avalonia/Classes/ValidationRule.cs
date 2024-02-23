using Avalonia.Input;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using Avalonia.Controls;

namespace SIT.Manager.Avalonia.Classes
{
    public class ValidationRule
    {
        public string Name { get; init; } = string.Empty;
        public Func<bool>? Check { get; init; } = null;
        public string ErrorMessage { get; init; } = string.Empty;
    }
}
