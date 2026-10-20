"""项目采集：读权威来源，写 dashboard/data/auto 下的六份项目数据（queue、structure、docs、decisions、health、audit-auto），
并顺手删掉 curated/notices 里过期的通知。形状见 dashboard/README.md §2.1。
只读来源、不改它们；任何一份采集失败只跳过那一份，在 stderr 说一句，不影响其余。

用法：python -I -X utf8 dashboard/tools/collect.py          （live.py --loop 在输入一变时跑、无变化时每 2 分钟周期刷新；也可手动）
      from collect import main; main()                       （live.py import 调用，不交互、不退出）
"""
import glob
import os
import re
import sys
import time
from datetime import datetime

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)  # python -I 不带脚本目录
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(ROOT, '.collab', 'tools'))  # 监控库 monitor：本机协作数据（.collab）只经它读，这是仓库代码引用 .collab 的唯一例外

import datafile  # noqa: E402
from monitor.read import collab, wpnames  # noqa: E402

SOURCE = 'collect.py'
STATES = ('进行中', '待编译', '等前置', '可派', '暂停')
RECENT_DECISIONS = 15
CODE_EXT = ('.cs', '.gd', '.py', '.ps1', '.json', '.md')  # 另有 .gdshader*
SKIP_DIRS = {'bin', 'obj', '.godot', '.venv', '__pycache__', 'godot-api', 'addons', 'node_modules'}
SKIP_PATHS = {'game/bake'}  # 本机生成的暂存
MAX_DEPTH = 4  # 目录树列到离仓库根第几层

# 文档地图照 AI 读的结构：顶层 CORE-INDEX，其下按 CORE-INDEX §1 的域与主文档，子文档按文件头 parent 挂到主文档下
TOP_DOC = 'docs/README.md'  # 唯一的顶层文档（CORE-INDEX）

# 项目分层（ENG-ARCH 分层）：层名 → 目录；game/gen、game/kernel、game/systems 取下一层
LAYERS = ['契约', '世界与公共几何', '内容生成', '运行时', '数据与工具']
SPLIT = ('game/gen', 'game/kernel', 'game/systems')
LAYER_OF = {'game/contracts': '契约',
            'game/gen/world': '世界与公共几何', 'game/gen/geometry': '世界与公共几何', 'game/gen/parts': '世界与公共几何',
            'game/data': '数据与工具', 'game/assets': '数据与工具', 'game/testing': '数据与工具',
            'pipeline': '数据与工具', 'scripts': '数据与工具', 'docs': '数据与工具'}
TOP_DIRS = ['game/contracts', 'game/gen', 'game/kernel', 'game/stage', 'game/flow', 'game/camera', 'game/systems',
            'game/style', 'game/audio', 'game/hud', 'game/data', 'game/assets', 'game/testing', 'pipeline', 'scripts', 'docs']
EXTRA_NOTE = {'数据与工具': '地图数据、离线管线、构建与门禁脚本、开发文档'}


# ---------- 通用 ----------

def rel(p):
    return os.path.relpath(p, ROOT).replace(os.sep, '/')


def read_text(path):
    return open(os.path.join(ROOT, path), encoding='utf-8-sig', errors='ignore').read().replace('\r\n', '\n')


def iso(ts):
    return time.strftime('%Y-%m-%dT%H:%M:%S', time.localtime(ts))


def brief(text):
    """去掉强调与反引号，收成一行；不截断（页面先显示摘要，细节点开看）。"""
    return re.sub(r'\s+', ' ', re.sub(r'[`*]', '', text or '')).strip()


def cells(line):
    return [c.strip() for c in line.strip().strip('|').split('|')]


def section(text, heading):
    """二级标题 heading 下的正文，到下一个二级标题为止。"""
    if heading not in text:
        return ''
    return text.split(heading, 1)[1].split('\n## ', 1)[0]


def front(text):
    """文档头部 front-matter 里的简单键值（只认 `键: 值` 的单行）。"""
    m = re.match(r'^---\n(.*?)\n---\n', text, re.S)
    return dict(re.findall(r'^(\w+):\s*(.*)$', m.group(1), re.M)) if m else {}


# ---------- queue ----------

def main_status(cell):
    return re.split(r'[（(]', cell, 1)[0].strip()


