"""原生文件通知驱动按份采集；唯一 Observer 与历史 Recorder 共用此进程。
用法：python -I -X utf8 dashboard/tools/live.py [--loop]。
产出：各份 v3 数据与 dashboard-publication.json；仅由文件通知触发采集。
"""
import os
import sys
import threading
import time
from concurrent.futures import ThreadPoolExecutor

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, '..', '..', '.collab', 'tools'))
import collect
import datafile
from catalog import DATASETS, CURATED_NAMES
from monitor import history, util as U
from monitor.read import codex, repo

LOCK_FILE = U.REPO + '/output/.locks/live.lock'
CHECK_S = 15


def normalized(path):
    return os.path.normcase(os.path.abspath(path))


def within(path, root):
    return path == root or path.startswith(root + os.sep)


def existing_parent(path):
    path = os.path.abspath(path)
    while not os.path.isdir(path):
        parent = os.path.dirname(path)
        if parent == path:
            raise FileNotFoundError(path)
        path = parent
    return path


def single_instance():
    """持有第一个字节的独占锁，退出自动释放。"""
    import msvcrt
    os.makedirs(os.path.dirname(LOCK_FILE), exist_ok=True)
    stream = open(LOCK_FILE, 'a+b')
    try:
        stream.seek(0)
        msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
    except OSError:
        stream.close()
        return None
    return stream


