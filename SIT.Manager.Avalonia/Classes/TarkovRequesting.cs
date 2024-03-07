using SIT.Manager.Avalonia.Exceptions;
using SIT.Manager.Avalonia.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Classes
{
    public partial class TarkovRequesting(Uri remoteEndPont, HttpClient httpClient, IZlibService compressionService)
    {
        private static readonly MediaTypeHeaderValue _contentHeaderType = new("application/json");

        private readonly HttpClient _httpClient = httpClient;
        private readonly IZlibService _compressionService = compressionService;

        public Uri RemoteEndPoint = remoteEndPont;

        private async Task<Stream> Send(string url, HttpMethod? method = null, string? data = null, TarkovRequestOptions? requestOptions = null, CancellationToken cancellationToken = default)
        {
            method ??= HttpMethod.Get;
            requestOptions ??= new TarkovRequestOptions();

            UriBuilder serverUriBuilder = new(requestOptions.SchemeOverride ?? RemoteEndPoint.Scheme, RemoteEndPoint.Host, RemoteEndPoint.Port, url);
            HttpRequestMessage request = new(method, serverUriBuilder.Uri);
            request.Headers.ExpectContinue = true;

            //Typically deflate, gzip
            foreach (string encoding in requestOptions.AcceptEncoding)
            {
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue(encoding));
            }

            if (method != HttpMethod.Get && !string.IsNullOrEmpty(data))
            {
                byte[] contentBytes = _compressionService.CompressToBytes(data, requestOptions.CompressionProfile, Encoding.UTF8);
                request.Content = new ByteArrayContent(contentBytes);
                request.Content.Headers.ContentType = _contentHeaderType;
                request.Content.Headers.ContentEncoding.Add("deflate");
                request.Content.Headers.Add("Content-Length", contentBytes.Length.ToString());
            }

            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(requestOptions.Timeout);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                return await response.Content.ReadAsStreamAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
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
                    return await Send(url, method, data, options, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    //TODO: I dislike rethrowing exceptions, the architecture of these net requests are flawed and need redesigned 
                    throw;
                }
            }
        }

        public async Task<string> PostJson(string url, string data, CancellationToken cancellationToken = default)
        {
            using Stream postStream = await Send(url, HttpMethod.Post, data, cancellationToken: cancellationToken);
            if (postStream == null)
            {
                return string.Empty;
            }
            using MemoryStream ms = new();
            await postStream.CopyToAsync(ms, cancellationToken);
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

        /// <summary>
        /// Ping the server and await a resoponse
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Return true if the server responded correctly otherwise false</returns>
        public async Task<bool> PingServer(CancellationToken cancellationToken = default)
        {
            using Stream postStream = await Send("/launcher/ping", cancellationToken: cancellationToken);
            if (postStream == null)
            {
                return false;
            }

            using MemoryStream ms = new();
            await postStream.CopyToAsync(ms, cancellationToken);

            string pingResponse = _compressionService.Decompress(ms.ToArray());
            return pingResponse.Equals("\"pong!\"");
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
