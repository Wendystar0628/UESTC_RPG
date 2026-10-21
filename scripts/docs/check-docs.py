"""校验 docs（不含 godot-api）：小节引用、文件头的 depends/related、相对链接、路径归属字段与门禁编号、导航树（没有孤儿文档）、
paths 里匹配不到入库文件的 glob、game/ 与 pipeline/ 里带节号的文档引用、顶层节号区间；另列出（不计问题数）「标题」对不上的小节引用与反引号里不存在的路径。

产出：无（仅标准输出文档一致性问题）
用法：python -I scripts/docs/check-docs.py
依赖：PyYAML、git。有问题时退出码为 1。
"""
import os, re, glob, sys, runpy, subprocess
from collections import Counter
import yaml

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
D = os.path.join(ROOT, 'docs')
FM = re.compile(r'^---\r?\n(.*?)\r?\n---\r?\n', re.S)


def git(*args, stdin=None):
    """git 输出按行（-z 时按 NUL）切开；路径不转义中文。"""
    out = subprocess.run(['git', '-C', ROOT, '-c', 'core.quotepath=false', *args], input=stdin, capture_output=True).stdout.decode('utf-8')
    return [x for x in out.split('\0' if '-z' in args else '\n') if x]


TRACKED = git('ls-files', '-z')


def glob_re(g):
    """与钩子的路由同一套写法：** 跨目录，* 与 ? 不跨 /。"""
    s = re.escape(g).replace(r'\*\*', '\0').replace(r'\*', '[^/]*').replace(r'\?', '[^/]').replace('\0', '.*')
    return re.compile('^' + s + '$')

docs = {}
for p in glob.glob(os.path.join(D, '**', '*.md'), recursive=True):
    if os.path.relpath(p, D).replace(chr(92), '/').startswith('godot-api/'):
        continue
    t = open(p, encoding='utf-8').read()
    m = FM.match(t)
    if not m:
        continue
    fm = yaml.safe_load(m.group(1))
    secs = set(re.findall(r'^#{1,6}\s+(\d+(?:\.\d+)*[a-z]?)(?=\s)', t, re.M)) | set(re.findall(r'\*\*(\d+(?:\.\d+)+)', t))
    docs[str(fm.get('id'))] = {'path': p, 'text': t, 'secs': secs, 'fm': fm}
codes = set(docs)
alt = '|'.join(sorted(map(re.escape, codes), key=len, reverse=True))


def ok(code, sec):
    s = docs[code]['secs']
    return sec in s or any(x.startswith(sec + '.') for x in s)


bad = []
# 记录类文档（提案、问题、实验、决策记录）会引用尚未写进正文的小节，不查 § 引用，只查文件头和链接
RECORDS = {'proposals', 'issues', 'experiments', 'decisions'}
for code, d in docs.items():
    t = d['text'] if d['fm'].get('category') not in RECORDS else ''
    # 跨文档引用 "SYS-WEATHER §6.4"
    for m in re.finditer(r'(' + alt + r')\s*§\s*(\d+(?:\.\d+)*)', t):
        if not ok(m.group(1), m.group(2)):
            bad.append((code, 'xref', m.group(0)))
    # 裸 "§6.4"：紧跟在别的编码后面的（如 "SYS-WALK §6–§8"）按那份文档查，否则按本文查
    for m in re.finditer(r'(?<![A-Z\-\w])§\s*(\d+(?:\.\d+)*)', t):
        pre = t[max(0, m.start() - 25):m.start()]
        if re.search(r'(' + alt + r')[^§\n]{0,3}$', pre):
            continue
        full_pre = t[t.rfind('\n', 0, m.start()) + 1:m.start()]
        # 引号或反引号里的是写法示例，不查
        if full_pre.count('`') % 2 or full_pre.count('"') % 2 or full_pre.count('“') > full_pre.count('”'):
            continue
        line_pre = full_pre[-80:]
        near = list(re.finditer(r'(' + alt + r')', line_pre))
        tgt = code
        if near and re.fullmatch(r'[\s§\d.*、，,–\-和与及的至少读先看尤其全篇各自结论（(]*', line_pre[near[-1].end():]):
            tgt = near[-1].group(1)
        if not ok(tgt, m.group(1)):
            bad.append((code, 'self', t[max(0, m.start() - 15):m.end()].replace('\n', ' ')))
    for k in ('depends', 'related'):
        for x in d['fm'].get(k) or []:
            if str(x) not in codes:
                bad.append((code, 'fm-' + k, x))
    for m in re.finditer(r'\]\(([^)#\s]+?\.md)(#[^)]*)?\)', d['text']):
        tgt = os.path.normpath(os.path.join(os.path.dirname(d['path']), m.group(1)))
        if not m.group(1).startswith('http') and not os.path.exists(tgt):
            bad.append((code, 'link', m.group(1)))