def queue_data():
    from monitor.check.flow import queue_fixes   # 可派与等前置怎么维护：判据只在巡逻那一份检查里，这里取同一份结果给施工页
    head = lambda key: next((l.split('：', 1)[1] for l in collab.status_head().splitlines() if key in l and '：' in l), '')
    source = collab.queue_rows()
    fixes = {i: text for i, _kind, text in queue_fixes(source)}
    rows = [{'id': r['id'], 'title': r['what'], 'status': main_status(r['state']), 'who': r['who'].lower(), 'dirs': r['dirs'], 'pre': r['pre'],
             'fix': fixes.get(r['id'])} for r in source]
    counts = {s: 0 for s in STATES}
    for r in rows:
        counts[r['status']] = counts.get(r['status'], 0) + 1
    return {'stage': brief(head('当前阶段')), 'priority': brief(head('当前优先级')), 'counts': counts, 'rows': rows}


# ---------- structure ----------

def scan_dir(path):
    """top 目录下每个目录"自己直接含"的 [文件数, 行数, 测试行数]（tests 子目录的行数记在它的父目录上）。"""
    own = {}
    for r, ds, fs in os.walk(os.path.join(ROOT, path)):
        ds[:] = [d for d in ds if d not in SKIP_DIRS and rel(os.path.join(r, d)) not in SKIP_PATHS]
        parts = rel(r).split('/')
        in_tests = 'tests' in parts
        owner = '/'.join(parts[:parts.index('tests')]) if in_tests else rel(r)
        for f in fs:
            if not (f.endswith(CODE_EXT) or '.gdshader' in f):
                continue
            n = open(os.path.join(r, f), 'rb').read().count(b'\n')
            c = own.setdefault(owner, [0, 0, 0])
            if in_tests:
                c[2] += n
            else:
                c[0] += 1
                c[1] += n
    return own


def total_of(own, path):
    """path 及其全部后代的合计 (files, loc, testLoc)。"""
    t = [0, 0, 0]
    for k, c in own.items():
        if k == path or k.startswith(path + '/'):
            t = [a + b for a, b in zip(t, c)]
    return t


def dir_note(path):
    """目录自己的 README 第一句；没有就留空。"""
    p = os.path.join(ROOT, path, 'README.md')
    if not os.path.isfile(p):
        return ''
    body = [l.strip() for l in read_text(rel(p)).splitlines() if l.strip() and not l.startswith(('#', '---', '>'))]
    return brief(body[0]) if body else ''


def layer_notes():
    """ENG-ARCH 分层表的"层 → 责任"，责任取第一句。"""
    notes = {}
    for l in section(read_text('docs/engineering/ENG-ARCH-工程架构.md'), '### 分层').splitlines():
        c = cells(l)
        if len(c) >= 3 and c[0] in LAYERS:
            notes[c[0]] = brief(c[2])
    return {**notes, **EXTRA_NOTE}


def layer_of(path):
    if path in LAYER_OF:
        return LAYER_OF[path]
    if path.startswith('game/gen/'):
        return '内容生成'
    if path.startswith('game/'):
        return '运行时'
    return '数据与工具'


def structure_dirs():
    out = []
    for top in TOP_DIRS:
        if not os.path.isdir(os.path.join(ROOT, top)):
            continue
        if top in SPLIT:
            subs = sorted(d for d in os.listdir(os.path.join(ROOT, top))
                          if os.path.isdir(os.path.join(ROOT, top, d)) and d not in SKIP_DIRS and d != 'tests')
            out += [f'{top}/{d}' for d in subs]
        else:
            out.append(top)
    return out


def dir_node(own, path):
    """目录节点：合计 + 子目录（到离仓库根第 4 层；只列含统计文件的）。"""
    files, loc, test_loc = total_of(own, path)
    kids = []
    if path.count('/') < MAX_DEPTH - 1:
        subs = sorted({k[len(path) + 1:].split('/')[0] for k in own if k.startswith(path + '/')})
        kids = [dir_node(own, f'{path}/{d}') for d in subs]
    return {'path': path, 'files': files, 'loc': loc, 'testLoc': test_loc, 'note': dir_note(path), 'children': kids}


def structure_data():
    notes = layer_notes()
    layers = {n: [] for n in LAYERS}
    for path in structure_dirs():
        own = scan_dir(path)
        files, _, test_loc = total_of(own, path)
        if not (files or test_loc):
            continue  # 空目录不列
        layers[layer_of(path)].append(dir_node(own, path))
    return {'layers': [{'name': n, 'note': notes.get(n, ''), 'dirs': layers[n]} for n in LAYERS if layers[n]]}


# ---------- docs ----------

def chars_of(text):
    """'1.4k' → 1400，'119' → 119。"""
    return round(float(text[:-1]) * 1000) if text.endswith('k') else int(text)


