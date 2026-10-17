"""由文档文件头生成 docs/design/DOCMAP.md、小节目录 SECTIONS.md 和路线表 ROUTES.json。

用法：python -I scripts/docs/gen_index.py；README.md 为手写入口，不由脚本改写。
依赖：PyYAML。
"""
import os, re, glob, sys, json
import yaml

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
D = os.path.join(ROOT, 'docs', 'design')
DOCMAP = os.path.join(D, 'DOCMAP.md')
CAT_ORDER = ['core', 'workflow', 'art', 'world', 'maps', 'systems', 'ui', 'engineering', 'reference',
             'gameplay', 'narrative', 'characters', 'content', 'tools', 'qa', 'ops', 'localization',
             'decisions', 'proposals', 'issues', 'experiments']
CAT_NAME = {'core': '共识层', 'workflow': '协作与流程', 'art': '美术方向', 'world': '世界内容（通用）', 'maps': '地图：清水河',
            'systems': '运行时系统', 'ui': '界面', 'engineering': '工程', 'reference': '参考',
            'gameplay': '玩法', 'narrative': '叙事', 'characters': '角色', 'content': '内容',
            'tools': '工具', 'qa': '测试', 'ops': '运维', 'localization': '本地化',
            'decisions': '决策记录', 'proposals': '提案', 'issues': '问题', 'experiments': '实验'}
NL = chr(10)


def load():
    docs = {}
    for p in glob.glob(os.path.join(D, '**', '*.md'), recursive=True):
        rel = os.path.relpath(p, D).replace(chr(92), '/')
        if os.path.basename(rel) == 'README.md' or rel in {'SECTIONS.md', 'DOCMAP.md'}:
            continue
        t = open(p, encoding='utf-8').read()
        m = re.match(r'^---\r?\n(.*?)\r?\n---\r?\n', t, re.S)
        if not m:
            print('no front matter:', rel)
            continue
        fm = yaml.safe_load(m.group(1))
        fm['_path'] = rel
        docs[str(fm.get('id'))] = fm
    return docs


def wps(v):
    out = []
    for x in v or []:
        out += [p for p in re.split(r'[，,\s]+', str(x)) if p]
    return out


def wpkey(w):
    m = re.match(r'WP-(\d+)([a-z]?)', w)
    return (int(m.group(1)), m.group(2)) if m else (999, w)


def folder_of(d):
    return d['_path'].rsplit('/', 1)[0] if '/' in d['_path'] else '.'


def build(docs):
    out = ['# 文档地图与工作包矩阵', '', '> 由 `scripts/docs/gen_index.py` 生成，不要手改。开发入口见 [总索引](README.md)，小节定位见 [SECTIONS.md](SECTIONS.md)。', '', '## 1 文档地图', '']
    n = 0
    for cat in CAT_ORDER:
        ds = sorted([d for d in docs.values() if d.get('category') == cat], key=lambda d: d['_path'])
        if not ds:
            continue
        n += 1
        folder = folder_of(ds[0])
        out += ['### 1.%d %s `%s/`' % (n, CAT_NAME.get(cat, cat), folder), '',
                '| 文档 | 规定什么 | 主责 | 门禁 |', '| --- | --- | --- | --- |']
        for d in ds:
            out.append('| [%s](%s) %s | %s | %s | %s |' % (
                d['id'], d['_path'], d.get('title', ''), d.get('summary', ''),
                '、'.join(wps(d.get('owners'))) or '—', '、'.join(map(str, d.get('gates') or [])) or '—'))
        out.append('')

    wp = {}
    for d in docs.values():
        for w in wps(d.get('owners')):
            wp.setdefault(w, {'own': [], 'contrib': []})['own'].append(d['id'])
        for w in wps(d.get('contributors')):
            wp.setdefault(w, {'own': [], 'contrib': []})['contrib'].append(d['id'])
    out += ['## 2 工作包与文档', '',
            '主责与增量取自各文档文件头的 `owners`、`contributors`；独占目录、起读与交付以 ENG-WP 为准。', '',
            '| 工作包 | 主责文档 | 有增量的文档 |', '| --- | --- | --- |']
    for w in sorted(wp, key=wpkey):
        out.append('| %s | %s | %s |' % (w, '、'.join(sorted(set(wp[w]['own']))) or '—',
                                          '、'.join(sorted(set(wp[w]['contrib']))) or '—'))
    out.append('')

    return NL.join(out)


