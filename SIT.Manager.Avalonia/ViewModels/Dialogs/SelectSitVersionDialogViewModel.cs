using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using SIT.Manager.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SIT.Manager.Avalonia.ViewModels.Dialogs
{
    public partial class SelectSitVersionDialogViewModel : ViewModelBase
    {
        [GeneratedRegex("This version works with version [0]{1,}\\.[0-9]{1,2}\\.[0-9]{1,2}\\.[0-9]{1,2}\\.[0-9]{1,5}")]
        private static partial Regex ClientVersionRegex();

        private readonly HttpClient _httpClient;

        [ObservableProperty]
        private GithubRelease? _selectedRelease;

        [ObservableProperty]
        private bool _fetchedReleases;

        /// <summary>
        /// Gets the collection of loaded posts.
        /// </summary>
        public ObservableCollection<GithubRelease> GithubReleases { get; } = [];

        public SelectSitVersionDialogViewModel(HttpClient httpClient) {
            _httpClient = httpClient;

            RxApp.TaskpoolScheduler.Schedule(FetchReleases);
        }

        private async void FetchReleases() {
            GithubReleases.Clear();
            List<GithubRelease> githubReleases = [];

            try {
                string releasesJsonString = await _httpClient.GetStringAsync(@"https://api.github.com/repos/stayintarkov/StayInTarkov.Client/releases");
                githubReleases = JsonSerializer.Deserialize<List<GithubRelease>>(releasesJsonString) ?? [];

            }
            catch (Exception ex) {
                GithubReleases.Clear();
                // TODO Loggy.LogToFile("InstallSIT: " + ex.Message);
            }

            if (githubReleases.Count > 0) {
                foreach (GithubRelease release in githubReleases) {
                    Match match = ClientVersionRegex().Match(release.body);
                    if (match.Success) {
                        string releasePatch = match.Value.Replace("This version works with version ", "");
                        release.tag_name = $"{release.name} - Tarkov Version: {releasePatch}";
                        release.body = releasePatch;
                        GithubReleases.Add(release);
                    }
                    else {
                        // TODO Loggy.LogToFile("FetchReleases: There was a release without a version defined: " + release.html_url);
                    }
                }
            }
            else {
                // TODO Loggy.LogToFile("InstallSIT: githubReleases was 0 for official branch");
                return;
            }

            if (GithubReleases.Count > 0) {
                FetchedReleases = true;
                SelectedRelease = GithubReleases.First();
            }
        }
    }
}

