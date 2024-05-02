using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using SIT.Manager.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Tools;
public partial class NetworkToolsViewModel(HttpClient httpClient, ResiliencePipelineProvider<string> pipelineProvider, ILogger<NetworkToolsViewModel> logger) : ActivatableUserControl
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<NetworkToolsViewModel> _logger = logger;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider = pipelineProvider;
    private CancellationTokenSource _cancellationTokenSource = new();

    [RelayCommand]
    private async Task CheckPortsAsync()
    {
        CancellationToken token = _cancellationTokenSource.Token;
        ResiliencePipeline<HttpResponseMessage> pipeline = _pipelineProvider.GetPipeline<HttpResponseMessage>("PortCheckerPipeline");

        //This might be the wrong way to use polly pipelines
        //TODO: Do further reading on polly to see if I can improve this
        try
        {
            //TODO: Have this load the ports from the configuration files so that custom ports work
            HttpResponseMessage reqResp = await pipeline.ExecuteAsync(async (CancellationToken ct) =>
            {
                HttpRequestMessage req = new(HttpMethod.Post, "/checkports");
                return await _httpClient.SendAsync(req, ct);
            }, token);

            if(reqResp.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                //TODO: Handle this. We've hit the rate limit
                return;
            }
            else if(reqResp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string response = await reqResp.Content.ReadAsStringAsync();
                
            }
        }
        catch (TaskCanceledException) { };
    }

    protected override async void OnDeactivated()
    {
        base.OnDeactivated();
        await _cancellationTokenSource.CancelAsync();
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new();
    }
}
