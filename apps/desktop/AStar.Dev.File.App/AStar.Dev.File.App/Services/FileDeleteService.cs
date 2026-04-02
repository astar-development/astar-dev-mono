using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AStar.Dev.File.App.Services;

public class FileDeleteService : IFileDeleteService
{
    public async Task DeleteFileAsync(string filePath, bool moveToRecycleBin = true)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
            return;

        await DeleteFilesAsync([filePath], moveToRecycleBin);
    }

    public async Task DeleteFilesAsync(IEnumerable<string> filePaths, bool moveToRecycleBin = true)
    {
        var files = filePaths.Where(f => System.IO.File.Exists(f)).ToList();
        if (files.Count == 0)
            return;

        await Task.Run(() =>
        {
            if (moveToRecycleBin)
            {
                if (OperatingSystem.IsWindows())
                {
                    MoveFilesToRecycleBinWindows(files);
                }
                else if (OperatingSystem.IsLinux())
                {
                    MoveFilesToTrashLinux(files);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    MoveFilesToTrashMacOS(files);
                }
                else
                {
                    PermanentlyDeleteFiles(files);
                }
            }
            else
            {
                PermanentlyDeleteFiles(files);
            }
        });
    }

    private static void PermanentlyDeleteFiles(IEnumerable<string> filePaths)
    {
        foreach (var file in filePaths)
        {
            try
            {
                System.IO.File.Delete(file);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete {file}: {ex.Message}");
            }
        }
    }

    private static void MoveFilesToTrashLinux(IEnumerable<string> filePaths)
    {
        try
        {
            var args = string.Join(" ", filePaths.Select(f => $"\"{f}\""));
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gio",
                    Arguments = $"trash {args}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Debug.WriteLine("gio trash failed, falling back to permanent delete");
                PermanentlyDeleteFiles(filePaths);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"gio trash not available: {ex.Message}. Falling back to permanent delete.");
            PermanentlyDeleteFiles(filePaths);
        }
    }

    private static void MoveFilesToTrashMacOS(IEnumerable<string> filePaths)
    {
        try
        {
            var args = string.Join(" ", filePaths.Select(f => $"\"{f}\""));
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "rm",
                    Arguments = $"-P {args}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"macOS trash failed: {ex.Message}");
            PermanentlyDeleteFiles(filePaths);
        }
    }

    private static void MoveFilesToRecycleBinWindows(IEnumerable<string> filePaths)
    {
        var paths = string.Join("\0", filePaths) + "\0\0";
        var fileOp = new SHFILEOPSTRUCT
        {
            wFunc = FileOperationType.FO_DELETE,
            pFrom = paths,
            fFlags = FileOperationFlags.FOF_ALLOWUNDO | FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOF_SILENT
        };

        try
        {
            int result = SHFileOperation(ref fileOp);

            if (result != 0)
            {
                Debug.WriteLine($"Shell delete failed with code {result}. Falling back to permanent delete.");
                PermanentlyDeleteFiles(filePaths);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Shell delete failed: {ex.Message}. Falling back to permanent delete.");
            PermanentlyDeleteFiles(filePaths);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public FileOperationType wFunc;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pTo;
        public FileOperationFlags fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpszProgressTitle;
    }

    private enum FileOperationType
    {
        FO_MOVE = 1,
        FO_COPY = 2,
        FO_DELETE = 3,
        FO_RENAME = 4
    }

    [Flags]
    private enum FileOperationFlags
    {
        FOF_MULTIDESTFILES = 0x0001,
        FOF_CONFIRMMOUSE = 0x0002,
        FOF_SILENT = 0x0004,
        FOF_RENAMEONCOLLISION = 0x0008,
        FOF_NOCONFIRMATION = 0x0010,
        FOF_WANTMAPPINGHANDLE = 0x0020,
        FOF_ALLOWUNDO = 0x0040,
        FOF_FILESONLY = 0x0080,
        FOF_SIMPLEPROGRESS = 0x0100,
        FOF_NOCONFIRMMKDIR = 0x0200,
        FOF_NOERRORUI = 0x0400,
        FOF_NOCOPYSECURITYATTRIBS = 0x0800,
        FOF_NORECURSION = 0x1000,
        FOF_NO_UI = FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_NOCONFIRMMKDIR
    }
}
