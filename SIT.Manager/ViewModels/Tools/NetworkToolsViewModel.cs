using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;
using SIT.Manager.Controls;
using SIT.Manager.Models.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SIT.Manager.ViewModels.Tools;
public partial class NetworkToolsViewModel(
    HttpClient httpClient,
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<NetworkToolsViewModel> logger) : ObservableObject
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<NetworkToolsViewModel> _logger = logger;
    private CancellationTokenSource _cancellationTokenSource = new();

    [ObservableProperty]
    private Symbol _akiSymbol = Symbol.Help;
    [ObservableProperty]
    private Symbol _natSymbol = Symbol.Help;
    [ObservableProperty]
    private Symbol _relaySymbol = Symbol.Help;

    [ObservableProperty] private PortCheckerResponse _portResponse;

    [RelayCommand]
    private async Task CheckPorts()
    {
        AkiSymbol = Symbol.Help;
        NatSymbol = Symbol.Help;
        RelaySymbol = Symbol.Help;
        CancellationToken token = _cancellationTokenSource.Token;
        ResiliencePipeline<HttpResponseMessage> pipeline =
            pipelineProvider.GetPipeline<HttpResponseMessage>("port-checker-pipeline");

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

            switch (reqResp.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    {
                        string response = await reqResp.Content.ReadAsStringAsync(token);
                        PortCheckerResponse? respModel = JsonSerializer.Deserialize<PortCheckerResponse>(response);
                        if (respModel == null)
                        {
                            //TODO: Logging here
                            return;
                        }
                        ProcessPortResponse(respModel);
                        break;
                    }
                case System.Net.HttpStatusCode.ServiceUnavailable:
                    {
                        //TODO: Handle this. We've hit the rate limit
                        break;
                    }
                default:
                    break;
            }
        }
        catch (TaskCanceledException) { };
    }

    private void ProcessPortResponse(PortCheckerResponse response)
    {
        //TODO: set appropriate fields
        _portResponse = response;

        AkiSymbol = response.AkiSuccess ? Symbol.Accept : Symbol.Clear;
        NatSymbol = response.NatSuccess ? Symbol.Accept : Symbol.Clear;
        RelaySymbol = response.RelaySuccess ? Symbol.Accept : Symbol.Clear;
    }

    /*protected override async void OnDeactivated()
    {
        base.OnDeactivated();
        await _cancellationTokenSource.CancelAsync();
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }*/
}
