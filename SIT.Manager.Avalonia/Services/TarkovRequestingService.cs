using Polly;
using Polly.Registry;
using SIT.Manager.Avalonia.Classes;
using SIT.Manager.Avalonia.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Services;
public class TarkovRequestingService(HttpClient httpClient, ResiliencePipelineProvider<string> resiliencePipelineProvider)
{
    private static readonly MediaTypeHeaderValue _contentHeaderType = new("application/json");
    private readonly HttpClient _httpClient = httpClient;
    private readonly ResiliencePipelineProvider<string> resiliencePipeline = resiliencePipelineProvider;

    //TODO: Combine these to reduce duplication
    public async Task<Stream> GetAsync(Uri remoteAddress, string path, CancellationToken cancellationToken = default)
    {
        ResiliencePipeline<HttpResponseMessage> pipeline = resiliencePipeline.GetPipeline<HttpResponseMessage>("get-pipeline");
        UriBuilder endpoint = new(remoteAddress) { Path = path };
        HttpResponseMessage reqResp = await pipeline.ExecuteAsync(async token => await _httpClient.GetAsync(endpoint.Uri, token), cancellationToken);
        return await reqResp.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<Stream> PostAsync(Uri remoteAddress, string path, string data, CancellationToken cancellationToken = default)
    {
        ResiliencePipeline<HttpResponseMessage> pipeline = resiliencePipeline.GetPipeline<HttpResponseMessage>("get-pipeline");
        UriBuilder endpoint = new(remoteAddress) { Path = path };

        HttpRequestMessage req = new(HttpMethod.Post, endpoint.Uri);

        byte[] contentBytes = SimpleZlib.CompressToBytes(data, (int) ZlibCompression.BestSpeed);
        req.Content = new ByteArrayContent(contentBytes);
        req.Content.Headers.ContentType = _contentHeaderType;
        req.Content.Headers.ContentEncoding.Add("deflate");
        req.Content.Headers.Add("Content-Length", contentBytes.Length.ToString());

        HttpResponseMessage reqResp = await pipeline.ExecuteAsync(async token => await _httpClient.SendAsync(req, token), cancellationToken);
        return await reqResp.Content.ReadAsStreamAsync(cancellationToken);
    }
}

public enum ZlibCompression
{
    BestSpeed = 1,
    BestCompression = 9
}
