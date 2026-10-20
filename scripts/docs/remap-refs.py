"""拆分或合并文档后改引用（STD-DOC §5.1）：按映射表把"编码 §节"改到新位置；代码与数据里的引用去掉节号。

映射表（文本，可给多份或一个文件夹）每行一条，# 后是注释：
  旧编码 §旧节 → 新编码 §新节      子节按前缀跟着走，个别子节另列
  旧编码 §旧节 → 新编码            并成整份
  旧编码 → 新编码                  整份改名或并入，不带 § 的编码也改
改了节号的文档，每个一级节都要列，不变的也列；漏列的节会报出来。
写新内容时要指向拆分后才有的位置，写成 ⟦新编码 §新节⟧，apply 原样保留并去掉括号。

用法（python -I -X utf8 scripts/docs/remap-refs.py …）：
  explicit                       拆之前：各文档里指本文的裸 §n 补上本文编码，搬到别的文档后不会指错
  apply --map M… [--files F…]    拆之后：docs/、scripts/ 与根目录说明按映射改；--files 只改给出的文件
  strip [--map M…] [--files F…]  game/、pipeline/ 里的文档引用去掉节号（STD-CODE §0.1），并按映射改整份改名的编码
  simplify                       最后：各文档里"本文编码 §n"去掉编码
没给 --files 时跳过别人改了还没提交的文件（docs/ 除外），列出来，提交后再用 --files 补。
"""
import glob
import os
import re
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')).replace('\\', '/')
FM = re.compile(r'^---\r?\n(.*?)\r?\n---\r?\n', re.S)
C = r'[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+'
SEC = r'\d+(?:\.\d+)*[a-z]?'
SEP = r'\s*(?:、|，|,|和|与|及|或|–|—|-|~|至)\s*'
RANGE = ('–', '—', '-', '~', '至')
TEXT = ('.md', '.cs', '.json', '.py', '.cjs', '.js', '.ps1', '.sh', '.txt', '.gdshader', '.gdshaderinc', '.cfg', '.toml', '.yaml', '.yml', '.tscn', '.tres')
SKIP = ('/.godot/', '/bin/', '/obj/', '/node_modules/', '/godot-api/', '/__pycache__/')


def load_map(paths):
    secs, docs = {}, {}
    files = []
    for p in paths:
        files += sorted(glob.glob(os.path.join(p, '*.txt'))) if os.path.isdir(p) else [p]
    for p in files:
        for n, line in enumerate(open(p, encoding='utf-8'), 1):
            line = line.split('#')[0].strip()
            if not line:
                continue
            a, b = [x.strip() for x in re.split(r'→|->', line)]
            ma = re.fullmatch(r'(' + C + r')(?:\s*§\s*(' + SEC + r'))?', a)
            mb = re.fullmatch(r'(' + C + r')(?:\s*§\s*(' + SEC + r'))?', b)
            assert ma and mb, f'{p}:{n} 看不懂：{line}'
            if ma.group(2) is None:
                docs[ma.group(1)] = mb.group(1)
                continue
            key, val = (ma.group(1), ma.group(2)), (mb.group(1), mb.group(2) or '')
            assert secs.get(key, val) == val, f'{p}:{n} 与前面的映射冲突：{line}'
            secs[key] = val
    return secs, docs


def doc_codes():
    out = {}
    for p in glob.glob(ROOT + '/docs/**/*.md', recursive=True):
        p = p.replace('\\', '/')
        if '/godot-api/' in p:
            continue
        f = FM.match(open(p, encoding='utf-8').read())
        c = f and re.search(r'^id:\s*(\S+)', f.group(1), re.M)
        if c:
            out[c.group(1)] = p
    return out


