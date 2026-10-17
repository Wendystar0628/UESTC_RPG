"""校验 docs（不含 godot-api）：小节引用、文件头的 depends/related、相对链接、工作包与门禁编号、导航树（没有孤儿文档）。

用法：python -I scripts/docs/check_docs.py
依赖：PyYAML。有问题时退出码为 1。
"""
import os, re, glob, sys
from collections import Counter
import yaml

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
D = os.path.join(ROOT, 'docs')
FM = re.compile(r'^---\r?\n(.*?)\r?\n---\r?\n', re.S)

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

# 工作包与门禁编号以 ENG-WP、ENG-GATES 为准
wp_text = ''.join(open(p, encoding='utf-8').read() for p in glob.glob(os.path.join(D, 'engineering', 'ENG-WP*.md')))
wps = set(re.findall(r'^\|\s*(WP-\d+[a-z]?)\s*\|', wp_text, re.M))
gates = set(re.findall(r'\b(G\d{1,2})\b', open(os.path.join(D, 'engineering', 'ENG-GATES-验收与门禁.md'), encoding='utf-8').read()))
for code, d in docs.items():
    fm = d['fm']
    for k in ('owners', 'contributors'):
        for w in fm.get(k) or []:
            base = re.match(r'(WP-\d+)', str(w))
            if not base or (str(w) not in wps and base.group(1) not in wps):
                bad.append((code, k, w))
    for g in fm.get('gates') or []:
        if str(g) not in gates:
            bad.append((code, 'gate', g))

# 导航树（WF-DOC §5.1）：主文档要列在 CORE-INDEX §1，子文档写 parent 并列在主文档里；不许有孤儿文档
index = docs.get('CORE-INDEX', {}).get('text', '')
m = re.search(r'^## 1 .*?(?=^## 2 )', index, re.S | re.M)
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

# 便于按需查阅（D-178）：规范文档要有 keywords、查阅行、§0 和 §0 末尾的接入要点
for code, d in docs.items():
    rel = os.path.relpath(d['path'], D).replace(chr(92), '/')
    if d['fm'].get('category') in RECORDS or (os.path.basename(rel) == 'README.md' and rel != 'README.md'):
        continue
    t = d['text']
    if not d['fm'].get('keywords'):
        bad.append((code, 'keywords', 'missing'))
    if not re.search(r'^> \*\*查阅\*\*：', t, re.M):
        bad.append((code, 'lookup', 'missing'))
    s0 = re.search(r'^## 0 .*?(?=^## [1-9])', t, re.S | re.M)
    if not s0:
        bad.append((code, 'sec0', 'missing'))
    elif not re.search(r'^### 0\.\d+ 接入要点\s*$', s0.group(0), re.M):
        bad.append((code, 'hooks', 'missing'))

# 小节目录要与文档一致：文档改过以后重跑 gen_index.py
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_index
sec_path = os.path.join(D, 'SECTIONS.md')
if not os.path.exists(sec_path) or open(sec_path, 'rb').read().decode('utf-8') != gen_index.build_sections():
    bad.append(('SECTIONS', 'stale', '重跑 scripts/docs/gen_index.py'))
rt_path = os.path.join(D, 'ROUTES.json')
if not os.path.exists(rt_path) or open(rt_path, 'rb').read().decode('utf-8') != gen_index.build_routes(gen_index.load()):
    bad.append(('ROUTES', 'stale', '重跑 scripts/docs/gen_index.py'))

# 体量上限（WF-DOC §5）：只列出，不算 problems；量法只在 doc_size.py
import doc_size
over = [(code, where, n) for p in doc_size.docs_in() for code, _, ov in [doc_size.check(p)] for where, n, _cap in ov]
print('docs', len(docs), 'problems', len(bad), dict(Counter(b[1] for b in bad)))
for b in bad:
    print('%s\t%s\t%s' % b)
if over:
    print('超体量上限', len(over), '处（WF-DOC §5）')
    for o in over:
        print('  %s\t%s\t%d 字' % o)
sys.exit(1 if bad else 0)
