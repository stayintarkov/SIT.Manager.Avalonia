using SIT.Manager.Models;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;
public interface IDiagnosticService
{
    public Task<string> CleanseLogFile(string fileData, bool bleachIt);
    public Task<string> GetLogFile(string logFilePath, bool bleachIt);
    public Task<Stream> GenerateDiagnosticReport(DiagnosticsOptions options);
}