# 路径归属只取文件头 paths；门禁编号以 ENG-GATES 为准
gates = set(re.findall(r'\b(G\d{1,2})\b', open(os.path.join(D, 'engineering', 'ENG-GATES-验收与门禁.md'), encoding='utf-8').read()))
for code, d in docs.items():
    fm = d['fm']
    for retired in ('owners', 'contributors'):
        if retired in fm:
            bad.append((code, 'fm-retired', retired))
    pv = fm.get('paths', [])
    if not isinstance(pv, list):
        bad.append((code, 'fm-paths', 'paths 应为列表（无归属写 []）'))
    else:
        for pattern in pv:
            if not isinstance(pattern, str) or not pattern.strip():
                bad.append((code, 'fm-paths', repr(pattern)))
            elif pattern.startswith(('/', '\\')) or re.match(r'^[A-Za-z]:', pattern) or '\\' in pattern or '..' in pattern.split('/'):
                bad.append((code, 'fm-paths', '应为仓库相对路径并使用 /：' + pattern))
            elif not pattern.startswith('.collab/'):
                # paths 只写已入库的源路径（STD-DOC §2）：一个入库文件都匹配不上的是生成物、被忽略的文件或已删的路径
                rx, head, folder = glob_re(pattern), re.split(r'[*?]', pattern, 1)[0], pattern.rstrip('/') + '/'
                if not any(rx.match(f) or f.startswith(folder) for f in TRACKED if f.startswith(head)):
                    bad.append((code, 'fm-paths-dead', pattern))
    for g in fm.get('gates') or []:
        if str(g) not in gates:
            bad.append((code, 'gate', g))

# 导航树（STD-DOC §5.1）：主文档要列在 CORE-INDEX §1，子文档写 parent 并列在主文档里；不许有孤儿文档
index = docs.get('CORE-INDEX', {}).get('text', '')
m = re.search(r'^## 1 .*?(?=^## |\Z)', index, re.S | re.M)
index1 = m.group(0) if m else ''
for code, d in docs.items():
    if code == 'CORE-INDEX' or d['fm'].get('category') in RECORDS:
        continue
    parent = d['fm'].get('parent')
    if parent:
        if parent not in docs:
            bad.append((code, 'tree', 'parent 不存在：%s' % parent))
        elif not re.search(r'(?<![A-Z0-9-])%s(?![A-Z0-9-])' % re.escape(code), docs[parent]['text']):
            bad.append((code, 'tree', '主文档 %s 里没有列出它' % parent))
    elif not re.search(r'(?<![A-Z0-9-])%s(?![A-Z0-9-])' % re.escape(code), index1):
        bad.append((code, 'tree', 'CORE-INDEX §1 没有列出它'))

# 便于按需查阅（DECISION-178）：规范文档要有 keywords、查阅行、§0 和 §0 末尾的接入要点
for code, d in docs.items():
    rel = os.path.relpath(d['path'], D).replace(chr(92), '/')
    if d['fm'].get('category') in RECORDS or (os.path.basename(rel) == 'README.md' and rel != 'README.md'):
        continue
    t = d['text']
    if not d['fm'].get('keywords'):
        bad.append((code, 'keywords', 'missing'))
    if not re.search(r'^> \*\*查阅\*\*：', t, re.M):
        bad.append((code, 'lookup', 'missing'))
    s0 = re.search(r'^## 0 .*?(?=^## [1-9]|\Z)', t, re.S | re.M)
    if not s0:
        bad.append((code, 'sec0', 'missing'))
    elif not re.search(r'^### 0\.\d+ 接入要点\s*$', s0.group(0), re.M):
        bad.append((code, 'hooks', 'missing'))

# 代码与数据里提到文档只写编码、不写节号（STD-DOC §3）；修法：remap-refs.py strip
CODE_REF = re.compile(r'(?<![A-Za-z0-9\-])(' + alt + r')(?![A-Za-z0-9\-])\s*(?:的\s*)?§\s*\d+(?:\.\d+)*')
for hit in git('grep', '-n', '-I', '-E', '§ *[0-9]', '--', 'game', 'pipeline'):
    where, _, text = hit.partition(':')
    line, _, text = text.partition(':')
    m = CODE_REF.search(text)
    if m:
        bad.append(('%s:%s' % (where, line), 'code-sec', m.group(0)))

# 顶层的节会增长，一组节写组名、不写区间（STD-DOC §3，DECISION-299）：区间前最近的编码是 CORE-INDEX，或写在 CORE-INDEX 本文里的裸区间
RANGE = re.compile(r'§\s*\d+\s*[–—~至-]\s*§?\s*\d+(?![.\d])')
LAST_CODE = re.compile(r'(?<![A-Za-z0-9\-])(' + alt + r'|顶层)(?![A-Za-z0-9\-])')
range_files = [(code, d['path']) for code, d in docs.items()] + [(name, os.path.join(ROOT, name)) for name in ('CLAUDE.md', 'AGENTS.md')]
for code, p in range_files:
    if not os.path.isfile(p):
        continue
    for n, l in enumerate(open(p, encoding='utf-8'), 1):
        for m in RANGE.finditer(l):
            near = LAST_CODE.findall(l[:m.start()])
            if (near[-1] if near else code) in ('CORE-INDEX', '顶层'):
                bad.append(('%s:%d' % (code, n), 'core-range', l[max(0, m.start() - 20):m.end()].strip()))

