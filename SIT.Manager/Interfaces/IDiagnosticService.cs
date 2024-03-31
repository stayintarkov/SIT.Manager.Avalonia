using SIT.Manager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Manager.Interfaces;
public interface IDiagnosticService
{
    public Task<string> CleanseLogFile(string fileData);
    public Task<string?> GetLogFile(string logFilePath);
    public Task<Stream> GenerateDiagnosticReport(DiagnosticsOptions options);
}
