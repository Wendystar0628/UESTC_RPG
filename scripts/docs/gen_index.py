"""由各文档的文件头重新生成 docs/design/README.md 的 §7 文档地图、§8 工作包与文档、§9 文档依赖图，
以及小节目录 docs/design/SECTIONS.md。

用法：python -I scripts/docs/gen_index.py
README 里从 "## 7 文档地图" 开始到文件末尾的内容会被整体替换，§0–§6 手写部分不动；SECTIONS.md 整个重写。
依赖：PyYAML。
"""
import os, re, glob, sys
import yaml

sys.stdout.reconfigure(encoding='utf-8')
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
D = os.path.join(ROOT, 'docs', 'design')
README = os.path.join(D, 'README.md')
MARK = '## 7 文档地图'
CAT_ORDER = ['core', 'art', 'world', 'maps', 'systems', 'ui', 'engineering', 'reference',
             'gameplay', 'narrative', 'characters', 'content', 'tools', 'qa', 'ops', 'localization',
             'decisions', 'proposals', 'issues', 'experiments']
CAT_NAME = {'core': '共识层', 'art': '美术方向', 'world': '世界内容（通用）', 'maps': '地图：清水河',
            'systems': '运行时系统', 'ui': '界面', 'engineering': '工程', 'reference': '参考',
            'gameplay': '玩法', 'narrative': '叙事', 'characters': '角色', 'content': '内容',
            'tools': '工具', 'qa': '测试', 'ops': '运维', 'localization': '本地化',
            'decisions': '决策记录', 'proposals': '提案', 'issues': '问题', 'experiments': '实验'}
NL = chr(10)


def load():
    docs = {}
    for p in glob.glob(os.path.join(D, '**', '*.md'), recursive=True):
        rel = os.path.relpath(p, D).replace(chr(92), '/')
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


def node(code):
    return code.replace('-', '_')


def folder_of(d):
    return d['_path'].rsplit('/', 1)[0] if '/' in d['_path'] else '.'


def empty_folders(k, used):
    out = ['### 7.%d 待用的文件夹' % k, '',
           '下列文件夹已建好，目前只有 README（编码规则和文件头模板）。新建文档后，重跑本脚本即可登记进上面的表。', '',
           '| 文件夹 | 编码 | 放什么 |', '| --- | --- | --- |']
    for p in sorted(glob.glob(os.path.join(D, '*', 'README.md'))):
        name = os.path.basename(os.path.dirname(p))
        if name in used:
            continue
        lines = open(p, encoding='utf-8').read().split(NL)
        m = re.match(r'# (\S+)/ · (\S+)', lines[0])
        if not m:
            continue
        desc = next((l for l in lines[1:] if l.strip()), '').strip()
        out.append('| [%s/](%s/README.md) | %s | %s |' % (m.group(1), m.group(1), m.group(2), desc))
    return out + ['']


def build(docs):
    out = [MARK, '']
    n = 0
    used = set()
    for cat in CAT_ORDER:
        ds = sorted([d for d in docs.values() if d.get('category') == cat], key=lambda d: d['_path'])
        if not ds:
            continue
        n += 1
        folder = folder_of(ds[0])
        used.add(folder.split('/')[0])
        out += ['### 7.%d %s `%s/`' % (n, CAT_NAME.get(cat, cat), folder), '',
                '| 文档 | 规定什么 | 主责 | 门禁 |', '| --- | --- | --- | --- |']
        for d in ds:
            out.append('| [%s](%s) %s | %s | %s | %s |' % (
                d['id'], d['_path'], d.get('title', ''), d.get('summary', ''),
                '、'.join(wps(d.get('owners'))) or '—', '、'.join(map(str, d.get('gates') or [])) or '—'))
        out.append('')
    out += empty_folders(n + 1, used)

    wp = {}
    for d in docs.values():
        for w in wps(d.get('owners')):
            wp.setdefault(w, {'own': [], 'contrib': []})['own'].append(d['id'])
        for w in wps(d.get('contributors')):
            wp.setdefault(w, {'own': [], 'contrib': []})['contrib'].append(d['id'])
    out += ['## 8 工作包与文档', '',
            '主责与增量取自各文档文件头的 `owners`、`contributors`；独占目录、起读与交付以 ENG-WP 为准。', '',
            '| 工作包 | 主责文档 | 有增量的文档 |', '| --- | --- | --- |']
    for w in sorted(wp, key=wpkey):
        out.append('| %s | %s | %s |' % (w, '、'.join(sorted(set(wp[w]['own']))) or '—',
                                          '、'.join(sorted(set(wp[w]['contrib']))) or '—'))
    out.append('')

    out += ['## 9 文档依赖图', '',
            '箭头从被依赖的文档指向依赖它的文档，取自文件头的 `depends`。所有文档都依赖本文，图中省略。', '',
            '```mermaid', 'graph LR']
    for cat in CAT_ORDER:
        ds = sorted([d for d in docs.values() if d.get('category') == cat], key=lambda d: d['id'])
        if not ds:
            continue
        out.append('  subgraph g_%s[%s]' % (cat, CAT_NAME.get(cat, cat)))
        for d in ds:
            out.append('    %s["%s"]' % (node(d['id']), d['id']))
        out.append('  end')
    for d in sorted(docs.values(), key=lambda d: d['id']):
        for dep in d.get('depends') or []:
            if str(dep) in docs:
                out.append('  %s --> %s' % (node(str(dep)), node(d['id'])))
    out += ['```', '']
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
    """小节目录：每份规范文档的每个 ##、### 小节一行，带行号和字数，供 grep 定位（ENG-ONBOARD §4.1）。"""
    items = []
    for p in glob.glob(os.path.join(D, '**', '*.md'), recursive=True):
        rel = os.path.relpath(p, D).replace(chr(92), '/')
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
    items.sort(key=lambda x: (x[0], x[1], x[2]))
    out = ['# 小节目录', '',
           '> 由 `scripts/docs/gen_index.py` 生成，不要手改。每份规范文档的每个小节一行：`编码 §号 标题 · L行号 · 字数`；'
           '★ 标出各文档的接入要点。用法：按关键词或编码 grep 本文件，再按行号只读原文的那一段（ENG-ONBOARD §4.1）。'
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


def main():
    docs = load()
    t = open(README, 'rb').read().decode('utf-8')
    i = t.find(MARK)
    if i < 0:
        sys.exit('README 里找不到 "%s"' % MARK)
    open(README, 'wb').write((t[:i] + build(docs)).encode('utf-8'))
    print('docs', len(docs), '-> README §7–§9 regenerated')
    s = build_sections()
    open(SECTIONS, 'wb').write(s.encode('utf-8'))
    print('SECTIONS.md', s.count(NL + '- ') + s.count(NL + '  - '), 'sections,', len(s), 'chars')


if __name__ == '__main__':
    main()
