using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Avalonia.Models;
using SIT.Manager.Avalonia.ViewModels.Dialogs;
using System;
using System.Net.Http;

namespace SIT.Manager.Avalonia.Controls
{
    public partial class SelectServerVersionDialog : ContentDialog, IStyleable
    {
        private readonly SelectServerVersionDialogViewModel dc;

        Type IStyleable.StyleKey => typeof(ContentDialog);

        public SelectServerVersionDialog() {
            dc = new SelectServerVersionDialogViewModel(App.Current.Services.GetService<HttpClient>());
            this.DataContext = dc;
            InitializeComponent();
        }

        public GithubRelease? GetSelectedGithubRelease() {
            return dc.SelectedRelease;
        }
    }
}