"""清理仓库这边的一次性产物（WF-RULE §13），默认只列出，加 --delete 才删。覆盖四类：
  1. output/ 里之后主线上已有 N 次提交（默认 20）的项：整项过期整项删，没过期的目录逐层往里查（最多 3 层），
     所以常被写的 output/build/ 里旧的日志、测试结果、核心专项目录也能清掉；
  2. output/cache/ 里烘焙缓存之外的目录（一次性的缓存副本，同样按提交轮次），以及烘焙缓存的旧格式目录
     （同一张图下 *-tbin-N 只留 N 最大的）；
  3. 已删工程留下的 bin/、obj/：所在目录里已经没有 .csproj；
  4. git 已失效的工作树登记（git worktree prune）；还登记着的工作树只列出，不删。
观察台里未结的审计项和通知引用的 output/ 文件不删，连同它们所在的目录（作者还要看）；还在的任务单（.collab/tasks，结案即删）引用的路径不删，引用的是目录就连里面的内容。
按提交轮次而不按时间算：不开发就没有提交，什么都不会过期。

用法：
  python -I -X utf8 scripts/build/clean-artifacts.py                列出可删的项和大小
  python -I -X utf8 scripts/build/clean-artifacts.py --delete       删掉它们
  python -I -X utf8 scripts/build/clean-artifacts.py --commits 10   改轮次
"""
import json
import os
import re
import shutil
import stat
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
OUT = os.path.join(ROOT, 'output')
KEEP = {'.locks', 'cache', 'shots', 'pipeline-wheels', 'posters', 'tools', 'prebake_game'}  # 构建锁、缓存（下面单独查）、固定机位截图、管线安装包、交给作者的海报、本机诊断工具（dotnet-trace 等）、收口预烘日志（巡逻据它判上次预烘）
CACHE_KEEP = {'bake'}  # output/cache 下长期保留的只有烘焙缓存
BUILD_ROOTS = ('game', 'scripts', 'pipeline')  # 找已删工程的 bin/obj
DEPTH = 3
COMMITS = 20
FORMAT_DIR = re.compile(r'^(.+)-tbin-(\d+)$')
TASK_LINK = re.compile(r'output/[\w./-]+', re.ASCII)  # 任务单里写的产物路径；只取 ASCII 段，中文名截在前缀
START_GAME_KEEP = {os.path.join(OUT, 'start_game', name) for name in
                   ('build.succeeded', 'build.log', 'import.log')}


def git(*args):
    return subprocess.run(['git', '-C', ROOT, *args], capture_output=True, text=True).stdout


def cutoff(commits):
    """往前数第 commits 次提交的时间；提交不够这么多时返回 None（什么都不算过期）。"""
    out = git('log', '-n', str(commits), '--format=%ct').split()
    return float(out[-1]) if len(out) >= commits else None


def newest(p):
    if os.path.isfile(p):
        return os.path.getmtime(p)
    times = [os.path.getmtime(os.path.join(r, f)) for r, _, fs in os.walk(p) for f in fs]
    return max(times) if times else os.path.getmtime(p)


def size(p):
    if os.path.isfile(p):
        return os.path.getsize(p)
    return sum(os.path.getsize(os.path.join(r, f)) for r, _, fs in os.walk(p) for f in fs)


def aged(directory, before, depth, skip=(), keep=()):
    """directory 下过期的项：整项过期就整项算，否则是目录就往里再查一层。"""
    out = []
    for name in sorted(os.listdir(directory)):
        p = os.path.join(directory, name)
        if name in skip:
            continue
        protected = any(os.path.normcase(k) == os.path.normcase(p) or
                        os.path.normcase(k).startswith(os.path.normcase(p) + os.sep) for k in keep)
        if newest(p) < before and not protected:
            out.append(p)
        elif os.path.isdir(p) and depth > 1:
            out += aged(p, before, depth - 1, keep=keep)
    return out


def old_formats():
    """烘焙缓存里同一张图下的旧格式目录：*-tbin-N 只留 N 最大的。"""
    bake = os.path.join(OUT, 'cache', 'bake')
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


def linked():
    """在用的引用指向的 output/ 下文件（绝对路径）：观察台未结的审计项与通知（dashboard/data/curated/*.js 是 Dash.put(名, {...}); 的本地数据），以及 .collab/tasks 下还在的任务单（结案即删，单子在就算在用）。"""
    out = set()
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
                out.add(os.path.normpath(os.path.join(ROOT, link)))
    tasks = os.path.join(ROOT, '.collab', 'tasks')
    for r, _, fs in os.walk(tasks):
        for f in (f for f in fs if f.endswith('.md')):
            for link in TASK_LINK.findall(open(os.path.join(r, f), encoding='utf-8').read()):
                out.add(os.path.normpath(os.path.join(ROOT, link.rstrip('./'))))
    return out


def stale(commits=COMMITS):
    """返回可删的绝对路径（四类合在一起），去掉观察台还引用着的。"""
    out = old_formats() + orphan_builds()
    before = cutoff(commits)
    if before is not None and os.path.isdir(OUT):
        out += aged(OUT, before, DEPTH, skip=KEEP, keep=START_GAME_KEEP)
        cache = os.path.join(OUT, 'cache')
        if os.path.isdir(cache):
            out += aged(cache, before, 1, skip=CACHE_KEEP)
    keep = linked()
    out = [p for p in out if not any(k == os.path.normpath(p) or k.startswith(os.path.normpath(p) + os.sep) or os.path.normpath(p).startswith(k + os.sep) for k in keep)]
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
    items = stale(commits)
    mb = sum(size(p) for p in items) / 1024 / 1024
    for p in items:
        print(os.path.relpath(p, ROOT).replace(chr(92), '/'))
    print('可删 %d 项，共 %.1f MB（output 按之后主线上已有 %d 次提交算）' % (len(items), mb, commits))
    for t in worktrees():
        print(('失效的工作树登记（--delete 时 prune）：' if t['prunable'] else '还登记着的工作树（确认不用了再 git worktree remove）：') + t['path'])
    if '--delete' in argv:
        for p in items:
            remove(p)
        git('worktree', 'prune')
        for r, ds, fs in sorted(os.walk(OUT), key=lambda t: -len(t[0])):  # 删掉清空后的目录
            if r != OUT and not ds and not fs and os.path.relpath(r, OUT).split(os.sep)[0] not in KEEP:
                os.rmdir(r)
        print('已删')


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
