using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Updater
{
    public static class Extentions
    {
        public static async Task DownloadAsync(this HttpClient client, Stream destination, string url, IProgress<double> progressReporter, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            long? contentLength = response.Content.Headers.ContentLength;
            if(!contentLength.HasValue)
            {
                await contentStream.CopyToAsync(destination, cancellationToken);
            }
            else
            {
                Progress<long> reportWrapper = new(br => progressReporter.Report((double)br / contentLength.Value));
                await contentStream.CopyToAsync(destination, 65535, reportWrapper, cancellationToken);
            }
        }

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
            while((bytesRead = await source.ReadAsync(dataBuffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(dataBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalReadBytes += bytesRead;
                progressReporter.Report(totalReadBytes);
            }
        }

        public static async Task MoveSIT(this DirectoryInfo directoryInfo, string destination)
            => await MoveSIT(directoryInfo, new DirectoryInfo(destination));

        public static async Task MoveSIT(this DirectoryInfo source, DirectoryInfo destination)
        {
            IEnumerable<DirectoryInfo> directories = source.EnumerateDirectories();
            IEnumerable<FileInfo> files = source.EnumerateFiles();
            destination.Create();

            foreach(DirectoryInfo directory in directories)
            {
                if (directory.Name.Equals("backup", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                DirectoryInfo newDestination = destination.CreateSubdirectory(directory.Name);
                await directory.MoveSIT(newDestination);
            }

            foreach (FileInfo file in files)
            {
                try
                {
                    file.MoveTo(Path.Combine(destination.FullName, file.Name));
                }
                catch (Exception) { }
            }
        }
    }
}
