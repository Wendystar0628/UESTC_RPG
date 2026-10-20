"""实时页眉心跳：只公开两个指挥是否在跑及 appStamp；独立进程刷新 v2 项目汇总。
用法：python -I -X utf8 dashboard/tools/live.py [--loop]；已有循环则退出。
产出：dashboard/data/auto/live.js、index.js；--loop 调 collect.py 产出 v2 数据。
"""
import os
import sys
import time
import subprocess
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, '..', '..', '.collab', 'tools'))
from datafile import write, refresh_index, app_stamp
from monitor import util as U
from monitor.read import collab
from monitor.view import team
EVERY_S = 15
COLLECT_INTERVAL_S = 120
LOCK_FILE = U.REPO + '/output/.locks/live.lock'
WATCH_DIRS = [U.COLLAB + d for d in ('/status', '/notes', '/tasks', '/tools/monitor', '/tools/state/inbox_wait/delivered')] + [U.REPO + '/docs', U.REPO + '/scripts', U.REPO + '/dashboard/tools', U.REPO + '/dashboard/data/curated']
WATCH_FILES = [U.REPO + '/.git/HEAD', U.REPO + '/.git/index', U.REPO + '/.git/refs/heads/main', U.REPO + '/.git/logs/refs/remotes/origin/main', U.STATE + '/patrol.txt']


def write_once():
    sessions, _ = team.sample(collab.queue_rows(), minutes=U.RECENT_MIN)
    result = []
    for side, who in (('claude', 'Claude'), ('codex', 'Codex')):
        session = next((s for s in sessions if s['side'] == side), {})
        state = session.get('state')
        result.append({'who': who, 'running': True if state == 'running' else False if state == 'idle' else None})
    path = write('auto', 'live', {'leads': result, 'appStamp': app_stamp()}, source='live.py', force=True)
    refresh_index()
    return path


def inputs_stamp():
    """项目采集各输入的最新修改时间与文件数（目录里删了文件也算变）；docs/godot-api 是生成参考，不看。"""
    versions = []
    for d in WATCH_DIRS:
        for root, dirs, files in os.walk(d):
            dirs[:] = [x for x in dirs if x not in ('godot-api', '__pycache__')]
            for f in files:
                try:
                    path = os.path.join(root, f)
                    stat = os.stat(path)
                    versions.append((path, stat.st_mtime_ns, stat.st_size))
                except OSError:
                    pass
    for f in WATCH_FILES:
        try:
            stat = os.stat(f)
            versions.append((f, stat.st_mtime_ns, stat.st_size))
        except OSError:
            pass
    return tuple(sorted(versions))


def single_instance():
    """独占打开锁文件的第一个字节：已有一个在跑就拿不到。进程退出（含崩溃）锁自动释放。返回要一直持有的文件对象，拿不到返回 None。"""
    import msvcrt
    os.makedirs(os.path.dirname(LOCK_FILE), exist_ok=True)
    f = open(LOCK_FILE, "a+b")
    try:
        f.seek(0)
        msvcrt.locking(f.fileno(), msvcrt.LK_NBLCK, 1)
    except OSError:
        f.close()
        return None
    return f


def main():
    if "--loop" not in sys.argv:
        write_once()
        return
    lock = single_instance()
    if lock is None:
        print("live.py --loop 已经在跑，退出")
        return
    last_collect, last_stamp = 0.0, None
    while True:
        t = time.time()
        try:
            write_once()
        except Exception as e:   # 写失败（磁盘、占用）下一轮再试
            print(f"{U.iso(t)} 写 live.js 失败：{type(e).__name__} {e}", file=sys.stderr)
        # 项目数据也由这个循环定时刷新：没有 Claude 会话（巡逻停了）时观察台照样更新
        stamp = inputs_stamp()
        if stamp != last_stamp or t - last_collect >= COLLECT_INTERVAL_S:
            last_collect, last_stamp = t, stamp
            try:
                # 独立进程从当前文件重建全部依赖，删除的子模块不能从父包属性继续存活。
                result = subprocess.run([sys.executable, '-I', '-X', 'utf8', os.path.join(HERE, 'collect.py')], check=False)
                if result.returncode:
                    print(f'{U.iso(t)} 项目采集退出码 {result.returncode}', file=sys.stderr)
            except BaseException as e:  # 采集出错不影响实时数据
                if isinstance(e, KeyboardInterrupt):
                    raise
                print(f"{U.iso(t)} 项目采集失败：{type(e).__name__} {e}", file=sys.stderr)
        time.sleep(max(1.0, EVERY_S - (time.time() - t)))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
