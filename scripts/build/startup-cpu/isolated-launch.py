"""Private-desktop launcher for startup CPU sampling; never switches desktops."""
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
    command = subprocess.list2cmdline(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", str(Path(__file__).with_name("capture.ps1")), "-Output", str(out), "-Mode", mode,
        "-TitleDwellSeconds", str(dwell_seconds)])
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

