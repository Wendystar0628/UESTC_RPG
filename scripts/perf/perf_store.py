"""性能记录发现、文件互斥与派生 JSON 发布；原始数据只读。
用法：由 analyze.py 调用；输入 output/perf 和作者 perf 根；不删除采集数据。
"""
import contextlib
import ctypes
import json
import os
import time
from pathlib import Path
from perf_model import read_json

@contextlib.contextmanager
def exclusive(path, timeout=0, create=False):
    """Windows 与记录器的 FileShare.None 互斥；文件可保留，存在不表示正在占用。"""
    path = Path(path)
    if create:
        path.parent.mkdir(parents=True, exist_ok=True)
    deadline = time.monotonic() + timeout
    handle = None
    if os.name == 'nt':
        kernel = ctypes.WinDLL('kernel32', use_last_error=True)
        kernel.CreateFileW.argtypes = [ctypes.c_wchar_p, ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p,
                                      ctypes.c_uint32, ctypes.c_uint32, ctypes.c_void_p]
        kernel.CreateFileW.restype = ctypes.c_void_p
        kernel.CloseHandle.argtypes = [ctypes.c_void_p]
        invalid = ctypes.c_void_p(-1).value
        while True:
            # Open existing session locks read-only; only the analyzer owns creation of its own lock.
            handle = kernel.CreateFileW(str(path), 0x80000000 | (0x40000000 if create else 0), 0,
                                        None, 4 if create else 3, 0x80, None)
            if handle != invalid:
                break
            code = ctypes.get_last_error()
            if code == 2 and not create:
                yield True
                return
            if code not in (32, 33):
                raise ctypes.WinError(code)
            if time.monotonic() >= deadline:
                yield False
                return
            time.sleep(.1)
        try:
            yield True
        finally:
            kernel.CloseHandle(handle)
    else:
        import fcntl
        try:
            handle = path.open('a+b' if create else 'rb')
        except FileNotFoundError:
            yield True
            return
        try:
            while True:
                try:
                    fcntl.flock(handle, fcntl.LOCK_EX | fcntl.LOCK_NB)
                    break
                except BlockingIOError:
                    if time.monotonic() >= deadline:
                        yield False
                        return
                    time.sleep(.1)
            yield True
        finally:
            handle.close()


def atomic_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    pending = path.with_name(path.name + '.pending')
    pending.write_text(json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    pending.replace(path)


def author_root(root):
    import re
    project = (Path(root) / 'game/project.godot').read_text(encoding='utf-8-sig')
    match = re.search(r'^config/custom_user_dir_name="([^"]+)"', project, re.M)
    if not match:
        raise ValueError('project.godot 缺少自定义用户目录名')
    return Path(os.environ.get('APPDATA', Path.home() / '.local/share')) / match[1] / 'perf'


def discover(out, author):
    paths = set()
    for directory in (Path(out) / 'runs', Path(author)):
        if directory.exists():
            paths.update(p.parent.resolve() for pattern in ('manifest.json', 'manifest.pending.json')
                         for p in directory.rglob(pattern) if not p.is_symlink())
    return sorted(paths)


def session_run_path(directory):
    run = Path(directory).resolve() / 'run.json'
    return run if run.is_file() else None


def session_run(directory):
    path = session_run_path(directory)
    return (read_json(path), path) if path else ({}, None)
