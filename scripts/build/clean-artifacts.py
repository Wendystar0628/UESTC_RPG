"""清理仓库这边的一次性产物（WF-RULE §13），默认只列出，加 --delete 才删。覆盖五类：
  1. output/ 里之后主线上已有 N 次提交（默认 20）的项：整项过期整项删，没过期的目录逐层往里查（最多 3 层），
     所以常被写的 output/build/ 里旧的日志、测试结果、核心专项目录也能清掉；
  2. output/cache/ 里烘焙缓存之外的目录（一次性的缓存副本，同样按提交轮次），以及烘焙缓存的旧格式目录
     （同一张图下 *-tbin-N 只留 N 最大的）；
  3. 还登记着的工作树（如截图工作树）里的 output/：与主检出的 output/ 同一套规则（1、2 和下面的保护），工作树本身只列出，不删；
  4. build/ 下的导出产物（如 build/windows）：之后主线上已有 N 次提交的，和 output/ 一样；
  5. 已删工程留下的 bin/、obj/：所在目录里已经没有 .csproj；git 已失效的工作树登记（git worktree prune）。
删除时持有构建锁（与构建、导入、门禁、预烘、截图同一把 output/.locks/build.lock），持锁后重算再删；
锁被持有就本次一项不删，免得删到正在写的产物。只列出时不碰锁。
保护：观察台里未结的审计项和通知引用的 output/ 文件不删，连同它们所在的目录里的其余项也不删（作者还要看）；
还在的任务单（.collab/tasks，结案即删）引用的路径不删，引用的是目录就连里面的内容，只保被引用的这一项，
它的祖先目录里没被引用且已过期的兄弟照常可删；施工队列里未结行对应的 output/行号目录及其后缀目录也保留（行号可去短横线）。
按提交轮次而不按时间算：不开发就没有提交，什么都不会过期。

用法：
  python -I -X utf8 scripts/build/clean-artifacts.py                列出可删的项和大小
  python -I -X utf8 scripts/build/clean-artifacts.py --delete       删掉它们
  python -I -X utf8 scripts/build/clean-artifacts.py --commits 10   改轮次
产出：无（列出一次性产物；--delete 删除过期项并清理失效工作树登记）。
"""
import contextlib
import ctypes
import json
import os
import re
import shutil
import stat
import subprocess
import sys
from ctypes import wintypes

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
OUT = os.path.join(ROOT, 'output')
EXPORTS = os.path.join(ROOT, 'build')  # 导出产物（build/windows 等）
BUILD_LOCK = os.path.join(OUT, '.locks', 'build.lock')
KEEP = {'.locks', 'cache', 'shots', 'pipeline-wheels', 'posters', 'tools', 'prebake_game'}  # 构建锁、缓存（下面单独查）、固定机位截图、管线安装包、交给作者的海报、本机诊断工具（dotnet-trace 等）、收口预烘日志（巡逻据它判上次预烘）
CACHE_KEEP = {'bake'}  # output/cache 下长期保留的只有烘焙缓存
BUILD_ROOTS = ('game', 'scripts', 'pipeline')  # 找已删工程的 bin/obj
DEPTH = 3
COMMITS = 20
FORMAT_DIR = re.compile(r'^(.+)-tbin-(\d+)$')
TASK_LINK = re.compile(r'output/[\w./-]+', re.ASCII)  # 任务单里写的产物路径；只取 ASCII 段，中文名截在前缀
START_GAME_KEEP = tuple(os.path.join('start_game', name) for name in ('build.succeeded', 'build.log', 'import.log'))


def git(*args):
    return subprocess.run(['git', '-C', ROOT, *args], capture_output=True, text=True).stdout


def cutoff(commits):
    """往前数第 commits 次提交的时间；提交不够这么多时返回 None（什么都不算过期）。"""
    out = git('log', '-n', str(commits), '--format=%ct').split()
    return float(out[-1]) if len(out) >= commits else None


def stamp(p):
    """最后写入或复制进来的时刻：复制会保留旧的修改时间，Windows 的 ctime 是在本机建立的时刻。"""
    return max(os.path.getmtime(p), os.path.getctime(p))


def newest(p):
    if os.path.isfile(p):
        return stamp(p)
    times = [stamp(os.path.join(r, f)) for r, _, fs in os.walk(p) for f in fs]
    return max(times) if times else stamp(p)


def size(p):
    if os.path.isfile(p):
        return os.path.getsize(p)
    return sum(os.path.getsize(os.path.join(r, f)) for r, _, fs in os.walk(p) for f in fs)


def guard(p, keep):
    """p 与受保护路径的关系：(整项受保护, 是受保护路径的祖先容器)。keep 里是 normcase 后的绝对路径。"""
    n = os.path.normcase(p)
    return (any(n == k or n.startswith(k + os.sep) for k in keep),
            any(k.startswith(n + os.sep) for k in keep))


