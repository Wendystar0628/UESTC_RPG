"""实时页眉与历史入口：一个单实例 live、一个 Windows 原生 Observer。
用法：python -I -X utf8 dashboard/tools/live.py [--loop]；已有循环则退出。
产出：live.js、index.js；来源变动触发 collect.py；历史在同行程线程记账。
"""
import os
import sys
import time
import subprocess
import threading
from contextlib import nullcontext

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, '..', '..', '.collab', 'tools'))
from datafile import write, refresh_index, app_stamp
from monitor import history, util as U
from monitor.read import collab
from monitor.view import team

EVERY_S = 15
COLLECT_INTERVAL_S = 120
LOCK_FILE = U.REPO + '/output/.locks/live.lock'
WATCH_DIRS = [U.COLLAB + d for d in ('/status', '/notes', '/tasks', '/tools/monitor', '/tools/state/inbox_wait/delivered')] + [U.REPO + '/docs', U.REPO + '/scripts', U.REPO + '/dashboard/tools', U.REPO + '/dashboard/data/curated']
WATCH_FILES = [U.REPO + '/.git/HEAD', U.REPO + '/.git/index', U.REPO + '/.git/refs/heads/main', U.REPO + '/.git/logs/refs/remotes/origin/main', U.STATE + '/patrol.txt']


def write_once(recorder=None):
    with recorder.source_lock if recorder is not None else nullcontext():
        sessions, _ = team.sample(collab.queue_rows(), minutes=U.RECENT_MIN)
    result = []
    for side, who in (('claude', 'Claude'), ('codex', 'Codex')):
        session = next((s for s in sessions if s['side'] == side), {})
        state = session.get('state')
        result.append({'who': who, 'running': True if state == 'running' else False if state == 'idle' else None})
    if recorder is not None:
        recorder.capture_leads(result)
    path = write('auto', 'live', {'leads': result, 'appStamp': app_stamp()}, source='live.py', force=True)
    refresh_index()
    return path


def single_instance():
    """持有第一个字节的独占锁，进程退出自动释放。"""
    import msvcrt
    os.makedirs(os.path.dirname(LOCK_FILE), exist_ok=True)
    f = open(LOCK_FILE, 'a+b')
    try:
        f.seek(0)
        msvcrt.locking(f.fileno(), msvcrt.LK_NBLCK, 1)
    except OSError:
        f.close()
        return None
    return f


def main():
    if '--loop' not in sys.argv:
        write_once()
        return
    lock = single_instance()
    if lock is None:
        print('live.py --loop 已经在跑，退出')
        return
    # 明确选择官方 Windows 通知实现；不能静默切成 PollingObserver。
    from watchdog.events import FileSystemEventHandler
    from watchdog.observers.read_directory_changes import WindowsApiObserver

    recorder = history.Recorder()
    changed, collect_dirty, stop = threading.Event(), threading.Event(), threading.Event()
    collect_dirty.set()
    directories = [os.path.normcase(os.path.abspath(p)) for p in WATCH_DIRS]
    files = {os.path.normcase(os.path.abspath(p)) for p in WATCH_FILES}

    class Changed(FileSystemEventHandler):
        def on_any_event(self, event):
            if event.event_type not in ('created', 'deleted', 'modified', 'moved'):
                return
            for path in (event.src_path, getattr(event, 'dest_path', None)):
                if not path:
                    continue
                recorder.mark_dirty(path)  # 只标脏；不在 Observer 回调读文件、git 或会话
                normalized = os.path.normcase(os.path.abspath(path))
                if normalized in files or any(normalized == d or normalized.startswith(d + os.sep) for d in directories):
                    collect_dirty.set()
                    changed.set()

    observer = WindowsApiObserver()  # C256 复用此实例与 Changed 回调，不能再创建第二个
    roots = [p for p, _ in recorder.watch_paths()]
    roots += [p for p in WATCH_DIRS if os.path.isdir(p)]
    roots += [os.path.dirname(p) for p in WATCH_FILES if os.path.isdir(os.path.dirname(p))]
    scheduled = []
    for path in sorted(set(os.path.abspath(p) for p in roots), key=lambda p: (len(p), p)):
        if any(os.path.normcase(path) == os.path.normcase(p) or os.path.normcase(path).startswith(os.path.normcase(p) + os.sep) for p in scheduled):
            continue
        observer.schedule(Changed(), path, recursive=True)
        scheduled.append(path)
    worker = threading.Thread(target=recorder.run, args=(stop,), name='dashboard-history', daemon=True)
    try:
        observer.start()
        worker.start()
        last_live, last_collect = 0.0, 0.0
        while True:
            changed.clear()
            t = time.time()
            if t - last_live >= EVERY_S:
                last_live = t
                try:
                    write_once(recorder)
                except Exception as error:
                    print(f'{U.iso(t)} 写 live.js 失败：{type(error).__name__} {error}', file=sys.stderr)
            if collect_dirty.is_set() or t - last_collect >= COLLECT_INTERVAL_S:
                collect_dirty.clear()
                last_collect = t
                try:
                    result = subprocess.run([sys.executable, '-I', '-X', 'utf8', os.path.join(HERE, 'collect.py')], check=False)
                    if result.returncode:
                        print(f'{U.iso(t)} 项目采集退出码 {result.returncode}', file=sys.stderr)
                except Exception as error:
                    print(f'{U.iso(t)} 项目采集失败：{type(error).__name__} {error}', file=sys.stderr)
            changed.wait(timeout=max(.1, EVERY_S - (time.time() - last_live)))
    finally:
        stop.set()
        recorder.wake()
        observer.stop()
        if observer.is_alive():
            observer.join()
        if worker.is_alive():
            worker.join(timeout=5)
        lock.close()


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    main()
