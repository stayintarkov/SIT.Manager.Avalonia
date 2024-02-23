using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.ViewModels.Dialogs;
using System;
using System.Net.Http;

namespace SIT.Manager.Avalonia.Views.Dialogs
{
    public partial class SelectSitVersionDialog : ContentDialog, IStyleable
    {
        private readonly SelectSitVersionDialogViewModel dc;

        Type IStyleable.StyleKey => typeof(ContentDialog);

        public SelectSitVersionDialog() {
            dc = new SelectSitVersionDialogViewModel(App.Current.Services.GetService<HttpClient>());
            this.DataContext = dc;
            InitializeComponent();
        }

        public GithubRelease? GetSelectedGithubRelease() {
            return dc.SelectedRelease;
        }
    }
}
