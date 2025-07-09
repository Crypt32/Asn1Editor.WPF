using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SysadminsLV.Asn1Editor.API.Utils; 

class Logger : IDisposable {
    readonly String _logDirectory;
    readonly StreamWriter sessionStream;

    public Logger(String appDataDirectory) {
        _logDirectory = Directory.CreateDirectory(Path.Combine(appDataDirectory, "Logs")).FullName;
        String dt = "Log-" + DateTime.Now.ToString("ddMMyyyyHHmmss");
        sessionStream = new StreamWriter(Path.Combine(_logDirectory, $"{dt}-{Process.GetCurrentProcess().Id}.log")) { AutoFlush = true };
        Task.Run(flushOldLogs);
    }
    
    void flushOldLogs() {
        var directory = new DirectoryInfo(_logDirectory);
        foreach (FileInfo? fileInfo in directory.EnumerateFiles().OrderByDescending(x => x.LastWriteTime).Skip(10)) {
            fileInfo.Delete();
        }
    }
    
    public void Write(String s) {
        sessionStream.WriteLine(s);
    }
    public void Write(Exception e) {
        String dt = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
        sessionStream.WriteLine($"[{dt}]" + " An exception has been thrown:");
        Exception? ex = e;
        do {
            sessionStream.WriteLine($"\tError message: {ex.Message}\r\n\tStack trace:\r\n{ex.StackTrace.Replace("   ", "\t\t")}");
            ex = ex.InnerException;
        } while (ex is not null);
    }
    public void Dispose() {
        sessionStream?.Dispose();
    }
}