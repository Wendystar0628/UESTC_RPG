"""观察台按份采集；live 的快慢通道调用同一生产表。
用法：python -I -X utf8 dashboard/tools/collect.py [数据名…]。
产出：data/auto 的 v3 数据；不写 index，不另开采集守护。
"""
import glob
import os
import re
import sys
import time
import threading
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(ROOT, '.collab', 'tools'))
import datafile
from monitor import util as U
from monitor.read import collab, repo
from monitor.view import attention, progress, team, system, hookfeed, people
SOURCE = 'monitor.view'
_LOCK = threading.RLock()
_VALUES = {}
_RAW = None
_SAMPLE = None
_ROWS = None

def rel(p):
    return os.path.relpath(p, ROOT).replace(os.sep, '/')


def read_text(path):
    return open(os.path.join(ROOT, path), encoding='utf-8-sig', errors='ignore').read().replace('\r\n', '\n')






def front(text):
    """文档头部 front-matter 里的简单键值（只认 `键: 值` 的单行）。"""
    m = re.match(r'^---\n(.*?)\n---\n', text, re.S)
    return dict(re.findall(r'^(\w+):\s*(.*)$', m.group(1), re.M)) if m else {}


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
    # 继续沿用 CORE-INDEX 域及 parent 排序，只保留作者需要的编码、简题、摘要。
    items = []
    for path in doc_files():
        text = read_text(path)
        fm = front(text)
        code = fm.get('id')
        if not code:
            continue
        items.append({'code': code, 'title': fm.get('title', code), 'summary': fm.get('summary', ''),
                      '_parent': fm.get('parent'), '_body': text})
    by_code = {d['code']: d for d in items}
    pattern = code_pattern(by_code)
    top = by_code.get('CORE-INDEX')
    children = {}
    for d in items:
        if d['_parent'] in by_code:
            children.setdefault(d['_parent'], []).append(d)
    for parent, ds in children.items():
        body = by_code[parent]['_body']
        ds.sort(key=lambda d: (body.find(d['code']) if d['code'] in body else len(body), d['code']))
    def subtree(d):
        return [d] + [x for child in children.get(d['code'], []) for x in subtree(child)]
    domains, placed = [], set()
    if top:
        domains.append({'name': '顶层', 'docs': [top]})
        placed.add(top['code'])
    for name, mains in index_domains(top['_body'] if top else '', pattern):
        group = [x for code in mains if code not in placed for x in subtree(by_code[code])]
        placed.update(x['code'] for x in group)
        domains.append({'name': name, 'docs': group})
    loose = [d for d in items if d['code'] not in placed]
    if loose:
        domains.append({'name': '待归入索引', 'docs': loose})
    return {'domains': [{'name': d['name'], 'docs': [{k: x[k] for k in ('code', 'title', 'summary')} for x in d['docs']]} for d in domains]}


# 只列本项目的既有生产方；C261 在同一表接入 v3 分析，不另开采集链。
# inputs 是通知来源名或 @公开数据名；@依赖完成会再标脏此份。
PRODUCERS = {
    'live': lambda context: live_data(context['sample']),
    'progress': lambda context: progress.build(context['rows']),
    'team': lambda context: team.build(context['rows'], context['sample']),
    'hookfeed': lambda context: hookfeed.build(),
    'docs': lambda context: docs_data(),
    'system': lambda context: system.build(required('docs')),
}
INPUTS = {
    'live': {'sessions'}, 'team': {'sessions', 'queue', 'model'},
    'progress': {'queue'}, 'hookfeed': {'hooks', 'local'},
    'attention': {'attention', 'shots', 'worktrees'}, 'feedback': {'letters'},
    'docs': {'docs'}, 'system': {'scripts', 'settings', 'hooks', '@docs'},
    '_git': {'git'}, '_app': {'app'},
}
SLOW_NAMES = {'docs', 'system', '_git', '_app', 'flow', 'collab'}


def required(name):
    with _LOCK:
        value = _VALUES.get(name)
    if value is None or value.get('errors'):
        detail = '；'.join(value['errors']) if value is not None else '尚未完成首次采集'
        raise ValueError('所需来源采集失败：' + name + '：' + detail)
    return value


def live_data(sample):
    leads = []
    for side, who in (('claude', 'Claude'), ('codex', 'Codex')):
        session = next((item for item in sample['sessions'] if item['side'] == side), {})
        state = session.get('state')
        leads.append({'who': who, 'running': True if state == 'running' else False if state == 'idle' else None,
                      'since': session.get('since')})
    return {'leads': leads}


