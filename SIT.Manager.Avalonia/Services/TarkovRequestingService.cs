using Polly;
using Polly.Registry;
using SIT.Manager.Avalonia.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services;
public class TarkovRequestingService(HttpClient httpClient, IZlibService zlibService, ResiliencePipelineProvider<string> resiliencePipelineProvider)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IZlibService _zlibService = zlibService;
    private readonly ResiliencePipelineProvider<string> resiliencePipeline = resiliencePipelineProvider;

    public async Task<Stream> GetAsync(Uri remoteAddress, string path, CancellationToken cancellationToken = default)
    {
        ResiliencePipeline<HttpResponseMessage> pipeline = resiliencePipeline.GetPipeline<HttpResponseMessage>("get-pipeline");
        UriBuilder endpoint = new(remoteAddress) { Path = path };
        HttpResponseMessage reqResp = await pipeline.ExecuteAsync(async token => await _httpClient.GetAsync(endpoint.Uri, token), cancellationToken);
        return await reqResp.Content.ReadAsStreamAsync(cancellationToken);
    }
}
