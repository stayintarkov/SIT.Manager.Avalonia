﻿using Polly;
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
public class AkiServerRequestingService(HttpClient httpClient, ResiliencePipelineProvider<string> resiliencePipelineProvider)
{
    private static readonly MediaTypeHeaderValue _contentHeaderType = new("application/json");
    private readonly HttpClient _httpClient = httpClient;
    private readonly ResiliencePipelineProvider<string> resiliencePipelineProvider = resiliencePipelineProvider;

    private async Task<Stream> SendAsync(Uri remoteAddress, string path, HttpMethod? method = null, string? data = null, ResiliencePipeline<HttpResponseMessage>? resiliencePipeline = null, CancellationToken cancellationToken = default)
    {
        if (resiliencePipeline == null && !resiliencePipelineProvider.TryGetPipeline("default-pipeline", out resiliencePipeline))
            throw new ArgumentNullException(nameof(resiliencePipeline), "No default pipeline was specified and argument was null.");

        UriBuilder endpoint = new(remoteAddress) { Path = path };
        HttpRequestMessage req = new(method ?? HttpMethod.Get, endpoint.Uri);

        if (data != null)
        {
            using MemoryStream ms = new();
            using ZLibStream zlib = new(ms, CompressionLevel.Fastest, true);
            await zlib.WriteAsync(Encoding.UTF8.GetBytes(data), cancellationToken);
            await zlib.DisposeAsync();
            byte[] contentBytes = ms.ToArray();
            req.Content = new ByteArrayContent(contentBytes);
            req.Content.Headers.ContentType = _contentHeaderType;
            req.Content.Headers.ContentEncoding.Add("deflate");
            req.Content.Headers.Add("Content-Length", contentBytes.Length.ToString());
        }

        HttpResponseMessage reqResp = await resiliencePipeline.ExecuteAsync(async token => await _httpClient.SendAsync(req, token), cancellationToken);
        Stream respStream = await reqResp.Content.ReadAsStreamAsync(cancellationToken);
        return await respStream.InflateAsync(cancellationToken);
    }

    public async Task<int> PingAsync(AkiServer akiServer, CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();
        Stream resp = await SendAsync(akiServer.Address, "/launcher/ping", resiliencePipeline: resiliencePipelineProvider.GetPipeline<HttpResponseMessage>("ping-pipeline"), cancellationToken: cancellationToken);
        stopwatch.Stop();
        string serverRespStr = await resp.ReadAsStringAsync(cancellationToken: cancellationToken);
        return serverRespStr.Equals("\"pong!\"") ? Convert.ToInt32(stopwatch.ElapsedMilliseconds) : -1;
    }

    /*
    public async Task<bool> LoginAsync(AkiProfile profile, CancellationToken cancellationToken = default)
    {
        
        string sessionID = await SendAsync()
    }
    */
}
