using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using SIT.Manager.Models.Tools;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SIT.Manager.ViewModels.Tools;

public partial class NetworkToolsViewModel(
    HttpClient httpClient,
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<NetworkToolsViewModel> logger)
    : ObservableObject
{
    private readonly ILogger<NetworkToolsViewModel> _logger = logger;
    private Symbol SuccessSymbol = Symbol.Accept;
    private Symbol FailSymbol = Symbol.Clear;

    [ObservableProperty] private PortCheckerResponse _portResponse = new();
    public CancellationTokenSource RequestCancellationSource = new();

    //There has got to be a better way to do this
    //Maybe this heat is fucking with my head?
    public Symbol AkiSymbol => PortResponse.AkiSuccess ? SuccessSymbol : FailSymbol;
    public Symbol NatSymbol => PortResponse.NatSuccess ? SuccessSymbol : FailSymbol;
    public Symbol RelaySymbol => PortResponse.RelaySuccess ? SuccessSymbol : FailSymbol;

    [RelayCommand]
    private async Task CheckPorts()
    {
        CancellationToken token = RequestCancellationSource.Token;
        ResiliencePipeline<HttpResponseMessage> pipeline =
            pipelineProvider.GetPipeline<HttpResponseMessage>("port-checker-pipeline");

        //This might be the wrong way to use polly pipelines
        //TODO: Do further reading on polly to see if I can improve this
        try
        {
            HttpResponseMessage reqResp = await pipeline.ExecuteAsync(async ct =>
            {
                HttpRequestMessage req = new(HttpMethod.Post, "/checkports");
                req.Content = JsonContent.Create(PortResponse.PortsUsed);
                return await httpClient.SendAsync(req, ct);
            }, token);
            
            switch (reqResp.StatusCode)
            {
                case HttpStatusCode.OK:
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
                case HttpStatusCode.ServiceUnavailable:
                    {
                        //TODO: Handle this. We've hit the rate limit
                        break;
                    }
                default:
                    {
                        //TODO: Logging here
                        return;
                    }
            }
        }
        catch (TaskCanceledException) { }
    }

    private void ProcessPortResponse(PortCheckerResponse response)
    {
        PortResponse = response;
        
        OnPropertyChanged(nameof(AkiSymbol));
        OnPropertyChanged(nameof(NatSymbol));
        OnPropertyChanged(nameof(RelaySymbol));
    }
}