def context(names, recorder=None):
    global _ROWS
    with _LOCK:
        rows, sampled = _ROWS, _SAMPLE
        values = dict(_VALUES)
    rows_error, sample_error = None, None
    if names & {'live', 'team', 'progress'}:
        try:
            rows = collab.queue_rows()
            with _LOCK:
                _ROWS = rows
        except Exception as error:
            rows_error = error
    if names & {'live', 'team'}:
        sampled = None
        try:
            if rows_error is not None:
                raise rows_error
            sampled = people.sample(rows)
        except Exception as error:
            sample_error = error
    with _LOCK:
        raw = _RAW
    return {'rows': rows, 'sample': sampled, 'values': values, 'rows_error': rows_error, 'sample_error': sample_error,
            'raw': raw if raw is not None else {'history': [], 'pushes': [], 'coveredSince': None}}


def run(names, recorder=None, app_paths=()):
    """仅重算点名项，完成一份即发布；返回成功/失败，不吞真实错误体。"""
    global _RAW, _SAMPLE
    names = set(names)
    done, failed = set(), set()
    shared = context(names, recorder)
    if names & {'live', 'team'} and shared['sample'] is not None:
        with _LOCK:
            _SAMPLE = shared['sample']
        if recorder is not None:
            recorder.observe_sample(shared['sample'])
    if '_git' in names:
        try:
            raw = repo.progress_facts()
            with _LOCK:
                _RAW = raw
            done.add('_git')
        except Exception as error:
            failed.add('_git')
            with _LOCK:
                _RAW = {'errors': [f'{type(error).__name__}: {error}']}
            print(f'collect：Git来源采集失败（{type(error).__name__}: {error}）', file=sys.stderr)
    if '_app' in names:
        datafile.publish_app(app_paths)
        done.add('_app')
    for name in sorted(names - {'_git', '_app', 'attention', 'feedback'}, key=lambda item: (item not in ('live', 'progress'), item)):
        try:
            if name not in PRODUCERS:
                raise ValueError('未接入生产方：' + name)
            if name in ('live', 'team', 'progress') and shared['rows_error'] is not None:
                raise shared['rows_error']
            if name in ('live', 'team') and shared['sample_error'] is not None:
                raise shared['sample_error']
            data = PRODUCERS[name](shared)
        except Exception as error:
            message = f'{type(error).__name__}: {error}'
            print(f'collect：{name} 采集失败（{message}）', file=sys.stderr)
            data = {'errors': [message]}
        datafile.write('auto', name, data, source=SOURCE + '.' + name)
        with _LOCK:
            _VALUES[name] = data
        (failed if data.get('errors') else done).add(name)
    selected = names & {'attention', 'feedback'}
    if selected:
        result = attention.refresh(datafile.write, names=tuple(sorted(selected)))
        for name, data in result.items():
            with _LOCK:
                _VALUES[name] = data
            (failed if data.get('errors') else done).add(name)
    return done, failed


def next_expiry():
    """同一会话事实的时间阈值：活跃静默分钟、过时与交回一小时到期。"""
    now = time.time()
    with _LOCK:
        sample = _SAMPLE
    if sample is None:
        return None
    deadlines = []
    for agent in sample['agents']:
        last = U.epoch(agent['last']) if agent.get('last') else None
        if last and agent.get('state') in ('running', 'stalled', 'unknown'):
            deadlines.append(last + (int(max(0, now - last) / 60) + 1) * 60)
            deadlines.extend(last + minutes * 60 for minutes in (U.STALL_MIN, U.ABANDON_MIN) if last + minutes * 60 > now)
        end = agent.get('completedAt') or agent.get('endedAt')
        ended = U.epoch(end) if end else None
        if ended and ended + 3600 > now:
            deadlines.append(ended + 3600)
    return min(deadlines) if deadlines else None


def main():
    names = set(sys.argv[1:] or INPUTS)
    unknown = names - set(INPUTS)
    if unknown:
        raise ValueError('未接入数据：' + ', '.join(sorted(unknown)))
    if '_app' in names:
        datafile.publish_app()
        names.remove('_app')
    # 一次性人工入口同样按份发布；常驻实时入口只由 live 持有。
    failed = set()
    for batch in ({'_git'} & names, names - SLOW_NAMES, names & SLOW_NAMES - {'_git'}):
        if batch:
            _, errors = run(batch)
            failed.update(errors)
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
