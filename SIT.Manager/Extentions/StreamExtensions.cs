using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.Manager.Extentions;
public static class StreamExtensions
{
    public static async Task CopyToAsync(this Stream source, Stream destination, ushort bufferSize, IProgress<long> progressReporter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(destination, nameof(destination));
        if (!source.CanRead)
            throw new InvalidOperationException($"'{nameof(source)}' is not readable.");
        if (!destination.CanWrite)
            throw new InvalidOperationException($"'{nameof(destination)}' is not writable.");

        byte[] dataBuffer = new byte[bufferSize];
        long totalReadBytes = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(dataBuffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(dataBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalReadBytes += bytesRead;
            progressReporter.Report(totalReadBytes);
        }
    }

    public static async Task<Stream> InflateAsync(this Stream zlibDataSource, CancellationToken cancellationToken = default)
    {
        MemoryStream ms = new();
        using ZLibStream inflateStream = new(ms, CompressionMode.Decompress);
        await inflateStream.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    public static async Task<string> ReadAsStringAsync(this Stream stream, CancellationToken cancellationToken = default)
        => await new StreamReader(stream).ReadToEndAsync(cancellationToken);
}
