"""项目采集：读权威来源，写 dashboard/data/auto 下的六份项目数据（queue、structure、docs、decisions、health、audit-auto），
并顺手删掉 curated/notices 里过期的通知。形状见 dashboard/README.md §2.1。
只读来源、不改它们；任何一份采集失败只跳过那一份，在 stderr 说一句，不影响其余。

用法：python -I -X utf8 dashboard/tools/collect.py          （巡逻每 10 分钟跑一次，也可手动）
      from collect import main; main()                       （巡逻 import 调用，不交互、不退出）
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
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'docs'))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'build'))
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

# 文档域（CORE-INDEX §1）：键、名称、所在目录
DOMAINS = [('collab', '协作'), ('eng', '工程'), ('plan', '计划与登记'), ('art', '画面'),
           ('sys', '系统与界面'), ('world', '世界规则'), ('map', '地图：清水河')]
DOMAIN_OF_DIR = {'workflow': 'collab', 'engineering': 'eng', 'ops': 'eng', 'tools': 'eng', 'core': 'plan',
                 'art': 'art', 'systems': 'sys', 'ui': 'sys', 'world': 'world', 'maps': 'map', '.': 'plan'}
PLAN_CODES = ('ENG-WP', 'ENG-ROADMAP')  # 放在 engineering/ 里但属于计划与登记

# 项目分层（ENG-ARCH 分层）：层名 → 目录；game/gen、game/kernel、game/systems 取下一层
LAYERS = ['契约', '世界与公共几何', '内容生成', '运行时', '数据与工具']
SPLIT = ('game/gen', 'game/kernel', 'game/systems')
LAYER_OF = {'game/contracts': '契约',
            'game/gen/world': '世界与公共几何', 'game/gen/geometry': '世界与公共几何', 'game/gen/parts': '世界与公共几何',
            'game/data': '数据与工具', 'game/assets': '数据与工具', 'game/shared': '数据与工具',
            'pipeline': '数据与工具', 'scripts': '数据与工具', 'docs': '数据与工具'}
TOP_DIRS = ['game/contracts', 'game/gen', 'game/kernel', 'game/stage', 'game/flow', 'game/camera', 'game/systems',
            'game/style', 'game/audio', 'game/hud', 'game/data', 'game/assets', 'game/shared', 'pipeline', 'scripts', 'docs']
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
    head = lambda key: next((l.split('：', 1)[1] for l in collab.status_head().splitlines() if key in l and '：' in l), '')
    rows = [{'id': r['id'], 'title': r['what'], 'status': main_status(r['state']), 'who': r['who'].lower(), 'dirs': r['dirs'], 'pre': r['pre']}
            for r in collab.queue_rows()]
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


def parent_code(code, codes):
    """主文档：同目录里编码是它前缀（以 - 分段）的最长一份；没有就 None。"""
    best = [c for c in codes if c != code and code.startswith(c + '-')]
    return max(best, key=len) if best else None


def doc_files():
    out = []
    for p in sorted(glob.glob(os.path.join(ROOT, 'docs', '**', '*.md'), recursive=True)):
        r = rel(p)
        if not r.startswith('docs/godot-api/') and r != 'docs/SECTIONS.md':
            out.append(r)
    return out


def domain_of(path, code):
    if code.startswith(PLAN_CODES):
        return 'plan'
    parts = path.split('/')
    return DOMAIN_OF_DIR.get(parts[1] if len(parts) > 2 else '.', 'plan')


def docs_data():
    import doc_size
    sections = doc_sections()
    items = []
    for path in doc_files():
        text = read_text(path)
        fm = front(text)
        code, chars, _ = doc_size.measure(os.path.join(ROOT, path))
        summary = fm.get('summary', '')
        items.append({'code': code, 'title': fm.get('title', code), 'path': path, 'summary': summary,
                      'chars': chars, 'parent': None, 'sections': sections.get(code, []), '_body': text})
    for d in items:
        same = [x['code'] for x in items if os.path.dirname(x['path']) == os.path.dirname(d['path'])]
        d['parent'] = parent_code(d['code'], same)
    # 引用关系：正文里出现的其他文档编码，按 from→to 计次；长编码优先，免得 ENG-GATES 吃掉 ENG-GATES-SHOTS
    codes = sorted({d['code'] for d in items}, key=len, reverse=True)
    pattern = re.compile(r'(?<![A-Za-z0-9-])(' + '|'.join(re.escape(c) for c in codes) + r')(?![A-Za-z0-9])')
    edges = {}
    for d in items:
        for to in pattern.findall(d.pop('_body')):
            if to != d['code']:
                edges[(d['code'], to)] = edges.get((d['code'], to), 0) + 1
    domains = [{'key': k, 'name': n, 'docs': sorted((d for d in items if domain_of(d['path'], d['code']) == k),
                                                    key=lambda d: d['code'])} for k, n in DOMAINS]
    return {'domains': [d for d in domains if d['docs']],
            'edges': [{'from': a, 'to': b, 'n': n} for (a, b), n in sorted(edges.items())]}


# ---------- decisions ----------

def decisions_data():
    rows = []
    for p in sorted(glob.glob(os.path.join(ROOT, 'docs', 'core', 'CORE-DECISIONS-*.md'))):
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