class Remap:
    def __init__(self, maps):
        self.secs, self.docs = load_map(maps) if maps else ({}, {})
        self.old = {c for c, _ in self.secs}
        codes = set(doc_codes()) | self.old | {c for c, _ in self.secs.values()} | set(self.docs) | set(self.docs.values())
        alt = '|'.join(sorted(map(re.escape, codes), key=len, reverse=True))
        self.alt = alt
        self.code = r'(?<![A-Za-z0-9\-])(' + alt + r')(?![A-Za-z0-9\-])'
        self.group = re.compile(self.code + r'(\s*(?:的\s*)?)§\s*(' + SEC + r')((?:' + SEP + r'§\s*' + SEC + r')*)')
        self.missing = set()

    def look(self, code, sec):
        if code not in self.old:
            return self.docs.get(code, code), sec
        parts = sec.split('.')
        for i in range(len(parts), 0, -1):
            k = '.'.join(parts[:i])
            if (code, k) in self.secs:
                nc, ns = self.secs[(code, k)]
                return nc, (ns + ''.join('.' + r for r in parts[i:])) if ns else ''
        self.missing.add(f'{code} §{sec}')
        return code, sec

    def items(self, m):
        code, first, tail = m.group(1), m.group(3), m.group(4)
        seq = [('', first)] + re.findall(r'(' + SEP + r')§\s*(' + SEC + ')', tail)
        out = []
        for i, (sep, sec) in enumerate(seq):
            prev = seq[i - 1][1] if i else ''
            if i and sep.strip() in RANGE and prev.isdigit() and sec.isdigit() and int(sec) > int(prev):
                # 一级节的区间逐节展开，中间有节搬去别处也能分开写
                mids = [self.look(code, str(k)) for k in range(int(prev) + 1, int(sec) + 1)]
                lc, ls = out[-1][1], out[-1][2]
                if ls.isdigit() and all(c == lc for c, _ in mids) and [s for _, s in mids] == [str(int(ls) + j + 1) for j in range(len(mids))]:
                    out.append((sep, lc, mids[-1][1]))
                else:
                    out += [('、', c, s) for c, s in mids]
                continue
            c, s = self.look(code, sec)
            out.append((sep, c, s))
        return out

    @staticmethod
    def render(items, gap):
        out, prev, seen = [], None, set()
        for sep, c, s in items:
            if (c, s) in seen:
                continue
            seen.add((c, s))
            if not out:
                out.append(c + ((' 的 §' if gap.strip() == '的' else ' §') + s if s else ''))
            elif not s:
                out.append('、' + c)
            elif c == prev:
                out.append(sep + '§' + s)
            else:
                out.append(('、' if sep.strip() in RANGE else sep) + c + ' §' + s)
            prev = c if s else None
        return ''.join(out)

    def rename(self, t):
        for a, b in self.docs.items():
            t = re.sub(r'(?<![A-Za-z0-9\-])' + re.escape(a) + r'(?![A-Za-z0-9\-])', b, t)
        return t

    def apply(self, t):
        keep = []
        t = re.sub(r'⟦(.*?)⟧', lambda m: keep.append(m.group(1)) or f'\x00{len(keep) - 1}\x00', t)
        t = self.group.sub(lambda m: self.render(self.items(m), m.group(2)) if m.group(1) in self.old or m.group(1) in self.docs else m.group(0), t)
        t = self.rename(t)
        return re.sub(r'\x00(\d+)\x00', lambda m: keep[int(m.group(1))], t)

    def strip(self, t):
        t = self.group.sub(lambda m: self.docs.get(m.group(1), m.group(1)), t)
        return self.rename(t)


def dirty():
    out = subprocess.run(['git', '-C', ROOT, 'status', '--porcelain'], capture_output=True).stdout.decode('utf-8', 'ignore')
    return {l[3:].strip().strip('"').split(' -> ')[-1] for l in out.splitlines() if not l[3:].startswith('docs/')}


def walk(*dirs):
    for d in dirs:
        for p in glob.glob(f'{ROOT}/{d}/**/*', recursive=True):
            p = p.replace('\\', '/')
            if p.endswith(TEXT) and os.path.isfile(p) and not any(s in p for s in SKIP) \
                    and os.path.basename(p) not in ('SECTIONS.md', 'ROUTES.json', 'packages.lock.json', 'remap-refs.py'):
                yield p


