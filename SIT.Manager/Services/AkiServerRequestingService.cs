using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Registry;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.ManagedProcess;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Services;
public class AkiServerRequestingService(
    HttpClient httpClient, 
    ResiliencePipelineProvider<string> resiliencePipelineProvider,
    IManagerConfigService configService) : IAkiServerRequestingService
{
    private static readonly MediaTypeHeaderValue _contentHeaderType = new("application/json");
    private static readonly Version standardUriFormatSupportedVersion = new Version("1.10.8827.30098");
    private readonly HttpClient _httpClient = httpClient;
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider = resiliencePipelineProvider;
    private readonly IManagerConfigService _configService = configService;

    private async Task<MemoryStream> SendAsync(Uri remoteAddress, string path, HttpMethod? method = null, string? data = null, ResiliencePipeline<HttpResponseMessage>? strategy = null, CancellationToken cancellationToken = default)
    {
        if (strategy == null && !_resiliencePipelineProvider.TryGetPipeline("default-pipeline", out strategy))
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
        reqResp.EnsureSuccessStatusCode();
        Stream respStream = await reqResp.Content.ReadAsStreamAsync(cancellationToken);
        return await respStream.InflateAsync(cancellationToken);
    }

    private Task<MemoryStream> SendAsync(AkiServer server, string path, HttpMethod? method = null, string? data = null, ResiliencePipeline<HttpResponseMessage>? strategy = null, CancellationToken cancellationToken = default)
        => SendAsync(server.Address, path, method, data, strategy, cancellationToken);

    //TODO: Add error handling
    public async Task<AkiServer> GetAkiServerAsync(Uri serverAddresss, bool fetchInformation = true)
    {
        AkiServer ret;
        if (fetchInformation)
        {
            var strategy = _resiliencePipelineProvider.GetPipeline<HttpResponseMessage>("ping-pipeline");
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

    public Task<AkiServer> GetAkiServerAsync(AkiServer server, bool fetchInformation = true)
        => GetAkiServerAsync(server.Address, fetchInformation);

    public async Task<int> GetPingAsync(AkiServer akiServer, CancellationToken cancellationToken = default)
    {
        var strategy = _resiliencePipelineProvider.GetPipeline<HttpResponseMessage>("ping-pipeline");
        Stopwatch stopwatch = Stopwatch.StartNew();
        using MemoryStream respStream = await SendAsync(akiServer, "/launcher/ping", strategy: strategy, cancellationToken: cancellationToken);
        stopwatch.Stop();

        using (StreamReader streamReader = new(respStream))
        {
            if (streamReader.ReadToEnd().Equals("\"pong!\"", StringComparison.InvariantCultureIgnoreCase))
                return Convert.ToInt32(stopwatch.ElapsedMilliseconds);
            else
                return -1;
        }
    }

    public async Task<List<AkiMiniProfile>> GetMiniProfilesAsync(AkiServer server, CancellationToken cancellationToken = default)
    {
        using MemoryStream respStream = await SendAsync(server, "/launcher/profiles", cancellationToken: cancellationToken);
        using StreamReader streamReader = new(respStream);
        return JsonConvert.DeserializeObject<List<AkiMiniProfile>>(await streamReader.ReadToEndAsync(cancellationToken)) ?? [];
    }

    public async Task<string> LoginAsync(AkiCharacter character)
    {
        Version SITVersion = new(_configService.Config.SitVersion);
        string compatibleUri = character.ParentServer.Address.AbsoluteUri[..^(SITVersion >= standardUriFormatSupportedVersion ? 0 : 1)];
        JsonObject loginData = new()
        {
            ["username"]    = character.Username,
            ["email"]       = character.Username,
            ["edition"]     = character.Edition,
            ["password"]    = character.Password,
            ["backendUrl"]  = compatibleUri
        };

        using (MemoryStream ms = await SendAsync(character.ParentServer, "/launcher/profile/login", HttpMethod.Post, loginData.ToJsonString()))
        using (StreamReader streamReader = new StreamReader(ms))
        {
            return streamReader.ReadToEnd();
        }
    }
}
