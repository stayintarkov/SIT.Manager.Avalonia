using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Extentions;

/// <summary>
/// Source: https://gist.github.com/dalexsoto/9fd3c5bdbe9f61a717d47c5843384d11
/// </summary>
internal static class HttpClientExtentions
{
    public static async Task DownloadAsync(this HttpClient client, Stream destination, string url, IProgress<double> progressReporter, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        long? contentLength = response.Content.Headers.ContentLength;
        if (!contentLength.HasValue)
        {
            await contentStream.CopyToAsync(destination, cancellationToken);
        }
        else
        {
            Progress<long> reportWrapper = new(br => progressReporter.Report((double) br / contentLength.Value));
            await contentStream.CopyToAsync(destination, 65535, reportWrapper, cancellationToken);
        }
    }
}
