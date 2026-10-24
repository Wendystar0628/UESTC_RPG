"""串行维护性能派生、索引、历史与保留范围；作者原始根只读。
用法：由 analyze.py 调用；输入 output/perf 和作者 perf 根，产出 index.json/history.jsonl 并按条数修剪 AI 数据。
"""
import contextlib
import ctypes
import json
import os
import shutil
import subprocess
import time
from collections import defaultdict
from pathlib import Path
from perf_model import CALC, at, manifest_path, read_json

AI_BATCHES_KEEP = 10
HISTORY_PER_GROUP = 200
DIAGNOSTIC_BYTES_KEEP = 2 * 1024 ** 3


@contextlib.contextmanager
def exclusive(path, timeout=0, create=False, allow_delete=False):
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
            handle = kernel.CreateFileW(str(path), 0x80000000 | (0x40000000 if create else 0), 4 if allow_delete else 0,
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


def git(root, *args):
    result = subprocess.run(['git', '-C', str(root), *args], capture_output=True, text=True, encoding='utf-8')
    return result.returncode, result.stdout.strip()


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


def nearest_run_path(directory, runs_root):
    directory = Path(directory).resolve()
    for candidate in (directory, *directory.parents):
        run = candidate / 'run.json'
        if run.is_file():
            return run
        if candidate == runs_root.resolve() or candidate.parent == candidate:
            break
    return None


def nearest_run(directory, runs_root):
    path = nearest_run_path(directory, runs_root)
    return (read_json(path), path) if path else ({}, None)


def comparison_key(manifest, run):
    graphics, machine = manifest.get('graphics') or {}, manifest.get('machine') or {}
    scene = run.get('scene') or run.get('scenario') or manifest.get('scene')
    key = dict(scene=scene, machine=machine.get('id'), configuration=at(manifest, 'build.configuration'),
               preset=graphics.get('preset'), effectiveGraphics=graphics.get('effective'),
               outputResolution=[at(manifest, 'display.windowWidthPx'), at(manifest, 'display.windowHeightPx')],
               internalResolution=[graphics.get('internalWidthPx'), graphics.get('internalHeightPx')],
               pacing=manifest.get('pacing'), graphicsApi=machine.get('api'))
    missing = [k for k, value in key.items() if value is None or (isinstance(value, list) and None in value)]
    labels = dict(scene='场景', machine='机器', configuration='构建配置', preset='预设', effectiveGraphics='生效画质',
                  outputResolution='输出分辨率', internalResolution='内部渲染分辨率', pacing='帧节奏', graphicsApi='图形接口')
    return key, labels, missing


def basic_state(manifest, streams, run, missing, schema):
    reasons = []
    if manifest.get('schema') != schema:
        return 'unsupported', ['schema 不是当前版本']
    if manifest.get('status') != 'closed':
        return 'partial', ['记录锁已释放但会话未完成关闭；未解析未闭合的原始流']
    if streams is None:
        return 'unsupported', ['当前 schema 的原始流不可用']
    if manifest.get('origin') == 'play':
        return 'evidence', ['作者会话只作证据，不作 A/B 基线']
    if run.get('status') == 'invalid':
        return 'invalid', run.get('reasons') or ['采集前提不满足']
    if not manifest.get('complete'):
        reasons.append('记录未完整结束或有丢行')
    if not streams['frames']:
        reasons.append('没有逐帧记录')
    if run.get('exitCode') != 0 or run.get('timedOut') is not False:
        reasons.append('AI 运行关闭事实不足或失败')
    coverage = run.get('coverage') or {}
    covered = bool(coverage) and all(isinstance(item, dict) and isinstance(item.get('observed'), (int, float))
                                   and isinstance(item.get('required'), (int, float))
                                   and item['observed'] >= item['required'] for item in coverage.values())
    if run.get('status') != 'valid' or not covered:
        reasons.append('采集前提或场景覆盖未确认')
    if missing:
        reasons.append('比较键缺值: ' + ', '.join(missing))
    if at(manifest, 'build.dirty') is not False:
        reasons.append('构建含未提交改动或干净状态未知')
    diagnostics = at(manifest, 'run.diagnostics')
    if diagnostics or run.get('diagnostic'):
        return 'evidence', ['诊断采集不进入基线或常规均值']
    return ('partial', reasons) if reasons else ('candidate', [])


def rebuild_states(entries, root):
    groups = defaultdict(list)
    for entry in entries:
        entry['status'] = entry['baseStatus']
        entry['statusReasons'] = list(entry.get('baseReasons', []))
        groups[json.dumps([entry['comparisonKey'], entry['scope'], entry['calc']], sort_keys=True)].append(entry)
    environment = {}
    for entry in sorted(entries, key=lambda e: e.get('endedAt') or ''):
        machine = entry['comparisonKey'].get('machine')
        if machine and entry.get('environmentSignature'):
            environment[machine] = entry['environmentSignature']
    for rows in groups.values():
        valid = []
        for entry in rows:
            if entry['baseStatus'] != 'candidate':
                continue
            commit = at(entry, 'build.commit')
            if not commit or git(root, 'merge-base', '--is-ancestor', commit, 'HEAD')[0] != 0:
                entry['status'], entry['statusReasons'] = 'partial', ['构建提交不在当前主线或身份未知']
            else:
                valid.append(entry)
        valid.sort(key=lambda e: e.get('endedAt') or '')
        for entry in valid:
            entry['status'] = 'superseded'
        if not valid:
            continue
        current = valid[-1]
        current['status'] = 'current'
        commit = at(current, 'build.commit')
        reasons = []
        code, changes = git(root, 'diff', '--name-only', commit, 'HEAD', '--', 'game/')
        dirty_code, dirty_changes = git(root, 'status', '--porcelain', '--', 'game/')
        if code != 0 or changes or dirty_code != 0 or dirty_changes:
            reasons.append('构建之后 game/ 已变更')
        newest = environment.get(current['comparisonKey']['machine'])
        if newest != current.get('environmentSignature'):
            reasons.append('最近采集的驱动、系统或引擎版本已变化')
        if reasons:
            current['status'], current['statusReasons'] = 'stale', reasons
    return entries


def history_group(row):
    return 'play' if row['origin'] == 'play' else json.dumps([row['comparisonKey'], row['scope']], sort_keys=True)


def history_update(path, summaries):
    rows = []
    if path.exists():
        rows = [json.loads(line) for line in path.read_text(encoding='utf-8').splitlines() if line.strip()]
    # Id + calc is one analysis; replace after a changed source/budget, never append duplicates.
    unique = {(row['id'], row['calc']): row for row in rows}
    unique.update({(row['id'], row['calc']): row for row in summaries})
    groups = defaultdict(list)
    for row in unique.values():
        groups[history_group(row)].append(row)
    kept = []
    for group in groups.values():
        kept.extend(sorted(group, key=lambda r: r.get('endedAt') or '')[-HISTORY_PER_GROUP:])
    pending = path.with_name(path.name + '.pending')
    pending.write_text(''.join(json.dumps(row, ensure_ascii=False, allow_nan=False) + '\n' for row in sorted(kept, key=lambda r: r.get('endedAt') or '')), encoding='utf-8')
    pending.replace(path)


def inside(path, parent):
    if Path(path).is_symlink():
        return False
    path, parent = Path(path).resolve(), Path(parent).resolve()
    return path != parent and path.is_relative_to(parent)


def unlocked_tree(directory):
    leases = contextlib.ExitStack()
    try:
        if any(not (path.parent / '.lock').is_file() for path in Path(directory).rglob('manifest.pending.json')):
            leases.close()
            return None
        for path in Path(directory).rglob('.lock'):
            if not leases.enter_context(exclusive(path, allow_delete=True)):
                leases.close()
                return None
        return leases
    except BaseException:
        leases.close()
        raise


def diagnostic_attachments(runs):
    """只枚举诊断清单明确登记且在本目录内的非原始文件。"""
    attachments = set()
    raw = {'manifest.json', 'manifest.pending.json', '.lock', 'frames.csv', 'samples.csv', 'hw.csv', 'hwfast.csv',
           'events.jsonl', 'run.json', 'diagnostics.json'}
    for record in Path(runs).rglob('diagnostics.json'):
        for relative in read_json(record).get('attachments', []):
            path = (record.parent / relative).resolve()
            if path.name not in raw and inside(path, record.parent) and path.is_file():
                attachments.add(path)
    return sorted(attachments)


def prune(out, entries):
    out, runs = Path(out).resolve(), (Path(out) / 'runs').resolve()
    removed = []
    if runs.exists():
        batches = sorted((p for p in runs.iterdir() if p.is_dir() and not p.is_symlink()),
                         key=lambda p: max((q.stat().st_mtime_ns for name in ('manifest.json', 'manifest.pending.json')
                                            for q in p.rglob(name)), default=p.stat().st_mtime_ns), reverse=True)
        protected = set(batches[:AI_BATCHES_KEEP])
        for entry in entries:
            source = Path(entry['source']).resolve()
            if entry['status'] in ('current', 'stale') and inside(source, runs):
                protected.add(runs / source.relative_to(runs).parts[0])
        for batch in batches:
            if batch in protected or not inside(batch, runs):
                continue
            lease = unlocked_tree(batch)
            if lease is not None:
                try:
                    manifests = [p for name in ('manifest.json', 'manifest.pending.json') for p in batch.rglob(name)]
                    if manifests:
                        shutil.rmtree(batch)
                        removed.append(str(batch))
                finally:
                    lease.close()
    derived = out / 'derived'
    if derived.exists():
        for directory in derived.iterdir():
            if not directory.is_dir() or not inside(directory, derived):
                continue
            summary_path = directory / 'summary.json'
            if summary_path.exists() and not manifest_path(Path(read_json(summary_path)['source'])).is_file():
                shutil.rmtree(directory)
                removed.append(str(directory))
    attachments = diagnostic_attachments(runs)
    total = sum(p.stat().st_size for p in attachments)
    for path in sorted(attachments, key=lambda p: p.stat().st_mtime_ns):
        if total <= DIAGNOSTIC_BYTES_KEEP:
            break
        total -= path.stat().st_size
        path.unlink()
        removed.append(str(path))
    return removed
