using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Threading;
using System.Diagnostics;
using SIT.Manager.Avalonia.Classes.Exceptions;
using System.Text.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using SIT.Manager.Avalonia.Interfaces;

namespace SIT.Manager.Avalonia.Classes
{
    public partial class TarkovRequesting(Uri remoteEndPont, HttpClient httpClient, HttpClientHandler httpClientHandler, IZlibService compressionService)
    {
        public Uri RemoteEndPoint = remoteEndPont;
        private readonly HttpClient _httpClient = httpClient;
        private readonly HttpClientHandler _httpClientHandler = httpClientHandler;
        private static readonly MediaTypeHeaderValue _contentHeaderType = new("application/json");
        private IZlibService _compressionService = compressionService;
        private async Task<Stream> Send(string url, HttpMethod? method = null, string? data = null, TarkovRequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            method ??= HttpMethod.Get;
            requestOptions ??= new TarkovRequestOptions();

            UriBuilder serverUriBuilder = new(requestOptions.SchemeOverride ?? RemoteEndPoint.Scheme, RemoteEndPoint.Host, RemoteEndPoint.Port, url);
            HttpRequestMessage request = new(method, serverUriBuilder.Uri);
            request.Headers.ExpectContinue = true;

            //Typically deflate, gzip
            foreach(string encoding in requestOptions.AcceptEncoding)
            {
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(encoding));
            }

            if(method != HttpMethod.Get && !string.IsNullOrEmpty(data))
            {
                byte[] contentBytes = _compressionService.CompressToBytes(data, requestOptions.CompressionProfile, Encoding.UTF8);
                request.Content = new ByteArrayContent(contentBytes);
                request.Content.Headers.ContentType = _contentHeaderType;
                request.Content.Headers.ContentEncoding.Add("deflate");
                request.Content.Headers.Add("Content-Length", contentBytes.Length.ToString());
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(requestOptions.Timeout);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cts.Token);
                return await response.Content.ReadAsStreamAsync(cancellationToken);
            }
            catch(HttpRequestException ex)
            {
                //TODO: Loggy logging
                if (requestOptions.TryAgain)
                {
                    TarkovRequestOptions options = new()
                    {
                        Timeout = TimeSpan.FromSeconds(5),
                        CompressionProfile = ZlibCompression.BestCompression,
                        SchemeOverride = "http://",
                        AcceptEncoding = ["deflate"],
                        TryAgain = false
                    };
                    return await Send(url, method, data, options, cancellationToken);
                }
                else
                {
                    //TODO: I dislike rethrowing exceptions, the architecture of these net requests are flawed and need redesigned 
                    throw;
                }
            }
        }

        public async Task<string> PostJson(string url, string data)
        {
            using Stream postStream = await Send(url, HttpMethod.Post, data);
            if (postStream == null)
                return string.Empty;
            using MemoryStream ms = new();
            await postStream.CopyToAsync(ms);
            return _compressionService.Decompress(ms.ToArray());
        }

        public async Task<string> LoginAsync(TarkovLoginInfo loginInfo)
        {
            string SessionID = await PostJson("/launcher/profile/login", JsonSerializer.Serialize(loginInfo));
            return SessionID.ToLowerInvariant() switch
            {
                "invalid_password" => throw new IncorrectServerPasswordException(),
                "failed" => throw new AccountNotFoundException(),
                _ => SessionID,
            };
        }

        public async Task<bool> RegisterAccountAsync(TarkovLoginInfo loginInfo)
        {
            string serverResponse = await PostJson("/launcher/profile/register", JsonSerializer.Serialize(loginInfo));
            return serverResponse.Equals("ok", StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<AkiServerConnectionResponse> QueryServer()
        {
            string connectionData = await PostJson("/launcher/server/connect", JsonSerializer.Serialize(new object()));
            return JsonSerializer.Deserialize<AkiServerConnectionResponse>(connectionData) ?? throw new JsonException("Server returned invalid json.");
        }
    }

    public class TarkovRequestOptions()
    {
        public ZlibCompression CompressionProfile { get; init; } = ZlibCompression.BestSpeed;
        public string? SchemeOverride { get; init; }
        public string[] AcceptEncoding { get; init; } = ["deflate", "gzip"];
        public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(1);
        public bool TryAgain { get; init; } = true;
    }
}
