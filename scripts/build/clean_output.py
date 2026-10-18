"""清理 output/ 里的一次性产物（WF-RULE §13）：之后主线上已经有了 N 次提交（默认 20）、又不在保留名单里的，列出或删掉。
按提交轮次而不按时间算：不开发就没有提交，什么都不会过期。

用法：
  python -I -X utf8 scripts/build/clean_output.py                列出可删的项和大小
  python -I -X utf8 scripts/build/clean_output.py --delete       删掉它们
  python -I -X utf8 scripts/build/clean_output.py --commits 10   改轮次
"""
import os
import shutil
import stat
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
OUT = os.path.join(ROOT, 'output')
KEEP = {'.locks', 'cache', 'shots', 'pipeline-wheels'}  # 构建锁、烘焙缓存、固定机位截图、管线安装包
COMMITS = 20


def cutoff(commits):
    """往前数第 commits 次提交的时间；提交不够这么多时返回 None（什么都不算过期）。"""
    out = subprocess.run(['git', '-C', ROOT, 'log', '-n', str(commits), '--format=%ct'],
                         capture_output=True, text=True).stdout.split()
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


def stale(commits=COMMITS):
    """返回可删的绝对路径：顶层整项过期就整项删。"""
    before = cutoff(commits)
    if before is None or not os.path.isdir(OUT):
        return []
    out = []
    for name in sorted(os.listdir(OUT)):
        p = os.path.join(OUT, name)
        if name in KEEP:
            continue
        if newest(p) < before:
            out.append(p)
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
    print('可删 %d 项，共 %.1f MB（之后主线上已有 %d 次提交）' % (len(items), mb, commits))
    if '--delete' in argv:
        for p in items:
            remove(p)
        for r, ds, fs in sorted(os.walk(OUT), key=lambda t: -len(t[0])):  # 删掉清空后的目录
            if r != OUT and not os.listdir(r) and os.path.relpath(r, OUT).split(os.sep)[0] not in KEEP:
                os.rmdir(r)
        print('已删')
    return 0


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.exit(main(sys.argv[1:]))
