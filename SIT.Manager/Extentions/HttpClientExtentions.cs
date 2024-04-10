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
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        long? contentLength = response.Content.Headers.ContentLength;
        if (!contentLength.HasValue)
        {
            await contentStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            double totalLength = contentLength.Value;
            Progress<long> reportWrapper = new(br =>
            {
                double currentValue = br;
                progressReporter.Report(currentValue / totalLength * 100);
            });
            await contentStream.CopyToAsync(destination, 65535, reportWrapper, cancellationToken).ConfigureAwait(false);
        }
    }
}
