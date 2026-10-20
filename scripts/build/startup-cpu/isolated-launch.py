"""Private-desktop launcher for startup CPU sampling; never switches desktops.

用法：python -I isolated-launch.py <output> <title|immediate|dwell> <dwell-seconds>
"""
import base64
import ctypes as c
from ctypes import wintypes as w
from pathlib import Path
import subprocess
import sys
import time

class STARTUPINFO(c.Structure):
    _fields_ = [("cb", w.DWORD), ("lpReserved", w.LPWSTR), ("lpDesktop", w.LPWSTR),
                ("lpTitle", w.LPWSTR), ("dwX", w.DWORD), ("dwY", w.DWORD),
                ("dwXSize", w.DWORD), ("dwYSize", w.DWORD), ("dwXCountChars", w.DWORD),
                ("dwYCountChars", w.DWORD), ("dwFillAttribute", w.DWORD),
                ("dwFlags", w.DWORD), ("wShowWindow", w.WORD), ("cbReserved2", w.WORD),
                ("lpReserved2", c.POINTER(c.c_byte)), ("hStdInput", w.HANDLE),
                ("hStdOutput", w.HANDLE), ("hStdError", w.HANDLE)]

class PROCESSINFO(c.Structure):
    _fields_ = [("hProcess", w.HANDLE), ("hThread", w.HANDLE),
                ("dwProcessId", w.DWORD), ("dwThreadId", w.DWORD)]

user = c.WinDLL("user32", use_last_error=True)
kernel = c.WinDLL("kernel32", use_last_error=True)
user.CreateDesktopW.argtypes = [w.LPCWSTR, w.LPCWSTR, c.c_void_p, w.DWORD, w.DWORD, c.c_void_p]
user.CreateDesktopW.restype = w.HANDLE
user.CloseDesktop.argtypes = [w.HANDLE]
kernel.CreateProcessW.argtypes = [w.LPCWSTR, w.LPWSTR, c.c_void_p, c.c_void_p, w.BOOL,
                                w.DWORD, c.c_void_p, w.LPCWSTR,
                                c.POINTER(STARTUPINFO), c.POINTER(PROCESSINFO)]
kernel.WaitForSingleObject.argtypes = [w.HANDLE, w.DWORD]
kernel.GetExitCodeProcess.argtypes = [w.HANDLE, c.POINTER(w.DWORD)]
kernel.CloseHandle.argtypes = [w.HANDLE]

out = Path(sys.argv[1]).resolve()
mode = sys.argv[2]
if mode not in ("title", "immediate", "dwell"):
    raise ValueError("Expected title, immediate or dwell")
dwell_seconds = float(sys.argv[3])
out.mkdir(parents=True, exist_ok=False)
desktop_name = "UESTC_C215_" + str(time.time_ns())
desktop = user.CreateDesktopW(desktop_name, None, None, 0, 0x01FF, None)
if not desktop:
    raise c.WinError(c.get_last_error())
process = PROCESSINFO()
try:
    startup = STARTUPINFO()
    startup.cb = c.sizeof(startup)
    startup.lpDesktop = desktop_name
    startup.dwFlags = 0x00000001  # STARTF_USESHOWWINDOW
    startup.wShowWindow = 0  # SW_HIDE; console remains on the private desktop
    # Redirect PowerShell streams, leaving the private console's real stdin intact
    # for dotnet-trace Stop. EncodedCommand avoids shell/path interpolation.
    def ps_literal(value):
        return "'" + str(value).replace("'", "''") + "'"

    capture_arguments = ["&", ps_literal(Path(__file__).with_name("capture.ps1")),
        "-Output", ps_literal(out), "-Mode", ps_literal(mode),
        "-TitleDwellSeconds", ps_literal(dwell_seconds)]
    stdout = ps_literal(out / "worker.stdout.txt")
    stderr = ps_literal(out / "worker.stderr.txt")
    script = ("$ErrorActionPreference = 'Stop'\ntry {\n    " + " ".join(capture_arguments) +
        " 1> " + stdout + " 2> " + stderr + "\n} catch {\n    " +
        "$_ | Out-String | Add-Content -LiteralPath " + stderr + "\n    exit 1\n}")
    arguments = ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-EncodedCommand", base64.b64encode(script.encode("utf-16-le")).decode("ascii")]
    command = subprocess.list2cmdline(arguments)
    if not kernel.CreateProcessW(None, c.create_unicode_buffer(command), None, None, False,
            0x00000010, None, str(Path(__file__).resolve().parents[3]), c.byref(startup), c.byref(process)):
        raise c.WinError(c.get_last_error())
    kernel.CloseHandle(process.hThread)
    while kernel.WaitForSingleObject(process.hProcess, 1000) == 258:
        pass
    code = w.DWORD()
    if not kernel.GetExitCodeProcess(process.hProcess, c.byref(code)):
        raise c.WinError(c.get_last_error())
    raise SystemExit(code.value)
finally:
    if process.hProcess:
        kernel.CloseHandle(process.hProcess)
    user.CloseDesktop(desktop)