def run(fn, files, default):
    skip = set() if files else dirty()
    changed, skipped = 0, []
    for p in files or default:
        full = p if os.path.isabs(p) else f'{ROOT}/{p}'
        try:
            t = open(full, encoding='utf-8', newline='').read()
        except (UnicodeDecodeError, OSError):
            continue
        new = fn(t)
        if new == t:
            continue
        rel = full.replace('\\', '/').removeprefix(ROOT + '/')
        if rel in skip:
            skipped.append(rel)
            continue
        open(full, 'w', encoding='utf-8', newline='').write(new)
        changed += 1
    print(f'改了 {changed} 个文件；别人还没提交、跳过的 {len(skipped)} 个')
    for p in skipped:
        print('  跳过', p)


def bare(t, alt):
    """指本文的裸 §n（判法同 check_docs；示例、列表续项、跟在别的编码后面的不算）。"""
    for m in re.finditer(r'(?<![A-Za-z0-9\-\w])§\s*(' + SEC + ')', t):
        line = t[t.rfind('\n', 0, m.start()) + 1:m.start()]
        if re.search(r'(' + alt + r')[^§\n]{0,3}$', line[-25:]) or re.search(r'§\s*' + SEC + SEP + r'$', line):
            continue
        if line.count('`') % 2 or line.count('"') % 2 or line.count('“') > line.count('”') or '⟦' in line[line.rfind('⟧') + 1:]:
            continue
        near = list(re.finditer(r'(' + alt + r')', line[-80:]))
        if near and re.fullmatch(r'[\s§\d.*、，,–\-和与及的至少读先看尤其全篇各自结论（(]*', line[-80:][near[-1].end():]):
            continue
        yield m


def explicit(r):
    total = 0
    for code, p in sorted(doc_codes().items()):
        t = open(p, encoding='utf-8', newline='').read()
        start = FM.match(t).end()
        hits = [m for m in bare(t, r.alt) if m.start() >= start]
        for m in reversed(hits):
            t = t[:m.start()] + code + ' ' + t[m.start():]
        open(p, 'w', encoding='utf-8', newline='').write(t)
        total += len(hits)
    print('补上本文编码', total, '处')


def simplify(r):
    total = 0
    for code, p in sorted(doc_codes().items()):
        t = open(p, encoding='utf-8', newline='').read()
        start, out, last = FM.match(t).end(), [], 0
        for m in re.finditer(r'(?<![A-Za-z0-9\-])' + re.escape(code) + r'(?![A-Za-z0-9\-])\s*§', t):
            before = t[max(0, m.start() - 30):m.start()]
            # 前面紧跟别的引用或别的编码时保留，否则会被读成那份文档的节
            if m.start() < start or re.search(r'§\s*' + SEC + SEP + r'$', before) or re.search(r'(' + r.alt + r')[^§\n]{0,3}$', before):
                continue
            out.append(t[last:m.start()] + '§')
            last = m.end()
            total += 1
        open(p, 'w', encoding='utf-8', newline='').write(''.join(out) + t[last:])
    print('去掉本文编码', total, '处')


def arg(a, k):
    if k not in a:
        return []
    i = a.index(k) + 1
    out = []
    while i < len(a) and not a[i].startswith('--'):
        out.append(a[i])
        i += 1
    return out


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    a = sys.argv[1:]
    cmd = a[0] if a else ''
    r = Remap(arg(a, '--map'))
    files = arg(a, '--files')
    if cmd == 'explicit':
        explicit(r)
    elif cmd == 'apply':
        root_docs = [f'{ROOT}/{f}' for f in ('AGENTS.md', 'CLAUDE.md', 'README.md')]
        run(r.apply, files, list(walk('docs', 'scripts')) + root_docs)
    elif cmd == 'strip':
        run(r.strip, files, walk('game', 'pipeline'))
    elif cmd == 'simplify':
        simplify(r)
    else:
        print(__doc__)
        sys.exit(1)
    if r.missing:
        print('映射表漏列的节：', '、'.join(sorted(r.missing)))
        sys.exit(1)
