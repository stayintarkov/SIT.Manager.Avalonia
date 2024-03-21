using Microsoft.Extensions.Logging;
using SIT.Manager.Avalonia.Interfaces;
using SIT.Manager.Avalonia.ManagedProcess;
using SIT.Manager.Avalonia.Models;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services;

public class AppUpdaterService(ILogger<AppUpdaterService> logger, HttpClient httpClient, IManagerConfigService managerConfigService) : IAppUpdaterService
{
    private const string MANAGER_VERSION_URL = @"https://api.github.com/repos/stayintarkov/SIT.Manager.Avalonia/releases/latest";

    private readonly ILogger<AppUpdaterService> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly IManagerConfigService _managerConfigService = managerConfigService;

    public async Task<bool> CheckForUpdate()
    {
        if (_managerConfigService.Config.LookForUpdates)
        {
            try
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0");

                string versionJsonString = await _httpClient.GetStringAsync(MANAGER_VERSION_URL);
                GithubRelease? latestRelease = JsonSerializer.Deserialize<GithubRelease>(versionJsonString);
                if (latestRelease != null)
                {
                    Version latestVersion = new(latestRelease.name);
                    return latestVersion.CompareTo(currentVersion) > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckForUpdate");
            }
        }
        return false;
    }

    public Task Update()
    {
        throw new NotImplementedException();
    }
}
