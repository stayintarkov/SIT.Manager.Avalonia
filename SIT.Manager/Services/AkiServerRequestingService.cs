using Newtonsoft.Json.Linq;
using Polly;
using Polly.Registry;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services;
public class AkiServerRequestingService(HttpClient httpClient, ResiliencePipelineProvider<string> resiliencePipelineProvider) : IAkiServerRequestingService
{
    private static readonly MediaTypeHeaderValue _contentHeaderType = new("application/json");
    private readonly HttpClient _httpClient = httpClient;
    private readonly ResiliencePipelineProvider<string> resiliencePipelineProvider = resiliencePipelineProvider;

    private async Task<MemoryStream> SendAsync(Uri remoteAddress, string path, HttpMethod? method = null, string? data = null, ResiliencePipeline<HttpResponseMessage>? strategy = null, CancellationToken cancellationToken = default)
    {
        if (strategy == null && !resiliencePipelineProvider.TryGetPipeline("default-pipeline", out strategy))
            throw new ArgumentNullException(nameof(strategy), "No default pipeline was specified and argument was null.");

        UriBuilder endpoint = new(remoteAddress) { Path = path };

        HttpResponseMessage reqResp = await strategy.ExecuteAsync(async (CancellationToken ct) =>
        {
            HttpRequestMessage req = new(method ?? HttpMethod.Get, endpoint.Uri);

            if (data != null)
            {
                using MemoryStream ms = new();
                using ZLibStream zlib = new(ms, CompressionLevel.Fastest, true);
                await zlib.WriteAsync(Encoding.UTF8.GetBytes(data), ct);
                await zlib.DisposeAsync();
                byte[] contentBytes = ms.ToArray();
                req.Content = new ByteArrayContent(contentBytes);
                req.Content.Headers.ContentType = _contentHeaderType;
                req.Content.Headers.ContentEncoding.Add("deflate");
                req.Content.Headers.Add("Content-Length", contentBytes.Length.ToString());
            }

            return await _httpClient.SendAsync(req, cancellationToken: ct);
        }, cancellationToken);
        Stream respStream = await reqResp.Content.ReadAsStreamAsync(cancellationToken);
        return await respStream.InflateAsync(cancellationToken);
    }
    //TODO: Add error handling
    public async Task<AkiServer> GetAkiServerAsync(Uri serverAddresss, bool fetchInformation = true)
    {
        AkiServer ret;
        if (fetchInformation)
        {
            var strategy = resiliencePipelineProvider.GetPipeline<HttpResponseMessage>("ping-pipeline");
            using MemoryStream respStream = await SendAsync(serverAddresss, "/launcher/server/connect", strategy: strategy);
            string json;
            using (StreamReader sr = new(respStream)) { json = sr.ReadToEnd(); }
            JObject connectInfo = JObject.Parse(json);
            string? serverName = (string?) connectInfo.GetValue("name");
            ret = new(serverAddresss)
            {
                Name = serverName ?? string.Empty
            };
        }
        else
            ret = new(serverAddresss);

        return ret;
    }
}
