using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using SIT.Manager.Models.Tools;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.ViewModels.Tools;

public partial class NetworkToolsViewModel(HttpClient httpClient,
                                           ResiliencePipelineProvider<string> pipelineProvider,
                                           ILogger<NetworkToolsViewModel> logger) : ObservableRecipient
{
    private readonly ILogger<NetworkToolsViewModel> _logger = logger;

    private CancellationTokenSource _requestCancellationSource = new();

    [ObservableProperty]
    private bool _hasRunPortCheck = false;

    [ObservableProperty]
    private PortCheckerResponse _portResponse = new();

    [RelayCommand]
    private async Task CheckPorts()
    {
        CancellationToken token = _requestCancellationSource.Token;
        ResiliencePipeline<HttpResponseMessage> pipeline =
            pipelineProvider.GetPipeline<HttpResponseMessage>("port-checker-pipeline");

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
        HasRunPortCheck = true;
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
