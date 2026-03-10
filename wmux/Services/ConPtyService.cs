using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace wmux.Services;

/// <summary>
/// Windows ConPTY (Console Pseudo Terminal) service.
/// Creates a PTY, spawns a shell process, and pipes I/O between
/// the shell and xterm.js running in WebView2.
/// </summary>
public sealed class ConPtySession : IDisposable
{
    // ── Win32 types ───────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD { public short X, Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD size, SafeFileHandle hInput,
        SafeFileHandle hOutput, uint dwFlags, out IntPtr phPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int ResizePseudoConsole(IntPtr hPC, COORD size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcess(string? lpApplicationName, string lpCommandLine,
        IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles,
        uint dwCreationFlags, IntPtr lpEnvironment, string? lpCurrentDirectory,
        ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList,
        int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags,
        IntPtr attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe,
        IntPtr lpPipeAttributes, int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    // ── Constants ─────────────────────────────────────────────────────────────

    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    private static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = new(0x00020016);

    // ── State ─────────────────────────────────────────────────────────────────

    private IntPtr _hPseudoConsole;
    private SafeFileHandle? _pipeIn, _pipeOut, _pipeInShell, _pipeOutShell;
    private PROCESS_INFORMATION _processInfo;
    private Stream? _readStream, _writeStream;
    private CancellationTokenSource _cts = new();

    public event Action<string>? DataReceived;
    public event Action? ProcessExited;

    public int ProcessId => _processInfo.dwProcessId;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Start(string shell, string workingDirectory, int cols = 120, int rows = 30)
    {
        CreatePipes();
        CreatePTY(cols, rows);
        SpawnProcess(shell, workingDirectory);
        BeginRead();
    }

    public void Write(string input)
    {
        if (_writeStream is null) return;
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        _writeStream.Write(bytes, 0, bytes.Length);
        _writeStream.Flush();
    }

    public void Resize(int cols, int rows)
    {
        if (_hPseudoConsole != IntPtr.Zero)
            ResizePseudoConsole(_hPseudoConsole, new COORD { X = (short)cols, Y = (short)rows });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void CreatePipes()
    {
        if (!CreatePipe(out _pipeInShell, out _pipeOut, IntPtr.Zero, 0) ||
            !CreatePipe(out _pipeIn, out _pipeOutShell, IntPtr.Zero, 0))
            throw new InvalidOperationException("CreatePipe failed: " + Marshal.GetLastWin32Error());
    }

    private void CreatePTY(int cols, int rows)
    {
        var size = new COORD { X = (short)cols, Y = (short)rows };
        int hr = CreatePseudoConsole(size, _pipeInShell!, _pipeOutShell!, 0, out _hPseudoConsole);
        if (hr < 0) throw new InvalidOperationException($"CreatePseudoConsole failed: {hr:X}");

        // ConPTY pipes don't support overlapped (async) I/O — use synchronous streams
        _readStream  = new FileStream(_pipeIn!,  System.IO.FileAccess.Read,  4096, isAsync: false);
        _writeStream = new FileStream(_pipeOut!, System.IO.FileAccess.Write, 4096, isAsync: false);
    }

    private void SpawnProcess(string shell, string workingDirectory)
    {
        // Build STARTUPINFOEX with PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE
        IntPtr size = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
        IntPtr attrList = Marshal.AllocHGlobal(size);
        try
        {
            InitializeProcThreadAttributeList(attrList, 1, 0, ref size);
            IntPtr hpcValue = _hPseudoConsole;
            UpdateProcThreadAttribute(attrList, 0, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                hpcValue, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero);

            var si = new STARTUPINFOEX
            {
                StartupInfo = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFOEX>() },
                lpAttributeList = attrList
            };

            bool ok = CreateProcess(null, shell, IntPtr.Zero, IntPtr.Zero, false,
                EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT,
                IntPtr.Zero, workingDirectory, ref si, out _processInfo);

            if (!ok) throw new InvalidOperationException("CreateProcess failed: " + Marshal.GetLastWin32Error());
        }
        finally
        {
            DeleteProcThreadAttributeList(attrList);
            Marshal.FreeHGlobal(attrList);
        }
    }

    private void BeginRead()
    {
        var token = _cts.Token;
        // Run synchronous blocking read on a dedicated thread (ConPTY pipes don't support overlapped I/O)
        Thread thread = new(() =>
        {
            var buffer = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int read = _readStream!.Read(buffer, 0, buffer.Length);
                    if (read == 0) break;
                    var text = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                    DataReceived?.Invoke(text);
                }
            }
            catch (ObjectDisposedException) { }
            catch { }
            finally { ProcessExited?.Invoke(); }
        })
        {
            IsBackground = true,
            Name = "ConPTY-Reader"
        };
        thread.Start();
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (_hPseudoConsole != IntPtr.Zero) { ClosePseudoConsole(_hPseudoConsole); _hPseudoConsole = IntPtr.Zero; }
        _readStream?.Dispose();
        _writeStream?.Dispose();
        _pipeIn?.Dispose();
        _pipeOut?.Dispose();
        _pipeInShell?.Dispose();
        _pipeOutShell?.Dispose();
        if (_processInfo.hProcess != IntPtr.Zero) CloseHandle(_processInfo.hProcess);
        if (_processInfo.hThread != IntPtr.Zero) CloseHandle(_processInfo.hThread);
    }
}