def aged(directory, before, depth, skip=(), keep=()):
    """directory 下过期的项：整项过期就整项算，否则是目录就往里再查一层。
    被引用的路径（及其里面）整项不动；含被引用路径的祖先目录只当容器、不整项算，往里查它没被引用的兄弟。"""
    out = []
    for name in sorted(os.listdir(directory)):
        p = os.path.join(directory, name)
        if name in skip:
            continue
        full, container = guard(p, keep)
        if full:
            continue
        if newest(p) < before and not container:
            out.append(p)
        elif os.path.isdir(p) and depth > 1:
            out += aged(p, before, depth - 1, keep=keep)
    return out


def old_formats(out_root):
    """out_root 烘焙缓存里同一张图下的旧格式目录：*-tbin-N 只留 N 最大的。"""
    bake = os.path.join(out_root, 'cache', 'bake')
    out = []
    maps = [m for m in sorted(os.listdir(bake)) if os.path.isdir(os.path.join(bake, m))] if os.path.isdir(bake) else []
    for m in maps:
        found = [(FORMAT_DIR.match(d), d) for d in os.listdir(os.path.join(bake, m))]
        found = [(int(f.group(2)), d) for f, d in found if f]
        out += [os.path.join(bake, m, d) for n, d in found if n < max(n for n, _ in found)]
    return out


def orphan_builds():
    """已删工程的 bin/、obj/：所在目录里没有 .csproj。"""
    out = []
    for top in BUILD_ROOTS:
        for r, ds, fs in os.walk(os.path.join(ROOT, top)):
            ds[:] = [d for d in ds if d != '.godot']
            for d in [d for d in ds if d in ('bin', 'obj')]:
                if not any(f.endswith('.csproj') for f in fs):
                    out.append(os.path.join(r, d))
            ds[:] = [d for d in ds if d not in ('bin', 'obj')]
    return out


def references():
    """在用的引用（output/ 开头的相对路径，对每个 output 根各自解析）：
    (观察台未结的审计项与通知的链接, 任务单里写的路径, 施工队列未结行的行号匹配式或 None)。
    观察台数据是 dashboard/data/curated/*.js 里 Dash.put(名, {...}); 的本地数据；
    任务单在 .collab/tasks，结案即删，单子或队列行在就算在用。"""
    audit = []
    for name in ('audit', 'notices'):
        f = os.path.join(ROOT, 'dashboard', 'data', 'curated', name + '.js')
        if not os.path.isfile(f):
            continue
        text = open(f, encoding='utf-8').read().strip()
        start, end = text.find('{'), text.rfind('}')
        if start < 0 or end < start:
            continue
        for item in json.loads(text[start:end + 1]).get('items', []):
            link = item.get('link') or ''
            if item.get('status') != 'done' and link.startswith('output/'):
                audit.append(link)
    pattern = None
    queue = os.path.join(ROOT, '.collab', 'status', '施工状态.md')
    text = open(queue, encoding='utf-8').read()
    if '## 施工队列' in text:
        part = text.split('## 施工队列', 1)[1].split('\n## ', 1)[0]
        columns = {}
        ids = set()
        for line in part.splitlines():
            if not line.strip().startswith('|'):
                continue
            cells = [cell.strip() for cell in line.strip().strip('|').split('|')]
            if cells[0] == '号':
                columns = {name: i for i, name in enumerate(cells)}
                continue
            if '号' not in columns or columns['号'] >= len(cells):
                continue
            row_id = cells[columns['号']]
            if re.fullmatch(r'[A-Z]-\d+[a-z]?', row_id):
                ids.update((row_id, row_id.replace('-', '')))
        if ids:
            pattern = re.compile(r'^(?:' + '|'.join(re.escape(i) for i in sorted(ids)) + r')(?=$|[^A-Za-z0-9])')
    tasks = []
    for r, _, fs in os.walk(os.path.join(ROOT, '.collab', 'tasks')):
        for f in (f for f in fs if f.endswith('.md')):
            tasks += [link.rstrip('./') for link in TASK_LINK.findall(open(os.path.join(r, f), encoding='utf-8').read())]
    return audit, tasks, pattern


def keep_set(out_root, refs):
    """out_root 里受保护的路径（normcase 后的绝对路径）：被引用的路径本身及其里面；审计项与通知引用的文件连同所在目录。"""
    audit, tasks, pattern = refs
    base = os.path.dirname(out_root)  # 引用以 output/ 开头，相对 output 根的上一级
    keep = {os.path.join(out_root, name) for name in START_GAME_KEEP}
    for link in audit:
        path = os.path.normpath(os.path.join(base, link))
        keep.add(path)
        if os.path.dirname(path) != out_root:
            keep.add(os.path.dirname(path))
    keep.update(os.path.normpath(os.path.join(base, link)) for link in tasks)
    if pattern:
        keep.update(os.path.join(out_root, name) for name in os.listdir(out_root)
                    if pattern.match(name) and os.path.isdir(os.path.join(out_root, name)))
    return {os.path.normcase(k) for k in keep}


