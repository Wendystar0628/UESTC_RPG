"""项目采集：读权威来源，写 dashboard/data/auto 下的六份项目数据（queue、structure、docs、decisions、health、audit-auto），
并顺手删掉 curated/notices 里过期的通知。形状见 dashboard/README.md §2.1。
只读来源、不改它们；任何一份采集失败只跳过那一份，在 stderr 说一句，不影响其余。

用法：python -I -X utf8 dashboard/tools/collect.py          （巡逻每 10 分钟跑一次，也可手动）
      from collect import main; main()                       （巡逻 import 调用，不交互、不退出）
"""
import calendar
import glob
import json
import os
import re
import subprocess
import sys
import time
from datetime import datetime

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)  # python -I 不带脚本目录
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'docs'))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'build'))
sys.path.insert(0, os.path.join(ROOT, '.collab', 'tools', 'watch'))

import datafile  # noqa: E402

SOURCE = 'collect.py'
STATUS_FILE = '.collab/status/施工状态.md'
STATES = ('进行中', '待编译', '等前置', '可派', '暂停')
RECENT_DECISIONS = 15
TEXT_MAX = 44      # 一句话字段的上限（约 40 个汉字）
SUMMARY_MAX = 60
QUICK_TEST_S = 2.0  # 与 patrol 的快检口径一致
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


def iso_from_utc(text):
    """'2026-10-18T13:58:30.5Z' → 本地时刻 ISO；解析不了返回原文。"""
    try:
        return iso(calendar.timegm(time.strptime(text[:19], '%Y-%m-%dT%H:%M:%S')))
    except ValueError:
        return text


def git(*args):
    try:
        r = subprocess.run(['git', '-C', ROOT, *args], capture_output=True, timeout=30)
        return r.stdout.decode('utf-8', 'ignore') if r.returncode == 0 else ''
    except Exception:
        return ''


def brief(text, limit=TEXT_MAX):
    """收成一句话：去掉强调、反引号与括号补充，取第一个句号或分号前；太短再并一句，太长在最近的逗号处断开。"""
    text = re.sub(r'\([^()]*\)|（[^（）]*）', '', re.sub(r'[`*]', '', text or '')).strip()
    parts = [p.strip() for p in re.split(r'[。；]', text) if p.strip()]
    out = parts[0] if parts else ''
    if len(out) < 8 and len(parts) > 1:
        out += '，' + parts[1]
    if len(out) > limit:
        cut = max(out.rfind(c, 0, limit) for c in '，、：')
        out = out[:cut] if cut >= 10 else out[:limit - 1] + '…'
    return out


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
    text = read_text(STATUS_FILE)
    head = lambda key: next((l.split('：', 1)[1] for l in text.splitlines() if key in l and '：' in l), '')
    rows, packages = [], []
    for l in section(text, '## 施工队列').splitlines():
        c = cells(l)
        if len(c) >= 6 and re.match(r'^[A-Z]-\d+[a-z]*$', c[0]):
            rows.append({'id': c[0], 'title': re.split(r'[。（(]', c[1], 1)[0].strip()[:TEXT_MAX], 'status': main_status(c[3]),
                         'who': c[4].lower(), 'dirs': c[2], 'pre': c[5]})
    for l in section(text, '## 活跃工作包').splitlines():
        c = cells(l)
        if len(c) >= 7 and re.match(r'^WP-\w+$', c[0]):
            block = '' if c[6] in ('—', '-') else brief(c[6])
            packages.append({'id': c[0], 'goal': brief(c[1]), 'status': main_status(c[5]), 'block': block})
    counts = {s: 0 for s in STATES}
    for r in rows:
        counts[r['status']] = counts.get(r['status'], 0) + 1
    return {'stage': brief(head('当前阶段')), 'priority': brief(head('当前优先级')),
            'counts': counts, 'rows': rows, 'packages': packages}


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
        if len(summary) > SUMMARY_MAX:
            summary = summary[:SUMMARY_MAX - 1] + '…'
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