# 只列出、不计问题数：「标题」在指向的那一节里找不到；正文反引号里的仓库路径或脚本名不存在且没被 git 忽略
notes = []


def section_text(text, sec):
    """标题号为 sec 的那一节（含标题行与子节），找不到返回 None。"""
    lines = text.split('\n')
    at = next((i for i, l in enumerate(lines) if re.match(r'^(#{1,6})\s+' + re.escape(sec) + r'\s', l)), None)
    if at is None:
        return None
    level = len(re.match(r'^#+', lines[at]).group(0))
    end = next((j for j in range(at + 1, len(lines)) if re.match(r'^#{1,%d}\s' % level, lines[j])), len(lines))
    return '\n'.join(lines[at:end])


def plain(s):
    return re.sub(r'[*`\s]', '', s)


TITLED = re.compile(r'(?:(?<![A-Za-z0-9\-])(' + alt + r')(?![A-Za-z0-9\-])\s*(?:的\s*)?)?§\s*(\d+(?:\.\d+)*)\s*「([^」\n]+)」')
for code, d in docs.items():
    if d['fm'].get('category') in RECORDS:
        continue
    for m in TITLED.finditer(d['text']):
        tgt = m.group(1) or code
        body = section_text(docs[tgt]['text'], m.group(2))
        if body is not None and plain(m.group(3)) not in plain(body):
            notes.append((code, 'title', m.group(0)))

PATH_TOKEN = re.compile(r'(?:game|docs|scripts|dashboard|pipeline|tools|\.collab|output|build)/[\w.\-/一-鿿]*')
SCRIPT_TOKEN = re.compile(r'[\w\-]+\.(?:py|ps1|sh|cjs|cmd)')
names = {os.path.basename(f) for f in TRACKED}
for base, _dirs, files in os.walk(os.path.join(ROOT, '.collab', 'tools')):
    names.update(files)
missing = []
for code, d in docs.items():
    body = re.sub(r'^```.*?^```', '', d['text'], flags=re.S | re.M)
    for tok in re.findall(r'`([^`\n]+)`', body):
        if PATH_TOKEN.fullmatch(tok) and not os.path.exists(os.path.join(ROOT, tok)):
            missing.append((code, tok))
        elif SCRIPT_TOKEN.fullmatch(tok) and tok not in names:
            notes.append((code, 'backtick', tok))
ignored = set(git('check-ignore', '--stdin', '-z', stdin='\0'.join(sorted({t for _, t in missing})).encode('utf-8'))) if missing else set()
notes += [(code, 'backtick', tok) for code, tok in dict.fromkeys(missing) if tok not in ignored and tok.rstrip('/') not in ignored]

# 小节目录要与文档一致：文档改过以后重跑 gen-index.py
gen_index = runpy.run_path(os.path.join(ROOT, 'scripts', 'docs', 'gen-index.py'))
sec_path = os.path.join(D, 'SECTIONS.md')
if not os.path.exists(sec_path) or open(sec_path, 'rb').read().decode('utf-8') != gen_index['build_sections']():
    bad.append(('SECTIONS', 'stale', '重跑 scripts/docs/gen-index.py'))
rt_path = os.path.join(D, 'ROUTES.json')
if not os.path.exists(rt_path) or open(rt_path, 'rb').read().decode('utf-8') != gen_index['build_routes'](gen_index['load']()):
    bad.append(('ROUTES', 'stale', '重跑 scripts/docs/gen-index.py'))

# 体量上限（STD-DOC §5）：只列出，不算 problems；量法只在 doc-size.py
doc_size = runpy.run_path(os.path.join(ROOT, 'scripts', 'docs', 'doc-size.py'))
over = [(code, where, n) for p in doc_size['docs_in']() for code, _, ov in [doc_size['check'](p)] for where, n, _cap in ov]
print('docs', len(docs), 'problems', len(bad), dict(Counter(b[1] for b in bad)))
for b in bad:
    print('%s\t%s\t%s' % b)
if over:
    print('超体量上限', len(over), '处（STD-DOC §5）')
    for o in over:
        print('  %s\t%s\t%d 字' % o)
if notes:
    print('只列出、不计问题数', len(notes), '处', dict(Counter(n[1] for n in notes)))
    for n in notes:
        print('  %s\t%s\t%s' % n)
sys.exit(1 if bad else 0)