@contextlib.contextmanager
def build_lock():
    """持有构建锁期间执行，给出是否拿到：与 scripts/lib/common.ps1 的 Enter-UESTCBuildLock 同一协议
    （不共享地打开主检出的 output/.locks/build.lock，所有工作树共用这一把）；被持有时给出 False，不等。"""
    os.makedirs(os.path.dirname(BUILD_LOCK), exist_ok=True)
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    kernel.CreateFileW.restype = wintypes.HANDLE
    kernel.CreateFileW.argtypes = (wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD, wintypes.LPVOID,
                                   wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE)
    handle = kernel.CreateFileW(BUILD_LOCK, 0xC0000000, 0, None, 4, 0x80, None)  # 读写、不共享、OPEN_ALWAYS
    if handle == wintypes.HANDLE(-1).value:
        error = ctypes.get_last_error()
        if error not in (32, 33):  # 共享冲突、锁冲突以外的错照报
            raise ctypes.WinError(error)
        yield False
        return
    try:
        yield True
    finally:
        kernel.CloseHandle(handle)


def exports(before):
    """build/ 下之后主线上已有 N 次提交的导出产物。"""
    if before is None or not os.path.isdir(EXPORTS):
        return []
    return aged(EXPORTS, before, 1)


def stale(commits, trees):
    """返回可删的绝对路径：主检出与登记着的工作树里的 output/，加 build/ 旧导出与已删工程的 bin/obj，去掉在用的引用。"""
    refs = references()
    before = cutoff(commits)
    out = orphan_builds() + exports(before)
    roots = [OUT] + [os.path.join(t['path'], 'output') for t in trees if not t['prunable']]
    for root in (r for r in roots if os.path.isdir(r)):
        keep = keep_set(root, refs)
        out += [p for p in old_formats(root) if guard(p, keep) == (False, False)]
        if before is None:
            continue
        out += aged(root, before, DEPTH, skip=KEEP, keep=keep)
        cache = os.path.join(root, 'cache')
        if os.path.isdir(cache):
            out += aged(cache, before, 1, skip=CACHE_KEEP, keep=keep)
    return sorted(set(out))


def worktrees():
    """登记着的工作树（主检出除外）：路径、是否已失效（目录没了，prune 会清掉）、最后动过的时间。"""
    out = []
    for block in git('worktree', 'list', '--porcelain').strip().split('\n\n'):
        lines = block.splitlines()
        path = next((l[9:] for l in lines if l.startswith('worktree ')), '')
        if not path or os.path.normcase(os.path.abspath(path)) == os.path.normcase(ROOT):
            continue
        prunable = any(l.startswith('prunable') for l in lines) or not os.path.isdir(path)
        touched = 0.0
        if not prunable:
            # 最后动过：改动文件的最新修改时间，没有改动就取检出时间（.git 文件）与 HEAD 提交时间
            changed = [os.path.join(path, l[3:].strip().strip('"').split(' -> ')[-1])
                       for l in git('-C', path, 'status', '--porcelain').splitlines()]
            times = [os.path.getmtime(f) for f in changed if os.path.exists(f)]
            head = git('-C', path, 'log', '-1', '--format=%ct').strip()
            dot = os.path.join(path, '.git')
            times += [float(head)] if head else []
            times += [os.path.getmtime(dot)] if os.path.exists(dot) else []
            touched = max(times) if times else 0.0
        out.append({'path': path, 'prunable': prunable, 'touched': touched})
    return out


def remove(p):
    """删文件或目录；git 对象这类只读文件先去掉只读属性再删。"""
    def writable(func, path, *_):
        os.chmod(path, stat.S_IWRITE)
        func(path)
    if os.path.isdir(p):
        shutil.rmtree(p, onexc=writable)
    elif os.path.exists(p):
        try:
            os.remove(p)
        except PermissionError:
            writable(os.remove, p)


def main(argv):
    commits = int(argv[argv.index('--commits') + 1]) if '--commits' in argv else COMMITS
    trees = worktrees()
    items = stale(commits, trees)
    mb = sum(size(p) for p in items) / 1024 / 1024
    for p in items:
        inside = os.path.normcase(p).startswith(os.path.normcase(ROOT) + os.sep)
        print((os.path.relpath(p, ROOT) if inside else p).replace(chr(92), '/'))
    print('可删 %d 项，共 %.1f MB（output 与 build 按之后主线上已有 %d 次提交算）' % (len(items), mb, commits))
    for t in trees:
        print(('失效的工作树登记（--delete 时 prune）：' if t['prunable'] else '还登记着的工作树（确认不用了再 git worktree remove）：') + t['path'])
    if '--delete' not in argv:
        return 0
    with build_lock() as held:
        if not held:
            print('构建锁被持有（构建、导入、门禁、预烘或截图在跑），本次不删，等锁空了再跑')
            return 1
        items = stale(commits, worktrees())  # 持锁后重算：列出之后生产者写进的不会被删
        for p in items:
            remove(p)
        git('worktree', 'prune')
        for r, ds, fs in sorted(os.walk(OUT), key=lambda t: -len(t[0])):  # 删掉清空后的目录
            if r != OUT and not ds and not fs and os.path.relpath(r, OUT).split(os.sep)[0] not in KEEP:
                os.rmdir(r)
    print('已删 %d 项' % len(items))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
