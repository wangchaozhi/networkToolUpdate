using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using CommandLine;

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
                Console.WriteLine($"Downloading update from: {opts.Url}");
                Console.WriteLine("Checking for updates...");

                // 假设这里有代码来检查更新，决定是否需要下载更新文件
                if (NeedsUpdate())
                {
                    Console.WriteLine("Checking if MyApp is running...");
                    EnsureApplicationIsNotRunning("WpfApp2"); // 确保 MyApp 不在运行

                    Console.WriteLine("Downloading update...");
                    string zipFilename = opts.Filename;
                    // DownloadUpdate("http://192.168.3.81:9000/sunshinefarm-images/2023/publish%285%29.zip", zipFilename);
                    DownloadUpdate(opts.Url, zipFilename);
                    Console.WriteLine("Applying update...");
                    ApplyUpdate(zipFilename);
                    Console.WriteLine("Cleaning up...");
                    Cleanup(zipFilename); // 删除zip文件和解压出来的文件夹
                    RestartApplication();
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

    private static void ApplyUpdate(string filename)
    {
        string targetDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string subDirectoryToExtract = "publish";

        using (ZipArchive archive = ZipFile.OpenRead(filename))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName.StartsWith(subDirectoryToExtract + "/", StringComparison.OrdinalIgnoreCase))
                {
                    string destinationPath = Path.GetFullPath(Path.Combine(targetDirectory,
                        entry.FullName.Substring(subDirectoryToExtract.Length + 1)));

                    if (destinationPath.StartsWith(targetDirectory, StringComparison.OrdinalIgnoreCase))
                    {
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
        }
    }

    private static void Cleanup(string zipFilename)
    {
        File.Delete(zipFilename);
        string directoryToDelete = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "publish");
        if (Directory.Exists(directoryToDelete))
        {
            Directory.Delete(directoryToDelete, recursive: true);
        }
    }

    private static void RestartApplication()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "WpfApp2.exe",
            UseShellExecute = true
        };
        Process.Start(startInfo);
        Environment.Exit(0);
    }
}