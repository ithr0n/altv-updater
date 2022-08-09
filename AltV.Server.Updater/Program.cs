using System.Net;

namespace AltV.Server.Updater;

public class Program
{
    const string mainUrl = "https://cdn.altv.mp";
    const string serverUrl = $"{mainUrl}/server";
    const string dataUrl = $"{mainUrl}/data";
    const string voiceServerUrl = $"{mainUrl}/voice-server";
    const string coreClrModuleUrl = $"{mainUrl}/coreclr-module";
    const string jsModuleUrl = $"{mainUrl}/js-module";
    const string jsBytecodeModuleUrl = $"{mainUrl}/js-bytecode-module";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Input the path or press Enter if the Loader is in the server folder");
        Console.Write("Path: ");

        string? path = Console.ReadLine()?.Trim();

        string? branch = null;

        while (string.IsNullOrEmpty(branch))
        {
            branch = RequestBranches();
        }

        if (string.IsNullOrEmpty(path))
        {
            path = Directory.GetCurrentDirectory();
        }

        Console.WriteLine("Cleanup cache, data and modules");
        Directory.Delete("cache", true);
        Directory.Delete("data", true);
        Directory.Delete("modules", true);

        Console.WriteLine($"\nDownload files to: {path}");

        await DownloadFiles(path, branch, "x64_win32");

        Console.WriteLine("\nFinish, Press enter to close...");
        Console.ReadLine();
    }

    private static string? RequestBranches()
    {
        Console.WriteLine("\nInput type BRANCH: release (r), rc and dev");
        Console.Write($"Branch: ");

        string? branch = Console.ReadLine();

        if (branch == "release" || branch == "rc" || branch == "dev")
            return branch;
        else if (branch == "r" || string.IsNullOrWhiteSpace(branch))
            return "release";
        else
            return null;
    }

    private static async Task DownloadFiles(string path, string branch, string platformPrefix)
    {
        string serverUrl = GenerateFullPath(Program.serverUrl, branch, platformPrefix);
        string dataUrl = GenerateFullPath(Program.dataUrl, branch);
        string voiceServerUrl = GenerateFullPath(Program.voiceServerUrl, branch, platformPrefix);
        string jsModuleUrl = GenerateFullPath(Program.jsModuleUrl, branch, platformPrefix);
        string jsBytecodeModuleUrl = GenerateFullPath(Program.jsBytecodeModuleUrl, branch, platformPrefix);
        string coreClrModuleUrl = GenerateFullPath(Program.coreClrModuleUrl, branch, platformPrefix);

        using var wb = new HttpClient();
        var postParams = new Dictionary<string, string>
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.33 Safari/537.36" }
        };

        Console.WriteLine("Downloading all files");

        var tasks = new List<Task>
        {
            DownloadFile(wb, serverUrl, path, "altv-server.exe"),
            DownloadFile(wb, serverUrl, path, "update.json"),

            DownloadFile(wb, dataUrl, path, "data/vehmodels.bin"),
            DownloadFile(wb, dataUrl, path, "data/vehmods.bin"),
            DownloadFile(wb, dataUrl, path, "data/clothes.bin"),
            DownloadFile(wb, dataUrl, path, "update.json", "data/update.json"),

            DownloadFile(wb, voiceServerUrl, path, "altv-voice-server.exe"),
            DownloadFile(wb, voiceServerUrl, path, "update.json", "voice-update.json"),
            
            DownloadFile(wb, coreClrModuleUrl, path, "AltV.Net.Host.dll"),
            DownloadFile(wb, coreClrModuleUrl, path, "AltV.Net.Host.runtimeconfig.json"),
            DownloadFile(wb, coreClrModuleUrl, path, "modules/csharp-module.dll", "modules/csharp-module/csharp-module.dll"),
            DownloadFile(wb, coreClrModuleUrl, path, "update.json", "modules/csharp-module/update.json"),

            DownloadFile(wb, jsModuleUrl, path, "modules/js-module/libnode.dll"),
            DownloadFile(wb, jsModuleUrl, path, "modules/js-module/js-module.dll"),
            DownloadFile(wb, jsModuleUrl, path, "modules/update.json", "modules/js-module/update.json"),

            DownloadFile(wb, jsBytecodeModuleUrl, path, "modules/js-bytecode-module.dll", "modules/js-bytecode-module/js-bytecode-module.dll"),
            DownloadFile(wb, jsBytecodeModuleUrl, path, "update.json", "modules/js-bytecode-module/update.json"),
        };

        await Task.WhenAll(tasks);
    }

    private static async Task DownloadFile(HttpClient wb, string pathUrl, string pathDisk, string pathFile, string? outputPath = null)
    {
        var urlPath = new Uri($"{pathUrl}/{pathFile}");
        var localFilePath = Path.Combine(pathDisk, pathFile);

        if (!string.IsNullOrEmpty(outputPath))
        {
            outputPath = $"{pathDisk}/{outputPath}";
        }
        else
        {
            outputPath = localFilePath;
        }

        var dirInfo = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dirInfo) && !Directory.Exists(dirInfo))
        {
            Directory.CreateDirectory(dirInfo);
        }

        try
        {
            var response = await wb.GetAsync(urlPath);
            using var fs = new FileStream(outputPath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static string GenerateFullPath(string url, string branch, string? platform = null)
    {
        var result = $"{url}/{branch}";

        if (!string.IsNullOrWhiteSpace(platform))
        {
            result = $"{result}/{platform}";
        }

        return result;
    }
}