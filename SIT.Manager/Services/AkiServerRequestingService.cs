using Polly;
using Polly.Registry;
using SIT.Manager.Extentions;
using SIT.Manager.Interfaces;
using SIT.Manager.Models.Aki;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    ResiliencePipelineProvider<string> resiliencePipelineProvider) : IAkiServerRequestingService
{
    private static readonly MediaTypeHeaderValue ContentHeaderType = new("application/json");
    private static readonly ImmutableArray<byte> ZlibMagicBytes = [0x01, 0x5E, 0x9C, 0xDA];

    private async Task<MemoryStream> SendAsync(Uri remoteAddress, string path, HttpMethod? method = null, string? data = null, ResiliencePipeline<HttpResponseMessage>? strategy = null, CancellationToken cancellationToken = default)
    {
        if (strategy == null && !resiliencePipelineProvider.TryGetPipeline("default-pipeline", out strategy))
            throw new ArgumentNullException(nameof(strategy), "No default pipeline was specified and argument was null.");

        UriBuilder endpoint = new(remoteAddress) { Path = path };

        HttpResponseMessage reqResp = await strategy.ExecuteAsync(static async (state, ct) =>
        {
            HttpRequestMessage req = new(state.method ?? HttpMethod.Get, state.endpoint.Uri);

            if (state.data != null)
            {
                using MemoryStream ms = new();
                await using ZLibStream zlib = new(ms, CompressionLevel.Fastest, true);
                await zlib.WriteAsync(Encoding.UTF8.GetBytes(state.data), ct);
                await zlib.FlushAsync(ct);
                byte[] contentBytes = ms.ToArray();
                req.Content = new ByteArrayContent(contentBytes);
                req.Content.Headers.ContentType = ContentHeaderType;
                req.Content.Headers.ContentEncoding.Add("deflate");
                req.Content.Headers.Add("Content-Length", contentBytes.Length.ToString());
            }

            return await state.httpClient.SendAsync(req, cancellationToken: ct);
        },(httpClient, method, endpoint, data), cancellationToken);
        reqResp.EnsureSuccessStatusCode();
        Stream respStream = await reqResp.Content.ReadAsStreamAsync(cancellationToken);

        Memory<byte> headerMagicNumber = new byte[2];
        _ = await respStream.ReadAsync(headerMagicNumber, cancellationToken);
        respStream.Seek(0, SeekOrigin.Begin);

        bool streamIsZlibCompressed = headerMagicNumber.Span[0] == 0x78 && ZlibMagicBytes.Contains(headerMagicNumber.Span[1]);
        MemoryStream ret = streamIsZlibCompressed ? await respStream.InflateAsync(cancellationToken) : respStream as MemoryStream ?? new MemoryStream();
        return ret;
    }

    private Task<MemoryStream> SendAsync(AkiServer server, string path, HttpMethod? method = null, string? data = null, ResiliencePipeline<HttpResponseMessage>? strategy = null, CancellationToken cancellationToken = default)
        => SendAsync(server.Address, path, method, data, strategy, cancellationToken);
    
    public async Task<AkiServer> GetAkiServerAsync(Uri serverAddress, bool fetchInformation = true, CancellationToken cancellationToken = default)
    {
        AkiServer ret = new(serverAddress);
        if (!fetchInformation) return ret;

        AkiServerInfo? serverInfo = await GetAkiServerInfoAsync(ret, cancellationToken);
        if (serverInfo != null) ret.Name = serverInfo.Name;

        return ret;
    }

    //TODO: Convert this to int? instead of returning -1
    public async Task<int> GetPingAsync(AkiServer akiServer, CancellationToken cancellationToken = default)
    {
        ResiliencePipeline<HttpResponseMessage> strategy = resiliencePipelineProvider.GetPipeline<HttpResponseMessage>("ping-pipeline");
        Stopwatch stopwatch = Stopwatch.StartNew();
        using MemoryStream respStream = await SendAsync(akiServer, "/launcher/ping", strategy: strategy, cancellationToken: cancellationToken);
        stopwatch.Stop();

        using StreamReader streamReader = new(respStream);
        int retPing = -1;
        if ((await streamReader.ReadToEndAsync(cancellationToken)).Equals("\"pong!\"", StringComparison.InvariantCultureIgnoreCase))
            retPing = Convert.ToInt32(stopwatch.ElapsedMilliseconds);
        return retPing;
    }

    public async Task<List<AkiMiniProfile>> GetMiniProfilesAsync(AkiServer server, CancellationToken cancellationToken = default)
    {
        using MemoryStream respStream = await SendAsync(server, "/launcher/profiles", cancellationToken: cancellationToken);
        return await JsonSerializer.DeserializeAsync<List<AkiMiniProfile>>(respStream, cancellationToken: cancellationToken) ?? [];
    }

    private static string CreateLoginData(AkiServer server, AkiCharacter character)
    {
        string compatibleUri = server.Address.AbsoluteUri[..^1];
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

    private async Task<string> LoginOrRegisterAsync(AkiServer server, AkiCharacter character, string operation, CancellationToken cancellationToken = default)
    {
        using MemoryStream ms = await SendAsync(server, Path.Combine("/launcher/profile", operation), HttpMethod.Post, CreateLoginData(server, character), cancellationToken: cancellationToken);
        using StreamReader streamReader = new(ms);
        return await streamReader.ReadToEndAsync(cancellationToken);
    }

    public async Task<(string, AkiLoginStatus)> LoginAsync(AkiServer server, AkiCharacter character, CancellationToken cancellationToken = default)
    {
        string resp = await LoginOrRegisterAsync(server, character, "login", cancellationToken);
        AkiLoginStatus status = resp.ToLowerInvariant() switch
        {
            "invalid_password" => AkiLoginStatus.IncorrectPassword,
            "failed" => AkiLoginStatus.AccountNotFound,
            _ => AkiLoginStatus.Success
        };
        return (resp, status);
    }

    public async Task<(string, AkiLoginStatus)> RegisterCharacterAsync(AkiServer server, AkiCharacter character, CancellationToken cancellationToken = default)
    {
        string resp = await LoginOrRegisterAsync(server, character, "register", cancellationToken);
        AkiLoginStatus status = resp.ToLowerInvariant() switch
        {
            "ok" => AkiLoginStatus.Success,
            "failed" => AkiLoginStatus.UsernameTaken,
            _ => throw new Exception("Uh oh...")
        };

        return status == AkiLoginStatus.Success ? await LoginAsync(server, character, cancellationToken) : (string.Empty, status);
    }

    public async Task<AkiServerInfo?> GetAkiServerInfoAsync(AkiServer server, CancellationToken cancellationToken = default)
    {
        using MemoryStream respStream = await SendAsync(server.Address, "/launcher/server/connect", cancellationToken: cancellationToken);
        return await JsonSerializer.DeserializeAsync<AkiServerInfo>(respStream, cancellationToken: cancellationToken);
    }

    public Task<MemoryStream> GetAkiServerImage(AkiServer server, string assetPath, CancellationToken cancellationToken = default)
        => SendAsync(server.Address, Path.Combine("/files/", assetPath), cancellationToken: cancellationToken);
}

public enum AkiLoginStatus
{
    Success,
    AccountNotFound,
    UsernameTaken,
    IncorrectPassword
}
