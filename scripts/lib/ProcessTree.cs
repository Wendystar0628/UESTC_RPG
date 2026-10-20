using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

        public ProcessTree(string executable, string arguments, string directory, bool visible, Encoding encoding, bool isolatedDesktop)
        {
            IntPtr outputRead = IntPtr.Zero, outputWrite = IntPtr.Zero;
            IntPtr errorRead = IntPtr.Zero, errorWrite = IntPtr.Zero, input = IntPtr.Zero;
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
                StringBuilder command = new StringBuilder("\"" + executable + "\" " + arguments);
                Check(CreateProcess(executable, command, IntPtr.Zero, IntPtr.Zero, true, flags,
                    IntPtr.Zero, directory, ref startup, out started));
                process = started.Process;
                Check(AssignProcessToJobObject(job, process));
                stdoutTask = Capture(outputRead, stdout, encoding); outputRead = IntPtr.Zero;
                stderrTask = Capture(errorRead, stderr, encoding); errorRead = IntPtr.Zero;
                Check(ResumeThread(started.Thread) != uint.MaxValue);
            }
            catch
            {
                // Also terminate a suspended process if assignment itself failed.
                try
                {
                    if (process != IntPtr.Zero)
                    {
                        Check(TerminateProcess(process, 1));
                        Check(WaitForSingleObject(process, 5000) == 0);
                    }
                }
                finally { Dispose(); }
                throw;
            }
            finally
            {
                Close(ref started.Thread); Close(ref outputRead); Close(ref outputWrite);
                Close(ref errorRead); Close(ref errorWrite); Close(ref input);
            }
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

        public void Finish()
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

        public void Dispose()
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

        private static void Check(bool success) { if (!success) { throw new Win32Exception(Marshal.GetLastWin32Error()); } }
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
    }
}