def test_record():
    """门禁测试记录（运行时刻、测试名、秒数、结果）里最近一次运行：时刻、超 2 s 的个数、失败个数。"""
    p = os.path.join(ROOT, 'output', 'build', 'test-record.tsv')
    if not os.path.isfile(p):
        return None, 0, 0
    rows = [l.split('\t') for l in open(p, encoding='utf-8').read().splitlines()]
    rows = [r for r in rows if len(r) == 4]
    if not rows:
        return None, 0, 0
    last = max(r[0] for r in rows)
    run = [r for r in rows if r[0] == last]
    return iso_from_utc(last), sum(1 for r in run if float(r[2]) > QUICK_TEST_S), sum(1 for r in run if r[3] == 'Failed')


def tests_health():
    import patrol
    (count, loc), ratio, _, _ = patrol.test_health()
    last, slow, failed = test_record()
    return {'count': count, 'loc': loc, 'ratio': round(ratio, 3), 'slow': slow, 'failed': failed, 'lastRun': last}


def gate_health():
    files = glob.glob(os.path.join(ROOT, 'output', 'build', 'gates', '*-result.json'))
    if not files:
        return None
    data = json.load(open(max(files, key=os.path.getmtime), encoding='utf-8-sig'))
    plan = data.get('Plan', {})
    bad = [r.get('Gate') for r in data.get('Results', []) if r.get('Status') != 'Passed']
    return {'at': iso_from_utc(data.get('GeneratedAt', '')), 'scope': plan.get('Scope', ''), 'wp': plan.get('WorkPackage', ''),
            'result': 'failed' if bad else 'passed', 'failedGates': bad}


def git_health():
    recent = []
    for l in git('log', '-5', '--format=%h%x09%s%x09%ct').splitlines():
        h, subject, ts = l.split('\t')
        recent.append({'hash': h, 'subject': subject, 'at': iso(int(ts))})
    unpushed = git('rev-list', '--count', '@{u}..HEAD').strip()
    return {'head': git('rev-parse', '--short', 'HEAD').strip(), 'unpushed': int(unpushed) if unpushed else None, 'recent': recent}


def docs_over():
    out = subprocess.run([sys.executable, '-I', '-X', 'utf8', os.path.join(ROOT, 'scripts', 'docs', 'doc_size.py'), '--over'],
                         capture_output=True, timeout=60).stdout.decode('utf-8', 'ignore')
    return len(set(re.findall(r'^超限\s+(\S+)', out, re.M)))


def health_data():
    import clean_artifacts
    return {'tests': tests_health(), 'gate': gate_health(), 'git': git_health(),
            'cleanup': {'stale': len(clean_artifacts.stale())}, 'docs': {'over': docs_over()}}


# ---------- audit-auto ----------

def file_item(kind, path, title):
    p = os.path.join(ROOT, path)
    return {'id': f'{kind}:{path}', 'kind': kind, 'title': title, 'path': path, 'since': iso(os.path.getmtime(p))}


def research_items():
    """调研索引里总览还在、审计列写着待审的行。"""
    out = []
    for l in read_text('.collab/research/INDEX.md').splitlines():
        c = cells(l)
        if len(c) >= 6 and re.match(r'^r\d+', c[0]) and c[4] != '已删' and '待审' in c[5]:
            path = c[4].strip('`') if '/' in c[4] else '.collab/research/INDEX.md'
            out.append({'id': f'research:{c[0]}', 'kind': 'research', 'title': brief(f'调研 {c[0]}：{c[3]}'), 'path': path,
                        'since': c[1].replace(' ', 'T') + ':00'})
    return out


def decision_notes():
    """给 Opus 的来信里含【需定】的，标题取第一行。"""
    out = []
    for p in sorted(glob.glob(os.path.join(ROOT, '.collab', 'notes', 'opus', '*.md'))):
        text = read_text(rel(p))
        if '【需定】' in text:
            title = brief(text.strip().splitlines()[0].lstrip('# ').strip())
            out.append(file_item('decision', rel(p), title))
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
            datafile.write('auto', name, job(), source=SOURCE)
        except Exception as e:
            print(f'collect：{name} 采集失败，已跳过（{type(e).__name__}: {e}）', file=sys.stderr)
    try:
        prune_notices()
    except Exception as e:
        print(f'collect：清理过期通知失败，已跳过（{type(e).__name__}: {e}）', file=sys.stderr)


if __name__ == '__main__':
    main()