def doc_sections():
    """docs/SECTIONS.md 里每份文档的节树（编码 → [{id, title, chars, children}]）：顶级节为一级，缩进两格的挂在上一个顶级节下。"""
    out, cur, top = {}, None, None
    for l in read_text('docs/SECTIONS.md').splitlines():
        m = re.match(r'^## (\S+) · ', l)
        if m:
            cur, top = m.group(1), None
            out[cur] = []
            continue
        m = re.match(r'^( {0,2})- (?:★ )?(\S+) (§[\d.]+) (.*) · L\d+ · (\S+)$', l)
        if not (cur and m):
            continue
        node = {'id': m.group(3), 'title': m.group(4), 'chars': chars_of(m.group(5)), 'children': []}
        if m.group(1):
            if top is not None:
                top['children'].append(node)
        else:
            out[cur].append(node)
            top = node
    return out


def code_pattern(codes):
    """匹配文档编码的正则：长编码优先，免得 ENG-GATES 吃掉 ENG-GATES-SHOTS。"""
    return re.compile(r'(?<![A-Za-z0-9-])(' + '|'.join(re.escape(c) for c in sorted(codes, key=len, reverse=True)) + r')(?![A-Za-z0-9-])')


def index_domains(index_text, pattern):
    """CORE-INDEX §1 文档地图里的域与主文档，顺序照原文：[(域名, [编码…])]。域是"**名字**（`文件夹`）"开头的段，
    段里先出现的编码是它的主文档；"信息在哪""仓库目录"这类不带文件夹的段不算域，一份文档只归第一次出现的那个域。"""
    m = re.search(r'^## 1 .*?(?=^## |\Z)', index_text, re.S | re.M)
    seen, out = set(), []
    for block in re.split(r'\n(?=\*\*)', m.group(0) if m else ''):
        name = re.match(r'\*\*([^*]+)\*\*（', block)
        codes = [c for c in dict.fromkeys(pattern.findall(block)) if c not in seen]
        if name and codes:
            seen.update(codes)
            out.append((name.group(1), codes))
    return out


def doc_files():
    out = []
    for p in sorted(glob.glob(os.path.join(ROOT, 'docs', '**', '*.md'), recursive=True)):
        r = rel(p)
        if not r.startswith('docs/godot-api/') and r != 'docs/SECTIONS.md':
            out.append(r)
    return out


def docs_data():
    import runpy
    doc_size = runpy.run_path(os.path.join(ROOT, 'scripts', 'docs', 'doc-size.py'))
    sections = doc_sections()
    items = []
    for path in doc_files():
        text = read_text(path)
        fm = front(text)
        code, chars, _ = doc_size['measure'](os.path.join(ROOT, path))
        summary = fm.get('summary', '')
        items.append({'code': code, 'title': fm.get('title', code), 'path': path, 'summary': summary,
                      'chars': chars, 'parent': fm.get('parent') or None, 'sections': sections.get(code, []), '_body': text})
    by_code = {d['code']: d for d in items}
    pattern = code_pattern(by_code)
    top = next((d for d in items if d['path'] == TOP_DOC), None)
    # 树：§1 的主文档照原文顺序，子文档照它在主文档里第一次被提到的位置
    kids = {}
    for d in items:
        if d['parent'] in by_code:
            kids.setdefault(d['parent'], []).append(d)
    for parent, ds in kids.items():
        body = by_code[parent]['_body']
        ds.sort(key=lambda d: (body.find(d['code']) if d['code'] in body else len(body), d['code']))

    def subtree(d):
        return [d] + [x for k in kids.get(d['code'], []) for x in subtree(k)]

    domains, placed = [], {top['code']} if top else set()
    for i, (name, mains) in enumerate(index_domains(top['_body'] if top else '', pattern)):
        docs = [x for c in mains if c not in placed for x in subtree(by_code[c])]
        placed.update(x['code'] for x in docs)
        domains.append({'key': 'd%d' % i, 'name': name, 'docs': docs})
    loose = [d for d in items if d['code'] not in placed]   # 索引里找不到的（check-docs 会报），单列出来不藏
    if loose:
        domains.append({'key': 'loose', 'name': '索引里没有', 'docs': loose})
    # 引用关系：正文里出现的其他文档编码，按 from→to 计次
    edges = {}
    for d in items:
        for to in pattern.findall(d.pop('_body')):
            if to != d['code']:
                edges[(d['code'], to)] = edges.get((d['code'], to), 0) + 1
    return {'top': top, 'domains': domains,
            'edges': [{'from': a, 'to': b, 'n': n} for (a, b), n in sorted(edges.items())]}