def main():
    if '--loop' not in sys.argv:
        datafile.publish_app()
        collect.run({'live'})
        return
    lock = single_instance()
    if lock is None:
        print('live.py --loop 已经在跑，退出')
        return
    from watchdog.events import FileSystemEventHandler
    from watchdog.observers.read_directory_changes import WindowsApiObserver

    recorder = history.Recorder()
    changed, stop = threading.Event(), threading.Event()
    mutex = threading.Lock()
    dirty, valid, inflight = {}, set(), set()
    app_changes = set()
    refresh = {'sessions', 'worktrees'}
    checkout_paths, rollout_paths, gate_roots = set(), set(), set()
    schedules = {}
    common = U.git('rev-parse', '--git-common-dir').strip()
    if not common:
        raise ValueError('无法定位 Git common-dir')
    common = normalized(os.path.join(U.REPO, common))
    dashboard = normalized(os.path.dirname(HERE))
    docs = normalized(U.REPO + '/docs')
    claude = normalized(U.CLAUDE_PROJ)
    database = {normalized(U.CODEX_DB + suffix) for suffix in ('', '-wal', '-shm')}
    local = normalized(U.COLLAB + '/tools/monitor/local.json')
    patrol_schedule = normalized(U.COLLAB + '/tools/README.md')
    guard = normalized(U.COLLAB + '/tools/hooks/log/guard.jsonl')
    hook_stats = {normalized(U.COLLAB + '/tools/hooks/log/' + name)
                  for name in ('stats-guard.json', 'stats-steer.json')}
    code_roots = {normalized(U.REPO + '/' + name) for name in ('game', 'scripts', 'pipeline')}
    queue = normalized(U.COLLAB + '/status/施工状态.md')
    model = normalized(U.REPO + '/docs/workflow/WF-MODEL-模型分工.md')
    # 确切的 WF-MODEL 文件名由启动时索引一次；不在每次事件遍历文档树。
    import glob
    model_paths = {normalized(path) for path in glob.glob(U.REPO + '/docs/workflow/WF-MODEL-*.md')}
    model_paths.add(model)
    settings = {normalized(os.path.expanduser(U.LOCAL[side][key]))
                for side, key in (('codex', 'hooks'), ('claude', 'settings'))}
    notes = {normalized(U.COLLAB + '/notes/claude'), normalized(U.STATE + '/inbox_wait/delivered')}
    agent_log = normalized(U.STATE + '/agents/claude.jsonl')
    curated = normalized(dashboard + '/data/curated')
    event_root = normalized(U.STATE + '/dashboard-events')
    git_files = {normalized(os.path.join(common, tail)) for tail in
                 ('refs/remotes/origin/main', 'logs/refs/remotes/origin/main', 'packed-refs')}
    worktrees = normalized(os.path.join(common, 'worktrees'))
    script_roots = [normalized(U.REPO + '/' + row['path']) for row in U.LOCAL['scriptRoots']]
    excluded = {'__pycache__', 'godot-api'}

    def mark(names):
        now = time.monotonic()
        names = set(names)
        while True:
            expanded = names | {name for name, spec in DATASETS.items()
                                if spec.inputs & {'@' + source for source in names}}
            if expanded == names:
                break
            names = expanded
        with mutex:
            for name in names:
                # 已有首个事件的截止时间不后移；合批间隔只认每份数据的登记。
                dirty.setdefault(name, now + DATASETS[name].batch_s)
                valid.discard(name)
        changed.set()

    def sources(path, directory=False):
        path = normalized(path)
        parts = set(path.split(os.sep))
        if parts & excluded:
            return set()
        result = set()
        if path == queue:
            result.add('queue')
        if path == patrol_schedule:
            result.add('patrol:interval')
        if path == local:
            result.update(('local', 'sessions', 'settings'))
        if path in database:
            result.add('sessions')
            with mutex:
                refresh.add('sessions')
        if path in rollout_paths or path == agent_log or (within(path, claude) and path.endswith(('.jsonl', '.meta.json'))):
            result.add('sessions')
        if path == guard or path in hook_stats:
            result.add('hooks')
        if path in settings:
            result.add('settings')
        if path in model_paths:
            result.add('model')
        if path in git_files:
            result.add('git')
        if within(path, worktrees):
            result.add('worktrees')
            with mutex:
                refresh.add('worktrees')
        if path in checkout_paths or (directory and any(within(target, path) for target in checkout_paths)):
            result.add('shots')
        if any(within(path, root) for root in gate_roots) and (path.endswith('-result.json') or directory):
            result.add('gates')
        if any(within(path, root) for root in code_roots) and (path.endswith('.cs') or directory):
            result.add('code')
        if os.path.dirname(path) in notes and os.path.basename(path).endswith('.md') and '-author-' in os.path.basename(path):
            result.add('letters')
        if path in {normalized(curated + '/audit.js'), normalized(U.STATE + '/dashboard-ack.json')}:
            result.add('attention')
        if path == normalized(curated + '/notices.js'):
            result.add('notices')
        if within(path, docs) and (path.endswith('.md') or path == normalized(docs + '/ROUTES.json') or directory):
            result.add('docs')
        if path == normalized(dashboard + '/index.html') or within(path, normalized(dashboard + '/app')) or within(path, normalized(dashboard + '/vendor')):
            result.add('app')
            with mutex:
                app_changes.add(path)
        if any(within(path, root) for root in script_roots):
            # 自动生成状态、来信、数据不是脚本变化，避免自产事件循环。
            if path.endswith(('.py', '.js', '.cjs', '.mjs', '.ps1', '.cmd', '.sh', '.cs')) or os.path.basename(path) == 'readme.md':
                result.add('scripts')
        if os.path.dirname(path) == event_root and path.endswith('.jsonl'):
            result.add('history:' + os.path.basename(path)[:-6])
        if path == normalized(event_root + '/checkpoint.json'):
            result.add('history:checkpoint')
        return result

    class Changed(FileSystemEventHandler):
        def on_any_event(self, event):
            if event.event_type not in ('created', 'deleted', 'modified', 'moved'):
                return
            if event.is_directory and event.event_type == 'modified':
                return
            for path in (event.src_path, getattr(event, 'dest_path', None)):
                if not path:
                    continue
                historical = recorder.mark_dirty(path)
                touched = sources(path, event.is_directory and event.event_type != 'modified')
                names = {name for name, spec in DATASETS.items() if spec.inputs & touched}
                if names:
                    mark(names)
                # 父目录被创建/替换后重设最小可用盯点，原子替换仍精确匹配src/dest。
                if event.is_directory and event.event_type != 'modified':
                    with mutex:
                        refresh.add('watches')
                if historical or touched or event.is_directory:
                    changed.set()

    observer = WindowsApiObserver()  # 与 Recorder 共用唯一的原生通知实例。
    handler = Changed()

    def configure_watches(update):
        nonlocal checkout_paths, rollout_paths, gate_roots
        if 'sessions' in update:
            threads = codex.threads(include_archived=False)
            known = {U.MAIN_ID}
            while True:
                descendants = {row['id'] for row in threads if row['parent'] in known}
                expanded = known | descendants
                if expanded == known:
                    break
                known = expanded
            rollout_paths = {normalized(row['path']) for row in threads if row['id'] in known and row.get('path')}
        if 'worktrees' in update:
            checkouts = repo.checkouts()
            checkout_paths = {normalized(root + '/output/shots/' + name)
                              for root in checkouts for name in ('总览.jpg', 'ui/总览.jpg')}
            gate_roots = {normalized(root + '/output/build/gates') for root in checkouts}
        # 缺失目标只盯最近父目录，创建通知到达后再缩到实际目录。
        wanted = [(path, recursive) for path, recursive in recorder.watch_paths()]
        wanted += [(U.CLAUDE_PROJ, True), (docs, True), (dashboard + '/app', True), (dashboard + '/vendor', True),
                   (worktrees, True), (event_root, False), (U.COLLAB + '/tools/hooks/log', False)]
        wanted += [(root, True) for root in script_roots if os.path.isdir(root)]
        wanted += [(root, True) for root in code_roots | gate_roots]
        exact = set(rollout_paths) | checkout_paths | database | git_files | settings | model_paths
        exact.update(root for root in script_roots if not os.path.isdir(root))
        exact.update((queue, local, patrol_schedule, guard, agent_log, dashboard + '/index.html',
                      U.STATE + '/dashboard-ack.json'))
        exact.update(curated + '/' + name + '.js' for name in CURATED_NAMES)
        wanted += [(os.path.dirname(path), False) for path in exact]
        wanted += [(path, False) for path in notes]
        roots = {}
        for path, recursive in wanted:
            existing = normalized(existing_parent(path))
            # 缺失的多级父路径必须递归监听创建，成功后马上缩回指定范围。
            recursive = recursive or existing != normalized(path)
            roots[existing] = roots.get(existing, False) or recursive
        minimum = {}
        for path in sorted(roots, key=lambda item: (len(item), item)):
            if not any(recursive and within(path, parent) for parent, recursive in minimum.items()):
                minimum[path] = roots[path]
        for path, recursive in list(schedules):
            if minimum.get(path) != recursive:
                observer.unschedule(schedules.pop((path, recursive)))
        for path, recursive in minimum.items():
            if (path, recursive) not in schedules:
                schedules[path, recursive] = observer.schedule(handler, path, recursive=recursive)

    lanes = tuple(dict.fromkeys(spec.lane for spec in DATASETS.values()))
    workers = {lane: ThreadPoolExecutor(max_workers=1, thread_name_prefix='dashboard-' + lane)
               for lane in lanes}
    jobs = {}
    recorder_thread = threading.Thread(target=recorder.run, args=(stop,), name='dashboard-history', daemon=True)
    try:
        configure_watches(refresh)
        refresh.clear()
        observer.start()
        recorder_thread.start()
        datafile.publish_app()
        mark({name for name, spec in DATASETS.items() if spec.kind != 'app'})
        next_check = time.monotonic() + CHECK_S
        # 时间依赖只在真实到期点安排一次采样；不是文件轮询。
        next_expiry = {}
        periodic = {name: time.monotonic() + spec.refresh_s for name, spec in DATASETS.items()
                    if spec.refresh_s is not None}
        while True:
            changed.clear()
            with mutex:
                updates = set(refresh)
                refresh.clear()
            if updates:
                configure_watches(updates)
            for lane, (future, names) in list(jobs.items()):
                if not future.done():
                    continue
                del jobs[lane]
                try:
                    done, failed = future.result()
                except Exception as error:
                    done, failed = set(), set(names)
                    print(f'{U.iso(time.time())} {lane}采集失败：{type(error).__name__}: {error}', file=sys.stderr)
                with mutex:
                    inflight.difference_update(names)
                    valid.update(done - set(dirty))
                    valid.difference_update(failed)
                dependencies = {'@' + name for name in done | failed}
                mark({name for name, spec in DATASETS.items() if spec.inputs & dependencies})
                if any(DATASETS[name].sample or DATASETS[name].expiry for name in done | failed):
                    # 下一次过时/离线阈值从同一份已采事实导出，不补采第二份。
                    next_expiry = collect.next_expiry()
            now = time.monotonic()
            expired = {name for name, due in next_expiry.items() if time.time() >= due}
            if expired:
                mark(({name for name, spec in DATASETS.items() if 'sessions' in spec.inputs}
                      if 'sessions' in expired else set()) | (expired - {'sessions'}))
                next_expiry = {name: due for name, due in next_expiry.items() if name not in expired}
            for name, due in periodic.items():
                if now >= due:
                    mark({name})
                    periodic[name] = now + DATASETS[name].refresh_s
            if now >= next_check:
                with mutex:
                    confirmed = valid - set(dirty) - inflight
                datafile.check(confirmed)
                next_check = now + CHECK_S
            for lane in lanes:
                if lane in jobs:
                    continue
                with mutex:
                    names = {name for name, due in dirty.items() if due <= now and DATASETS[name].lane == lane
                             and not any(dependency[1:] in dirty or dependency[1:] in inflight
                                         for dependency in DATASETS[name].inputs if dependency.startswith('@'))}
                    for name in names:
                        del dirty[name]
                    inflight.update(names)
                    app_job = any(DATASETS[name].kind == 'app' for name in names)
                    paths = set(app_changes) if app_job else set()
                    if app_job:
                        app_changes.clear()
                if names:
                    future = workers[lane].submit(collect.run, names, recorder, paths)
                    future.add_done_callback(lambda _: changed.set())
                    jobs[lane] = future, names
            with mutex:
                deadlines = [due for name, due in dirty.items()
                             if DATASETS[name].lane not in jobs
                             and not any(dependency[1:] in dirty or dependency[1:] in inflight
                                         for dependency in DATASETS[name].inputs if dependency.startswith('@'))]
            deadline = min([next_check] + list(periodic.values()) + deadlines)
            if next_expiry:
                deadline = min(deadline, time.monotonic() + max(0, min(next_expiry.values()) - time.time()))
            changed.wait(max(.01, deadline - time.monotonic()))
    finally:
        stop.set()
        recorder.wake()
        observer.stop()
        if observer.is_alive():
            observer.join()
        for worker in workers.values():
            worker.shutdown(wait=True, cancel_futures=True)
        if recorder_thread.is_alive():
            recorder_thread.join(timeout=5)
        lock.close()


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    main()
