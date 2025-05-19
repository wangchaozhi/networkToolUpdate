using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using CommandLine;
using Microsoft.Win32;

namespace WpfUpdate;

public class Options
{
    [Option('u', "url", Required = true,  HelpText = "URL to download the update zip file.")]
    public string Url { get; set; }

    [Option('f', "filename", Required = true, HelpText = "Filename for the downloaded zip file.")]
    public string Filename { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(opts =>
            {
                var targetDirectory = GetApplicationDirectoryFromRegistry();
                Console.WriteLine("Checking for updates...");

                // 假设这里有代码来检查更新，决定是否需要下载更新文件
                if (NeedsUpdate())
                {
                    Console.WriteLine("Checking if MyApp is running...");
                    EnsureApplicationIsNotRunning("WpfApp2"); // 确保 MyApp 不在运行
                    Console.WriteLine("Downloading update...");
                    DownloadUpdate(opts.Url, opts.Filename, targetDirectory);
                    Console.WriteLine("Applying update...");
                    ApplyUpdate(opts.Filename, targetDirectory);
                    Console.WriteLine("Cleaning up...");
                    Cleanup(opts.Filename, targetDirectory);
                    RestartApplication(targetDirectory);
                }

                Console.WriteLine("Update process completed.");
            })
            .WithNotParsed<Options>(errors =>
            {
                // 处理解析错误
                Console.WriteLine("Required arguments missing. Please provide all necessary arguments.");
                Environment.Exit(1); // 使用非零值表示发生了错误
            });
    }


    static bool NeedsUpdate()
    {
        // 实现版本检查逻辑
        return true;
    }

    private static void EnsureApplicationIsNotRunning(string appName)
    {
        // 循环遍历所有同名进程
        foreach (var process in Process.GetProcessesByName(appName))
        {
            Console.WriteLine("Found running instance of MyApp. Attempting to close it...");
            if (!process.CloseMainWindow()) // 尝试正常关闭
            {
                var processHasExited = process.HasExited;
    
                process.Kill(); // 正常关闭失败，尝试强制结束
                process.WaitForExit(); // 等待进程退出
            }
        }
    }

    private static void DownloadUpdate(string url, string filename)
    {
        using (var client = new WebClient())
        {
            client.DownloadFile(url, filename);
        }
    }
    private static void DownloadUpdate(string url, string filename, string targetDirectory)
    {
        string fullPath = Path.Combine(targetDirectory, filename);
        using (var client = new WebClient())
        {
            Directory.CreateDirectory(targetDirectory);
            client.DownloadFile(url, fullPath);
        }
    }
    private static void ApplyUpdate(string filename, string targetDirectory)
    {
        string subDirectoryToExtract = "publish";
        string fullPath = Path.Combine(targetDirectory, filename);
        using (ZipArchive archive = ZipFile.OpenRead(fullPath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string destinationPath = Path.Combine(targetDirectory, entry.FullName.Substring(subDirectoryToExtract.Length + 1));
                if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                }
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }
    }
    
    private static void Cleanup(string zipFilename, string targetDirectory)
    {
        string fullPath = Path.Combine(targetDirectory, zipFilename);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        string directoryToDelete = Path.Combine(targetDirectory, "publish");
        if (Directory.Exists(directoryToDelete))
        {
            Directory.Delete(directoryToDelete, recursive: true);
        }
    }

  
    private static void RestartApplication(string targetDirectory)
    {
        string executablePath = Path.Combine(targetDirectory, "WpfApp2.exe");
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = true
        };
        Process.Start(startInfo);
        Environment.Exit(0);
    }

    
    
    private static string GetApplicationDirectoryFromRegistry()
    {
        const string keyPath = @"SOFTWARE\MyCompany\MyApp";
        using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
        {
            if (key != null)
            {
                object directoryPath = key.GetValue("InstallationDirectory");
                if (directoryPath != null)
                    return directoryPath.ToString();
            }
        }
        return null; // 或者返回一个默认路径
    }

}