# ---------- decisions ----------

def decisions_data():
    rows = []
    for p in sorted(glob.glob(os.path.join(ROOT, 'docs', 'registry', 'REG-DECISIONS-*.md'))):
        for l in read_text(rel(p)).splitlines():
            c = cells(l)
            if len(c) >= 5 and re.match(r'^D-\d+$', c[0]):
                rows.append((int(c[0][2:]), {'id': c[0], 'text': brief(c[1]), 'status': main_status(c[-1]), 'scope': c[2]}))
    rows.sort(key=lambda r: -r[0])
    return {'total': len(rows), 'recent': [r for _, r in rows[:RECENT_DECISIONS]]}


# ---------- health ----------

def health_data():
    """健康检查只有一份实现（.collab/tools/monitor/check/），巡逻读同一份；这里把全部检查写成 health.js。"""
    from monitor import check
    from monitor.record import public
    return {'checks': [public(r) for r in check.run_all()]}


# ---------- audit-auto ----------

def file_item(kind, path, title):
    p = os.path.join(ROOT, path)
    return {'id': f'{kind}:{path}', 'kind': kind, 'title': title, 'path': path, 'since': iso(os.path.getmtime(p))}


def research_items():
    """调研索引里总览还在、审计列写着待审的行。"""
    out = []
    for l in collab.research_index().splitlines():
        c = cells(l)
        if len(c) >= 6 and re.match(r'^r\d+', c[0]) and c[4] != '已删' and '待审' in c[5]:
            path = c[4].strip('`') if '/' in c[4] else '.collab/research/INDEX.md'
            out.append({'id': f'research:{c[0]}', 'kind': 'research', 'title': brief(f'调研 {c[0]}：{c[3]}'), 'path': path,
                        'since': c[1].replace(' ', 'T') + ':00'})
    return out


def decision_notes():
    """给主会话的来信里含【需定】的，标题取第一行。"""
    out = []
    for n in collab.notes('claude'):
        if '【需定】' in n['text']:
            rel_path = os.path.relpath(n['path'], ROOT).replace(os.sep, '/')
            out.append(file_item('decision', rel_path, brief(n['text'].strip().splitlines()[0].lstrip('# '))))
    return out


def audit_data():
    items = []
    if os.path.isfile(os.path.join(ROOT, 'output', 'shots', '总览.jpg')):
        items.append(file_item('shots', 'output/shots/总览.jpg', '固定机位总览截图'))
    for p in sorted(glob.glob(os.path.join(ROOT, 'output', 'posters', '*.pdf'))):
        items.append(file_item('poster', rel(p), os.path.splitext(os.path.basename(p))[0]))
    for p in sorted(glob.glob(os.path.join(ROOT, 'output', 'reports', '*.html'))):
        items.append(file_item('report', rel(p), os.path.splitext(os.path.basename(p))[0]))
    return {'items': items + research_items() + decision_notes()}


# ---------- 过期通知 ----------

def expired(item, now):
    """有 expires、早于现在、又没钉住的通知。"""
    if item.get('pinned') or not item.get('expires'):
        return False
    try:
        at = datetime.fromisoformat(item['expires'])
    except ValueError:
        return False
    return (at.timestamp() if at.tzinfo else time.mktime(at.timetuple())) < now


def prune_notices():
    data = datafile.read('curated', 'notices')
    if not data:
        return
    now = time.time()
    keep = [i for i in data.get('items', []) if not expired(i, now)]
    if len(keep) != len(data.get('items', [])):
        datafile.write('curated', 'notices', {**data, 'items': keep})


# ---------- 入口 ----------

def main():
    jobs = [('queue', queue_data), ('structure', structure_data), ('docs', docs_data),
            ('decisions', decisions_data), ('health', health_data), ('audit-auto', audit_data)]
    for name, job in jobs:
        try:
            data = job()
            if name != 'docs':  # 文档目录是各文档标题原文，文档自己带名
                data = wpnames.annotate_tree(data)  # 工作包号在写文件处配中文包名
            datafile.write('auto', name, data, source=SOURCE)
        except Exception as e:
            print(f'collect：{name} 采集失败，已跳过（{type(e).__name__}: {e}）', file=sys.stderr)
    try:
        prune_notices()
    except Exception as e:
        print(f'collect：清理过期通知失败，已跳过（{type(e).__name__}: {e}）', file=sys.stderr)


if __name__ == '__main__':
    main()
