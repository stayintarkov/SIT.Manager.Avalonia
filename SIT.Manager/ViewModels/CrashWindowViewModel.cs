using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels;
public partial class CrashWindowViewModel
{
    [RelayCommand]
    private void ExitLauncher()
    {
        Environment.Exit(-1);
    }
}
