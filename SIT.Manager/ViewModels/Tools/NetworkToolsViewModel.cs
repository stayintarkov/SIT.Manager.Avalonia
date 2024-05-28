using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using SIT.Manager.Models.Tools;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Tools;

public partial class NetworkToolsViewModel : ObservableRecipient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NetworkToolsViewModel> _logger;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;

    private CancellationTokenSource _requestCancellationSource = new();

    [ObservableProperty]
    private bool _checkLocalServer = true;

    [ObservableProperty]
    private bool _hasRunPortCheck = false;

    [ObservableProperty]
    private string _externalServerIP = string.Empty;

    [ObservableProperty]
    private PortCheckerResponse _portResponse = new();

    public IAsyncRelayCommand CheckPortsCommand { get; }

    public NetworkToolsViewModel(HttpClient httpClient,
                                 ResiliencePipelineProvider<string> pipelineProvider,
                                 ILogger<NetworkToolsViewModel> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pipelineProvider = pipelineProvider;

        CheckPortsCommand = new AsyncRelayCommand(CheckPorts);
    }

    private async Task ExternalServerPortCheck(CancellationToken token)
    {
        // TODO
    }

    private async Task LocalServerPortCheck(CancellationToken token)
    {
        ResiliencePipeline<HttpResponseMessage> pipeline = _pipelineProvider.GetPipeline<HttpResponseMessage>("port-checker-pipeline");

        //This might be the wrong way to use polly pipelines
        //TODO: Do further reading on polly to see if I can improve this
        try
        {
            HttpResponseMessage reqResp = await pipeline.ExecuteAsync(async ct =>
            {
                HttpRequestMessage req = new(HttpMethod.Post, "/checkports")
                {
                    Content = JsonContent.Create(PortResponse.PortsUsed)
                };
                return await _httpClient.SendAsync(req, ct).ConfigureAwait(false);
            }, token);

            switch (reqResp.StatusCode)
            {
                case HttpStatusCode.OK:
                    {
                        string response = await reqResp.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                        PortCheckerResponse? respModel = JsonSerializer.Deserialize<PortCheckerResponse>(response);
                        if (respModel == null)
                        {
                            //TODO: Logging here
                            return;
                        }

                        await ProcessPortResponse(respModel);
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

    private async Task CheckPorts()
    {
        CancellationToken token = _requestCancellationSource.Token;
        if (CheckLocalServer)
        {
            await LocalServerPortCheck(token);
        }
        else
        {
            await ExternalServerPortCheck(token);
        }
    }

    private async Task ProcessPortResponse(PortCheckerResponse response)
    {
        PortResponse = response;
        HasRunPortCheck = true;

        // We had a successfull check so just put an artificial wait here to
        // force a slight delay on users spamming the button to check their ports
        await Task.Delay(Random.Shared.Next(2500));
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _requestCancellationSource = new CancellationTokenSource();
        HasRunPortCheck = false;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        _requestCancellationSource.Cancel();
        _requestCancellationSource.Dispose();
    }
}