SECTIONS = os.path.join(D, 'SECTIONS.md')
RECORDS = {'decisions', 'proposals', 'issues', 'experiments'}
HEAD = re.compile(r'^(#{2,3})\s+(.*?)\s*$')


def kchars(n):
    return '%.1fk' % (n / 1000) if n >= 1000 else str(n)


def outline(text):
    """返回 [(级别, 编号, 标题, 行号, 字数)]；字数到下一个同级或更高级标题为止，跳过代码块里的 #。"""
    lines = text.split(NL)
    heads, fence = [], False
    for i, l in enumerate(lines):
        if l.lstrip().startswith('```'):
            fence = not fence
            continue
        m = None if fence else HEAD.match(l)
        if m:
            heads.append((len(m.group(1)), i, m.group(2)))
    offs = [0]
    for l in lines:
        offs.append(offs[-1] + len(l) + 1)
    out = []
    for k, (lv, i, title) in enumerate(heads):
        end = len(lines)
        for lv2, i2, _ in heads[k + 1:]:
            if lv2 <= lv:
                end = i2
                break
        m = re.match(r'(\d+(?:\.\d+)*[a-z]?)\s+(.*)', title)
        num, name = (m.group(1), m.group(2)) if m else ('', title)
        out.append((lv, num, name, i + 1, offs[end] - offs[i]))
    return out


def build_sections():
    """小节目录：每份规范文档的每个 ##、### 小节一行，带行号和字数，供 grep 定位（WF-ONBOARD §3.1）。"""
    items = []
    for p in glob.glob(os.path.join(D, '**', '*.md'), recursive=True):
        rel = os.path.relpath(p, D).replace(chr(92), '/')
        if rel in {'SECTIONS.md', 'DOCMAP.md'} or (os.path.basename(rel) == 'README.md' and rel != 'README.md'):
            continue
        t = open(p, 'rb').read().decode('utf-8').replace('\r\n', NL)
        m = re.match(r'^---\n(.*?)\n---\n', t, re.S)
        if not m:
            continue
        fm = yaml.safe_load(m.group(1))
        if fm.get('category') in RECORDS:
            continue
        items.append((CAT_ORDER.index(fm.get('category')) if fm.get('category') in CAT_ORDER else 99,
                      0 if rel == 'README.md' else 1, rel, fm, t))
    items.sort(key=lambda x: (x[0], x[1], x[2]))
    out = ['# 小节目录', '',
           '> 由 `scripts/docs/gen_index.py` 生成，不要手改。每份规范文档的每个小节一行：`编码 §号 标题 · L行号 · 字数`；'
           '★ 标出各文档的接入要点。用法：按关键词或编码 grep 本文件，再按行号只读原文的那一段（WF-ONBOARD §3.1）。'
           '行号以生成时为准，原文改过以后可能差几行，以标题文字为准。', '']
    for _, _, rel, fm, t in items:
        code = str(fm.get('id'))
        out += ['## %s · %s' % (code, fm.get('title', '')), '',
                '`docs/design/%s` · %s 字 · %s' % (rel, kchars(len(t)), fm.get('summary', ''))]
        kw = fm.get('keywords') or []
        if kw:
            out.append('关键词：' + '、'.join(map(str, kw)))
        out.append('')
        for lv, num, name, line, size in outline(t):
            star = '★ ' if '接入要点' in name else ''
            sec = ' §' + num if num else ''
            out.append('%s- %s%s%s %s · L%d · %s' % ('  ' if lv == 3 else '', star, code, sec, name, line, kchars(size)))
        out.append('')
    return NL.join(out)


