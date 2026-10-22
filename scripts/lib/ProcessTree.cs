using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Uestc.Tools
{
    // Windows PowerShell 5.1 cannot use Process.Kill(entireProcessTree). Start
    // suspended so even a fast launcher cannot escape before job assignment.
    public sealed class ProcessTree : IDisposable
    {
        private IntPtr job;
        private IntPtr process;
        private IntPtr desktop;
        private readonly object lifetime = new object();
        private readonly StringBuilder stdout = new StringBuilder();
        private readonly StringBuilder stderr = new StringBuilder();
        private Task stdoutTask;
        private Task stderrTask;
        private bool finished;
        public string StdOut { get { lock (stdout) { return stdout.ToString(); } } }
        public string StdErr { get { lock (stderr) { return stderr.ToString(); } } }
        public int ExitCode
        {
            get { uint code; Check(GetExitCodeProcess(process, out code)); return unchecked((int)code); }
        }
        public bool HasExited
        {
            get
            {
                uint result = WaitForSingleObject(process, 0);
                Check(result != uint.MaxValue);
                return result == 0;
            }
        }

        public sealed class FileUser
        {
            public int ProcessId;
            public long StartTimeFileTime;
        }

        // Ask Windows about the existing lock handle; no owner file or task registry.
        public static FileUser[] GetFileUsers(string path)
        {
            uint session;
            int code = RmStartSession(out session, 0, new StringBuilder(33));
            if (code != 0) { throw new Win32Exception(code); }
            Exception failure = null;
            try
            {
                code = RmRegisterResources(session, 1, new string[] { Path.GetFullPath(path) }, 0, IntPtr.Zero, 0, IntPtr.Zero);
                if (code != 0) { throw new Win32Exception(code); }
                uint needed, count = 0, reasons;
                code = RmGetList(session, out needed, ref count, null, out reasons);
                for (int attempt = 0; code == 234 && attempt < 3; attempt++)
                {
                    RestartProcessInfo[] users = new RestartProcessInfo[needed];
                    count = needed;
                    code = RmGetList(session, out needed, ref count, users, out reasons);
                    if (code == 0)
                    {
                        FileUser[] result = new FileUser[count];
                        for (int index = 0; index < result.Length; index++)
                        {
                            var process = users[index].Process;
                            result[index] = new FileUser { ProcessId = process.ProcessId,
                                StartTimeFileTime = ((long)(uint)process.StartTime.dwHighDateTime << 32) | (uint)process.StartTime.dwLowDateTime };
                        }
                        return result;
                    }
                }
                if (code != 0) { throw new Win32Exception(code); }
                return new FileUser[0];
            }
            catch (Exception caught) { failure = caught; throw; }
            finally
            {
                code = RmEndSession(session);
                if (code != 0)
                {
                    var cleanup = new Win32Exception(code);
                    if (failure != null) { failure.Data["ProcessTree.RmEndSession"] = cleanup; }
                    else { throw cleanup; }
                }
            }
        }

        public ProcessTree(string executable, string arguments, string directory, bool visible, Encoding encoding, bool isolatedDesktop, IDictionary environment)
        {
            IntPtr outputRead = IntPtr.Zero, outputWrite = IntPtr.Zero;
            IntPtr errorRead = IntPtr.Zero, errorWrite = IntPtr.Zero, input = IntPtr.Zero;
            IntPtr environmentBlock = IntPtr.Zero;
            ProcessInformation started = new ProcessInformation();
            try
            {
                string desktopName = null;
                if (isolatedDesktop)
                {
                    desktopName = "UESTC_Capture_" + Guid.NewGuid().ToString("N");
                    desktop = CreateDesktop(desktopName, null, IntPtr.Zero, 0, 0x01FF, IntPtr.Zero);
                    Check(desktop != IntPtr.Zero);
                }
                job = CreateJobObject(IntPtr.Zero, null);
                Check(job != IntPtr.Zero);
                ExtendedLimits limits = new ExtendedLimits();
                limits.Basic.LimitFlags = 0x2000; // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                Check(SetInformationJobObject(job, 9, ref limits, Marshal.SizeOf(typeof(ExtendedLimits))));
                SecurityAttributes security = new SecurityAttributes();
                security.Length = Marshal.SizeOf(typeof(SecurityAttributes));
                security.InheritHandle = true;
                Check(CreatePipe(out outputRead, out outputWrite, ref security, 0));
                Check(SetHandleInformation(outputRead, 1, 0));
                Check(CreatePipe(out errorRead, out errorWrite, ref security, 0));
                Check(SetHandleInformation(errorRead, 1, 0));
                input = CreateFile("NUL", 0x80000000, 3, ref security, 3, 0, IntPtr.Zero);
                Check(input != new IntPtr(-1));
                StartupInfo startup = new StartupInfo();
                startup.Size = Marshal.SizeOf(typeof(StartupInfo));
                startup.Desktop = desktopName;
                startup.Flags = 0x100; // STARTF_USESTDHANDLES
                startup.Input = input; startup.Output = outputWrite; startup.Error = errorWrite;
                uint flags = 4; // CREATE_SUSPENDED
                if (!visible) { flags |= 0x08000000; } // CREATE_NO_WINDOW
                if (environment != null && environment.Count > 0)
                {
                    environmentBlock = CreateEnvironmentBlock(environment);
                    flags |= 0x00000400; // CREATE_UNICODE_ENVIRONMENT
                }
                StringBuilder command = new StringBuilder("\"" + executable + "\" " + arguments);
                Check(CreateProcess(executable, command, IntPtr.Zero, IntPtr.Zero, true, flags,
                    environmentBlock, directory, ref startup, out started));
                process = started.Process;
                Check(AssignProcessToJobObject(job, process));
                stdoutTask = Capture(outputRead, stdout, encoding); outputRead = IntPtr.Zero;
                stderrTask = Capture(errorRead, stderr, encoding); errorRead = IntPtr.Zero;
                Check(ResumeThread(started.Thread) != uint.MaxValue);
            }
            catch (Exception failure)
            {
                // Preserve the startup failure; its Data carries each cleanup failure.
                // Also terminate a suspended process if assignment itself failed.
                if (process != IntPtr.Zero)
                {
                    try { Check(TerminateProcess(process, 1)); }
                    catch (Exception cleanup) { failure.Data["ProcessTree.TerminateProcess"] = cleanup; }
                    try
                    {
                        uint result = WaitForSingleObject(process, 5000);
                        Check(result != uint.MaxValue);
                        if (result != 0) { throw new TimeoutException("Process did not exit within 5 seconds after startup failed."); }
                    }
                    catch (Exception cleanup) { failure.Data["ProcessTree.WaitForSingleObject"] = cleanup; }
                }
                try { Dispose(); }
                catch (Exception cleanup) { failure.Data["ProcessTree.Dispose"] = cleanup; }
                throw;
            }
            finally
            {
                if (environmentBlock != IntPtr.Zero) { Marshal.FreeHGlobal(environmentBlock); }
                Close(ref started.Thread); Close(ref outputRead); Close(ref outputWrite);
                Close(ref errorRead); Close(ref errorWrite); Close(ref input);
            }
        }

        private static IntPtr CreateEnvironmentBlock(IDictionary environment)
        {
            // Native CreateProcess needs a sorted, double-NUL terminated UTF-16
            // block. Preserve inherited variables, including drive current dirs.
            var merged = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                merged[(string)entry.Key] = (string)entry.Value;
            foreach (DictionaryEntry entry in environment)
            {
                string key = entry.Key == null ? null : entry.Key.ToString();
                string value = entry.Value == null ? null : entry.Value.ToString();
                if (String.IsNullOrEmpty(key) || key.IndexOfAny(new[] { '=', '\0' }) >= 0 ||
                    (value != null && value.IndexOf('\0') >= 0))
                    throw new ArgumentException("Invalid child environment variable.");
                if (value == null) { merged.Remove(key); }
                else { merged[key] = value; }
            }
            var block = new StringBuilder();
            foreach (var entry in merged) { block.Append(entry.Key).Append('=').Append(entry.Value).Append('\0'); }
            block.Append('\0');
            return Marshal.StringToHGlobalUni(block.ToString());
        }

        private static Task Capture(IntPtr pipe, StringBuilder text, Encoding encoding)
        {
            StreamReader reader = new StreamReader(new FileStream(new SafeFileHandle(pipe, true), FileAccess.Read), encoding, true);
            return Task.Factory.StartNew(delegate
            {
                using (reader)
                {
                    char[] buffer = new char[4096];
                    int count;
                    while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        lock (text) { text.Append(buffer, 0, count); }
                    }
                }
            }, System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        // Process.CloseMainWindow cannot address a window on our private desktop.
        // Use a fresh thread without windows/hooks; never switch the input desktop.
        public long PostClose(IntPtr window)
        {
            lock (lifetime)
            {
                if (finished || job == IntPtr.Zero) { throw new ObjectDisposedException("ProcessTree"); }
                var result = new TaskCompletionSource<long>();
                Thread sender = new Thread(delegate()
                {
                    IntPtr windowProcess = IntPtr.Zero;
                    try
                    {
                        if (desktop != IntPtr.Zero) { Check(SetThreadDesktop(desktop), "SetThreadDesktop"); }
                        uint windowProcessId;
                        Check(GetWindowThreadProcessId(window, out windowProcessId) != 0, "GetWindowThreadProcessId");
                        windowProcess = OpenProcess(0x1000, false, windowProcessId); // PROCESS_QUERY_LIMITED_INFORMATION
                        Check(windowProcess != IntPtr.Zero, "OpenProcess(window PID)");
                        // Console launchers may own a child process window; validate its actual PID against our job.
                        bool owned;
                        Check(IsProcessInJob(windowProcess, job, out owned), "IsProcessInJob(window PID)");
                        if (!owned)
                            throw new InvalidOperationException("The ready window PID " + windowProcessId + " does not belong to this process tree.");
                        long postedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                        Check(PostMessage(window, 0x0010, UIntPtr.Zero, IntPtr.Zero), "PostMessage(WM_CLOSE)");
                        result.SetResult(postedAt);
                    }
                    catch (Exception caught) { result.SetException(caught); }
                    finally { Close(ref windowProcess); }
                });
                sender.IsBackground = true;
                sender.Start();
                sender.Join(); // Own the short sender through completion before desktop/job release.
                return result.Task.GetAwaiter().GetResult();
            }
        }

        public void Finish()
        {
            lock (lifetime)
            {
                if (finished) { return; }
                finished = true;
                // Termination and pipe drain share one bounded cleanup budget. Keep
                // ownership until descendants have exited, then release the job.
                DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                try
                {
                    if (job != IntPtr.Zero)
                    {
                        Check(TerminateJobObject(job, 1));
                        Accounting accounting;
                        do
                        {
                            Check(QueryInformationJobObject(job, 1, out accounting, Marshal.SizeOf(typeof(Accounting)), IntPtr.Zero));
                            if (accounting.ActiveProcesses == 0) { break; }
                            if (DateTime.UtcNow >= deadline) { throw new TimeoutException("Process tree did not exit within 5 seconds."); }
                            System.Threading.Thread.Sleep(10);
                        } while (true);
                    }
                    Task[] readers = Array.FindAll(new[] { stdoutTask, stderrTask }, delegate(Task task) { return task != null; });
                    int remaining = Math.Max(0, (int)(deadline - DateTime.UtcNow).TotalMilliseconds);
                    if (readers.Length > 0 && !Task.WaitAll(readers, remaining))
                    {
                        throw new TimeoutException("Process output did not close within 5 seconds.");
                    }
                }
                finally { Close(ref job); }
            }
        }

        public void Dispose()
        {
            lock (lifetime)
            {
                try { Finish(); }
                finally
                {
                    Close(ref process);
                    if (desktop != IntPtr.Zero)
                    {
                        IntPtr owned = desktop; desktop = IntPtr.Zero;
                        Check(CloseDesktop(owned));
                    }
                }
            }
        }

        private static void Check(bool success) { if (!success) { throw new Win32Exception(Marshal.GetLastWin32Error()); } }
        private static void Check(bool success, string operation)
        {
            if (success) { return; }
            int code = Marshal.GetLastWin32Error();
            throw new Win32Exception(code, operation + ": " + new Win32Exception(code).Message);
        }
        private static void Close(ref IntPtr handle)
        {
            if (handle != IntPtr.Zero && handle != new IntPtr(-1)) { CloseHandle(handle); }
            handle = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Length; public IntPtr Descriptor; [MarshalAs(UnmanagedType.Bool)] public bool InheritHandle; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct StartupInfo
        {
            public int Size; public string Reserved; public string Desktop; public string Title;
            public int X, Y, XSize, YSize, XChars, YChars, FillAttribute, Flags;
            public short ShowWindow, ReservedSize; public IntPtr ReservedData, Input, Output, Error;
        }
        [StructLayout(LayoutKind.Sequential)] private struct ProcessInformation { public IntPtr Process, Thread; public uint ProcessId, ThreadId; }
        [StructLayout(LayoutKind.Sequential)] private struct RestartUniqueProcess
        {
            public int ProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME StartTime;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct RestartProcessInfo
        {
            public RestartUniqueProcess Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string ApplicationName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string ServiceName;
            public uint ApplicationType, Status, SessionId;
            [MarshalAs(UnmanagedType.Bool)] public bool Restartable;
        }
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)] private static extern int RmStartSession(out uint session, uint flags, StringBuilder key);
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)] private static extern int RmRegisterResources(uint session, uint fileCount, string[] files, uint applicationCount, IntPtr applications, uint serviceCount, IntPtr services);
        [DllImport("rstrtmgr.dll")] private static extern int RmGetList(uint session, out uint needed, ref uint count, [In, Out] RestartProcessInfo[] processes, out uint reasons);
        [DllImport("rstrtmgr.dll")] private static extern int RmEndSession(uint session);
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits
        {
            public long ProcessTime, JobTime; public uint LimitFlags;
            public UIntPtr MinimumWorkingSet, MaximumWorkingSet; public uint ActiveProcessLimit;
            public UIntPtr Affinity; public uint PriorityClass, SchedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits
        {
            public BasicLimits Basic; public ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes;
            public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemory, PeakJobMemory;
        }
        [StructLayout(LayoutKind.Sequential)] private struct Accounting
        {
            public long UserTime, KernelTime, PeriodUserTime, PeriodKernelTime;
            public uint PageFaults, TotalProcesses, ActiveProcesses, TerminatedProcesses;
        }
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(IntPtr job, int kind, ref ExtendedLimits limits, int size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool QueryInformationJobObject(IntPtr job, int kind, out Accounting accounting, int size, IntPtr returned);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateJobObject(IntPtr job, uint exitCode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool IsProcessInJob(IntPtr process, IntPtr job, out bool result);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, uint processId);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CreatePipe(out IntPtr read, out IntPtr write, ref SecurityAttributes attributes, uint size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetHandleInformation(IntPtr handle, uint mask, uint flags);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern IntPtr CreateFile(string name, uint access, uint share, ref SecurityAttributes attributes, uint creation, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool CreateProcess(string application, StringBuilder command, IntPtr processAttributes, IntPtr threadAttributes, bool inherit, uint flags, IntPtr environment, string directory, ref StartupInfo startup, out ProcessInformation process);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(IntPtr process, uint exitCode);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateDesktop(string name, string device, IntPtr mode, uint flags, uint access, IntPtr attributes);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseDesktop(IntPtr desktop);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetThreadDesktop(IntPtr desktop);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)] private static extern bool PostMessage(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);
    }
}
