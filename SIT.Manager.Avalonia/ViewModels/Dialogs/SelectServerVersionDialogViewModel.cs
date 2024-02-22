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
    public partial class SelectServerVersionDialogViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient;

        [ObservableProperty]
        private GithubRelease? _selectedRelease;

        [ObservableProperty]
        private bool _fetchedReleases;

        /// <summary>
        /// Gets the collection of loaded posts.
        /// </summary>
        public ObservableCollection<GithubRelease> GithubReleases { get; } = [];

        public SelectServerVersionDialogViewModel(HttpClient httpClient) {
            _httpClient = httpClient;

            RxApp.TaskpoolScheduler.Schedule(FetchReleases);
        }

        private async void FetchReleases() {
            GithubReleases.Clear();
            List<GithubRelease> githubReleases = [];

            try {
                string releasesJsonString = await _httpClient.GetStringAsync(@"https://api.github.com/repos/stayintarkov/SIT.Aki-Server-Mod/releases");
                githubReleases = JsonSerializer.Deserialize<List<GithubRelease>>(releasesJsonString) ?? [];
            }
            catch (Exception ex) {
                GithubReleases.Clear();
                // TODO Loggy.LogToFile("Install Server: " + ex.Message);
            }

            if (githubReleases.Count > 0) {
                foreach (GithubRelease release in githubReleases) {
                    var zipAsset = release.assets.Find(asset => asset.name.EndsWith(".zip"));
                    if (zipAsset != null) {
                        Match match = ServerVersionRegex().Match(release.body);
                        if (match.Success) {
                            string releasePatch = match.Groups[1].Value;
                            release.tag_name = release.name + " - Tarkov Version: " + releasePatch;
                            release.body = releasePatch;
                            GithubReleases.Add(release);
                        }
                        else {
                            // TODO Loggy.LogToFile("FetchReleases: There was a release without a version defined: " + release.html_url);
                        }
                    }
                }
            }
            else {
                // TODO Loggy.LogToFile("Install Server: githubReleases was 0 for official branch");
            }

            if (GithubReleases.Count > 0) {
                FetchedReleases = true;
                SelectedRelease = GithubReleases.First();
            }
        }

        [GeneratedRegex("This server version works with EFT version ([0]{1,}\\.[0-9]{1,2}\\.[0-9]{1,2})\\.[0-9]{1,2}\\.[0-9]{1,5}")]
        private static partial Regex ServerVersionRegex();
    }
}