ROUTES = os.path.join(D, 'ROUTES.json')


def build_routes(docs):
    """路线表（定案 r016 §3）：文档编码到路径、工作包到文档编码、路径 glob 到文档编码；钩子按它在读改文件时注入。"""
    allfm = dict(docs)
    t = open(os.path.join(D, 'README.md'), encoding='utf-8').read()
    m = re.match(r'^---\r?\n(.*?)\r?\n---\r?\n', t, re.S)
    if m:
        fm = yaml.safe_load(m.group(1))
        fm['_path'] = 'README.md'
        allfm[str(fm.get('id'))] = fm
    paths = {c: 'docs/design/' + d['_path'] for c, d in allfm.items()}
    # ENG-WP §2 主表：包、起读栏
    wp_text = open(os.path.join(D, 'engineering', 'ENG-WP-工作包派工单.md'), encoding='utf-8').read().replace('\r\n', NL)
    sec = re.search(r'^## 2 .*?(?=^##)', wp_text, re.S | re.M).group(0)
    reads = {}
    for line in sec.split(NL):
        c = [x.strip() for x in line.split('|')]
        if len(c) >= 8 and re.fullmatch(r'WP-\d+[a-z]?', c[1]):
            m = re.search(r'接入要点：([^；]*)', c[5])
            reads[c[1]] = []
            for tok in re.split(r'[、,，]', m.group(1) if m else ''):
                tok = tok.strip()
                if tok in ('', '无'):
                    continue
                cm = re.match(r'[A-Z]+(?:-[A-Z0-9]+)*', tok)
                if not cm or cm.group(0) not in paths:
                    raise SystemExit('ENG-WP §2 %s 起读栏的编码不存在：%s' % (c[1], tok))
                reads[c[1]].append(cm.group(0))
    own = json.load(open(os.path.join(ROOT, 'ownership.json'), encoding='utf-8'))
    pkgs = {r['owner'] for r in own['rules'] if r['owner'] != 'Opus'} | set(reads)
    wp = {}
    for w in sorted(pkgs, key=wpkey):
        codes = []
        for c in sorted(allfm):
            d = allfm[c]
            if d.get('category') == 'workflow':
                continue
            if any(o == w or (w.startswith(o) and re.fullmatch(r'[a-z]', w[len(o):])) for o in wps(d.get('owners'))):
                codes.append(c)
        for c in reads.get(w, reads.get(w.rstrip('abcdefghijklmnopqrstuvwxyz'), [])):  # 子包没有自己的行时用母包的
            if c not in codes and allfm[c].get('category') != 'workflow':
                codes.append(c)
        wp[w] = codes
    globs = {}
    for c, d in allfm.items():
        pv = d.get('paths') or []
        for g in [pv] if isinstance(pv, str) else pv:
            globs.setdefault(str(g), []).append(c)
    out = {'version': 1, 'docs': paths, 'wp': wp,
           'paths': [{'glob': g, 'docs': sorted(set(globs[g]))} for g in sorted(globs)]}
    return json.dumps(out, ensure_ascii=False, sort_keys=True, indent=1) + NL


def main():
    docs = load()
    open(DOCMAP, 'wb').write(build(docs).encode('utf-8'))
    print('docs', len(docs), '-> DOCMAP.md regenerated')
    s = build_sections()
    open(SECTIONS, 'wb').write(s.encode('utf-8'))
    print('SECTIONS.md', s.count(NL + '- ') + s.count(NL + '  - '), 'sections,', len(s), 'chars')
    r = build_routes(docs)
    open(ROUTES, 'wb').write(r.encode('utf-8'))
    print('ROUTES.json', len(r), 'chars')


if __name__ == '__main__':
    main()
