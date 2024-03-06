using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views
{
    public partial class PlayPage : UserControl
    {
        public PlayPage()
        {
            InitializeComponent();
            this.DataContext = App.Current.Services.GetService<PlayPageViewModel>();
            if (DataContext is PlayPageViewModel dataContext)
            {
                dataContext.RegisterAddressBox(AddressBox);
                AddressBox.RevealPassword = !dataContext._configService.Config.HideIpAddress;
                AddressBox.LostFocus += (o, e) => { AddressBox.RevealPassword = !dataContext._configService.Config.HideIpAddress; };
            }
        }
    }
}