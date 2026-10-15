"""由各文档的文件头重新生成 docs/design/README.md 的 §7 文档地图、§8 工作包与文档、§9 文档依赖图。

用法：python -I scripts/docs/gen_index.py
README 里从 "## 7 文档地图" 开始到文件末尾的内容会被整体替换，§0–§6 手写部分不动。
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
        if os.path.basename(rel) == 'README.md':
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
            '主责与增量取自各文档文件头的 `owners`、`contributors`；独占目录、必读与交付以 ENG-WP 为准。', '',
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


def main():
    docs = load()
    t = open(README, 'rb').read().decode('utf-8')
    i = t.find(MARK)
    if i < 0:
        sys.exit('README 里找不到 "%s"' % MARK)
    open(README, 'wb').write((t[:i] + build(docs)).encode('utf-8'))
    print('docs', len(docs), '-> README §7–§9 regenerated')


if __name__ == '__main__':
    main()
