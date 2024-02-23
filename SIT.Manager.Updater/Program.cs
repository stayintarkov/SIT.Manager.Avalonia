using SIT.Manager.Updater;
using System.Diagnostics;
using System.IO.Compression;

const string SITMANAGER_PROC_NAME = "SIT.Manager.exe";
const string SITMANAGER_RELEASE_URL = @"https://github.com/stayintarkov/SIT.Manager/releases/latest/download/SIT.Manager.zip";

bool skipInteractivity = false;
bool killProcNoPrompt = false;
bool launchAfter = false;

//args = ["-nointeract", "-nopromptkill", "-launchafter"];
HashSet<string> options = args.Select(x => x.ToLowerInvariant()).ToHashSet();
skipInteractivity = options.Contains("-nointeract") || (Console.IsOutputRedirected && !Console.IsInputRedirected);
killProcNoPrompt = options.Contains("-nopromptkill");
launchAfter = options.Contains("-launchafter");

if (!skipInteractivity)
{
    Console.WriteLine("Ready to download latest version");
    Console.WriteLine("Press any key to start...");
    Console.ReadKey();
}

Process[] managerProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(SITMANAGER_PROC_NAME));
if(managerProcesses.Length > 0)
{
    if (killProcNoPrompt == false)
    {
        if(skipInteractivity)
            Environment.Exit(2);
        Console.WriteLine("An instance of 'SIT.Manager' was found. Would you like to close all instances? Y/N");
        string? response = Console.ReadLine();
        if (response == null || !response.Equals("y", StringComparison.InvariantCultureIgnoreCase))
            Environment.Exit(1);
    }

    foreach (var process in managerProcesses)
    {
        Console.WriteLine("Killing {0} with PID {1}\n", process.ProcessName, process.Id);
        bool clsMsgSent = process.CloseMainWindow();
        if(!clsMsgSent || !process.WaitForExit(TimeSpan.FromSeconds(5)))
        {
            process.Kill();
        }
    }
}


string workingDir = AppDomain.CurrentDomain.BaseDirectory;
if(!File.Exists(Path.Combine(workingDir, SITMANAGER_PROC_NAME)))
{
    Console.WriteLine("Unable to find '{0}' in root directory. Make sure the app is installed correctly.", SITMANAGER_PROC_NAME);
    if (!skipInteractivity)
    {
        Console.ReadKey();
    }
    Environment.Exit(3);
}

using HttpClient client = new();
string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
Directory.CreateDirectory(tempPath);
string zipName = Path.GetFileName(SITMANAGER_RELEASE_URL);
string zipPath = Path.Combine(tempPath, zipName);

int progressBarUpdateRate = 30;
using(CLIProgressBar progressBar = new(progressBarUpdateRate))
{
    try
    {
        Console.WriteLine("Downloading '{0}' to '{1}'", zipName, zipPath);
        Progress<double> progress = new(progressBar.Report);
        using FileStream fs = new(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await client.DownloadAsync(fs, SITMANAGER_RELEASE_URL, progress);
        progressBar.Report(1);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error during download: {0}", ex.Message);
        if(!skipInteractivity)
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
        Environment.Exit(4);
    }
}

Console.WriteLine("\nDownload complete.");
Console.WriteLine("Creating backup of SIT.Manager");

string backupPath = Path.Combine(workingDir, "Backup");
if(Directory.Exists(backupPath))
    Directory.Delete(backupPath, true);

DirectoryInfo workingFolderInfo = new(workingDir);
await workingFolderInfo.MoveSIT(backupPath);
FileInfo configFile = new(Path.Combine(backupPath, "ManagerConfig.json"));
if (configFile.Exists)
    configFile.CopyTo(Path.Combine(workingFolderInfo.FullName, configFile.Name));

Console.WriteLine("\nBackup complete. Extracting new version..\n");

ZipFile.ExtractToDirectory(zipPath, tempPath, false);

DirectoryInfo releasePath = new(Path.Combine(tempPath, "Release"));
await releasePath.MoveSIT(workingDir);

Directory.Delete(tempPath, true);

Console.WriteLine($"\nUpdate done. Backup can be found in the {Path.GetFileName(backupPath)} folder. Your settings have been saved.");
if(!skipInteractivity)
{
    Console.WriteLine("Press any key to finish...");
    Console.ReadKey();
}

if (launchAfter)
    Process.Start(SITMANAGER_PROC_NAME);

Environment.Exit(0);