using System.Diagnostics;
using System.Runtime.InteropServices;

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
        var files = filePaths.Where(System.IO.File.Exists).ToList();
        if (files.Count == 0)
            return;

        await Task.Run(() =>
        {
            if (moveToRecycleBin)
            {
                if (OperatingSystem.IsWindows())
                    MoveFilesToRecycleBinWindows(files);
                else if (OperatingSystem.IsLinux())
                    MoveFilesToTrashLinux(files);
                else if (OperatingSystem.IsMacOS())
                    MoveFilesToTrashMacOs(files);
                else
                    PermanentlyDeleteFiles(files);
            }
            else
                PermanentlyDeleteFiles(files);
        });
    }

    private static void PermanentlyDeleteFiles(IEnumerable<string> filePaths)
    {
        foreach (string file in filePaths)
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
        string[] paths = filePaths as string[] ?? filePaths.ToArray();
        try
        {
            string args = string.Join(" ", paths.Select(f => $"\"{f}\""));
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

            if (process.ExitCode == 0) return;

            Debug.WriteLine("gio trash failed, falling back to permanent delete");
            PermanentlyDeleteFiles(paths);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"gio trash not available: {ex.Message}. Falling back to permanent delete.");
            PermanentlyDeleteFiles(paths);
        }
    }

    private static void MoveFilesToTrashMacOs(IEnumerable<string> filePaths)
    {
        string[] paths = filePaths as string[] ?? filePaths.ToArray();
        try
        {
            string args = string.Join(" ", paths.Select(f => $"\"{f}\""));
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
            PermanentlyDeleteFiles(paths);
        }
    }

    private static void MoveFilesToRecycleBinWindows(IEnumerable<string> filePaths)
    {
        IEnumerable<string> pathsAsArray = filePaths as string[] ?? filePaths.ToArray();
        string paths = string.Join("\0", pathsAsArray) + "\0\0";
        var fileOp = new Shfileopstruct
        {
            wFunc = FileOperationType.FoDelete,
            pFrom = paths,
            fFlags = FileOperationFlags.FofAllowundo | FileOperationFlags.FofNoconfirmation | FileOperationFlags.FofNoerrorui | FileOperationFlags.FofSilent
        };

        try
        {
            int result = SHFileOperation(ref fileOp);

            if (result == 0) return;

            Debug.WriteLine($"Shell delete failed with code {result}. Falling back to permanent delete.");
            PermanentlyDeleteFiles(pathsAsArray);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Shell delete failed: {ex.Message}. Falling back to permanent delete.");
            PermanentlyDeleteFiles(pathsAsArray);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    private static extern int SHFileOperation(ref Shfileopstruct lpFileOp);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct Shfileopstruct
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
        FoMove = 1,
        FoCopy = 2,
        FoDelete = 3,
        FoRename = 4
    }

    [Flags]
    private enum FileOperationFlags
    {
        FofMultidestfiles = 0x0001,
        FofConfirmmouse = 0x0002,
        FofSilent = 0x0004,
        FofRenameoncollision = 0x0008,
        FofNoconfirmation = 0x0010,
        FofWantmappinghandle = 0x0020,
        FofAllowundo = 0x0040,
        FofFilesonly = 0x0080,
        FofSimpleprogress = 0x0100,
        FofNoconfirmmkdir = 0x0200,
        FofNoerrorui = 0x0400,
        FofNocopysecurityattribs = 0x0800,
        FofNorecursion = 0x1000,
        FofNoUi = FofSilent | FofNoconfirmation | FofNoerrorui | FofNoconfirmmkdir
    }
}
