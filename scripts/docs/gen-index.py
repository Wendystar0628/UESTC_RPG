"""由文档文件头生成 docs/ 的小节目录 SECTIONS.md 和钩子路线表 ROUTES.json（文档地图是手写的 README §1）。

产出：docs/SECTIONS.md、docs/ROUTES.json
用法：python -I scripts/docs/gen-index.py；README.md 为手写入口，不由脚本改写。
依赖：PyYAML。
"""
import os, re, glob, sys, json
import yaml

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
D = os.path.join(ROOT, 'docs')
CAT_ORDER = ['core', 'product', 'art', 'world', 'maps', 'systems', 'ui', 'engineering', 'plan', 'workflow',
             'standards', 'registry']
CAT_NAME = {'core': '总纲', 'product': '作品', 'art': '画面', 'world': '世界', 'maps': '地图：清水河', 'systems': '系统',
            'ui': '界面', 'engineering': '工程', 'plan': '计划', 'workflow': '协作', 'standards': '规范', 'registry': '登记'}
NL = chr(10)


def load():
    docs = {}
    for p in glob.glob(os.path.join(D, '**', '*.md'), recursive=True):
        rel = os.path.relpath(p, D).replace(chr(92), '/')
        if rel.startswith('godot-api/'):  # 生成的 API 参考，不是设计文档
            continue
        if os.path.basename(rel) == 'README.md' or rel == 'SECTIONS.md':
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
    """小节目录：每份规范文档的每个 ##、### 小节一行，带行号和字数，供 grep 定位（STD-DOC §1）。"""
    items = []
    for p in glob.glob(os.path.join(D, '**', '*.md'), recursive=True):
        rel = os.path.relpath(p, D).replace(chr(92), '/')
        if rel.startswith('godot-api/'):  # 生成的 API 参考，不是设计文档
            continue
        if rel == 'SECTIONS.md' or (os.path.basename(rel) == 'README.md' and rel != 'README.md'):
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
    # 域内按文档树排：域总纲在前，主题文档按编码，子文档紧跟在它的主题文档后面
    def tree_key(x):
        cat, root, rel, fm, _t = x
        code, par = str(fm.get('id')), str(fm.get('parent') or '')
        if root == 0 or code.endswith('-INDEX'):
            return (cat, root, 0, '', 0, code)
        topic = par if par and not par.endswith('-INDEX') else code
        return (cat, root, 1, topic, 0 if topic == code else 1, code)
    items.sort(key=tree_key)
    out = ['# 小节目录', '',
           '> 由 `scripts/docs/gen-index.py` 生成，不要手改。每份规范文档的每个小节一行：`编码 §号 标题 · L行号 · 字数`；'
           '★ 标出各文档的接入要点。用法：按关键词或编码 grep 本文件，再按行号只读原文的那一段（CORE-INDEX §1）。'
           '行号以生成时为准，原文改过以后可能差几行，以标题文字为准。', '']
    for _, _, rel, fm, t in items:
        code = str(fm.get('id'))
        out += ['## %s · %s' % (code, fm.get('title', '')), '',
                '`docs/%s` · %s 字 · %s' % (rel, kchars(len(t)), fm.get('summary', ''))]
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
    """路线表（定案 r016 §3）：文档编码到路径、工作包到文档编码（按文件头 owners）、路径 glob 到文档编码；钩子按它在读改文件时注入。"""
    allfm = dict(docs)
    t = open(os.path.join(D, 'README.md'), encoding='utf-8').read()
    m = re.match(r'^---\r?\n(.*?)\r?\n---\r?\n', t, re.S)
    if m:
        fm = yaml.safe_load(m.group(1))
        fm['_path'] = 'README.md'
        allfm[str(fm.get('id'))] = fm
    paths = {c: 'docs/' + d['_path'] for c, d in allfm.items()}
    own = json.load(open(os.path.join(ROOT, 'ownership.json'), encoding='utf-8'))
    pkgs = {r['owner'] for r in own['rules'] if r['owner'] != 'Opus'}
    wp = {}
    for w in sorted(pkgs, key=wpkey):
        codes = []
        for c in sorted(allfm):
            d = allfm[c]
            if d.get('category') == 'workflow':
                continue
            if any(o == w or (w.startswith(o) and re.fullmatch(r'[a-z]', w[len(o):])) for o in wps(d.get('owners'))):
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
    print('docs', len(docs))
    s = build_sections()
    open(SECTIONS, 'wb').write(s.encode('utf-8'))
    print('SECTIONS.md', s.count(NL + '- ') + s.count(NL + '  - '), 'sections,', len(s), 'chars')
    r = build_routes(docs)
    open(ROUTES, 'wb').write(r.encode('utf-8'))
    print('ROUTES.json', len(r), 'chars')


if __name__ == '__main__':
    main()
