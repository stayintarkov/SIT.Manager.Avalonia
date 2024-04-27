using Polly;
using Polly.Registry;
using SIT.Manager.Exceptions;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    private static readonly byte[] zlibMagicBytes = new byte[] { 0x01, 0x5E, 0x9C, 0xDA };
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

        Memory<byte> magicNumber = new(new byte[2]);
        await respStream.ReadAsync(magicNumber, cancellationToken);
        respStream.Seek(0, SeekOrigin.Begin);

        if (magicNumber.Span[0] == 0x78 && zlibMagicBytes.Contains(magicNumber.Span[1]))
        {
            return await respStream.InflateAsync(cancellationToken);
        }
        else
        {
            return respStream as MemoryStream ?? new MemoryStream();
        }

    }

    private Task<MemoryStream> SendAsync(AkiServer server, string path, HttpMethod? method = null, string? data = null, ResiliencePipeline<HttpResponseMessage>? strategy = null, CancellationToken cancellationToken = default)
        => SendAsync(server.Address, path, method, data, strategy, cancellationToken);

    //TODO: Add error handling
    public async Task<AkiServer> GetAkiServerAsync(Uri serverAddresss, bool fetchInformation = true, CancellationToken cancellationToken = default)
    {
        AkiServer ret = new(serverAddresss);
        if (fetchInformation)
        {
            AkiServerInfo? serverInfo = await GetAkiServerInfoAsync(ret, cancellationToken);
            if(serverInfo != null)
            {
                ret.Name = serverInfo.Name;
            }
        }

        return ret;
    }

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
        return await JsonSerializer.DeserializeAsync<List<AkiMiniProfile>>(respStream, cancellationToken: cancellationToken) ?? [];
    }

    private string CreateLoginData(AkiCharacter character)
    {
        Version SITVersion = new(_configService.Config.SitVersion);
        string compatibleUri = character.ParentServer.Address.AbsoluteUri[..^(SITVersion >= standardUriFormatSupportedVersion ? 0 : 1)];
        JsonObject loginData = new()
        {
            ["username"] = character.Username,
            ["email"] = character.Username,
            ["edition"] = character.Edition,
            ["password"] = character.Password,
            ["backendUrl"] = compatibleUri
        };
        return loginData.ToJsonString();
    }

    private async Task<string> LoginOrRegisterAsync(AkiCharacter character, string operation, CancellationToken cancellationToken = default)
    {
        using (MemoryStream ms = await SendAsync(character.ParentServer, Path.Combine("/launcher/profile", operation), HttpMethod.Post, CreateLoginData(character), cancellationToken: cancellationToken))
        using (StreamReader streamReader = new StreamReader(ms))
        {
            return await streamReader.ReadToEndAsync(cancellationToken);
        }
    }

    public async Task<(string, AkiLoginStatus)> LoginAsync(AkiCharacter character, CancellationToken cancellationToken = default)
    {
        string resp = await LoginOrRegisterAsync(character, "login", cancellationToken);
        AkiLoginStatus status = resp.ToLowerInvariant() switch
        {
            "invalid_password" => AkiLoginStatus.IncorrectPassword,
            "failed" => AkiLoginStatus.AccountNotFound,
            _ => AkiLoginStatus.Success
        };
        return (resp, status);
    }

    public async Task<(string, AkiLoginStatus)> RegisterCharacterAsync(AkiCharacter character, CancellationToken cancellationToken = default)
    {
        string resp = await LoginOrRegisterAsync(character, "register", cancellationToken);
        AkiLoginStatus status = resp.ToLowerInvariant() switch
        {
            "ok" => AkiLoginStatus.Success,
            "failed" => AkiLoginStatus.UsernameTaken,
            _ => throw new Exception("Uh oh...")
        };

        return status == AkiLoginStatus.Success ? await LoginAsync(character, cancellationToken) : (string.Empty, status);
    }

    public async Task<AkiServerInfo?> GetAkiServerInfoAsync(AkiServer server, CancellationToken cancellationToken = default)
    {
        using MemoryStream respStream = await SendAsync(server.Address, "/launcher/server/connect", cancellationToken: cancellationToken);
        using (StreamReader sr = new(respStream))
        {
            return await JsonSerializer.DeserializeAsync<AkiServerInfo>(respStream, cancellationToken: cancellationToken);
        }
    }

    public Task<MemoryStream> GetAkiServerImage(AkiServer server, string assetPath, CancellationToken cancellationToken = default)
    {
        return SendAsync(server.Address, Path.Combine("/files/", assetPath), cancellationToken: cancellationToken);
    }
}

//TODO: Rename this and move it. This is more of a general request status
public enum AkiLoginStatus
{
    Success,
    AccountNotFound,
    UsernameTaken,
    IncorrectPassword
}
