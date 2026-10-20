"""观察台数据文件的读写：每个数据文件是一行 `Dash.put("名字", <JSON>);`，页面用 <script> 直接加载（file:// 下也能用）。
产出：dashboard/data/auto/*.js、dashboard/data/curated/*.js、.collab/tools/state/dashboard-app.json
采集脚本、发布工具都经这里读写，不手写数据文件。写入先写临时文件再改名，页面不会读到半个文件。

用法：
  from datafile import read, write
  write('auto', 'progress', {...})     # dashboard/data/auto/progress.js
  data = read('curated', 'notices')    # 没有这个文件时返回 None
"""
import json
import os
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # dashboard/
DATA = os.path.join(ROOT, 'data')
KINDS = ('auto', 'curated')


def path(kind, name):
    assert kind in KINDS, kind
    return os.path.join(DATA, kind, name + '.js')


def read(kind, name):
    p = path(kind, name)
    if not os.path.isfile(p):
        return None
    text = open(p, encoding='utf-8').read().strip()
    head = f'Dash.put({json.dumps(name)}, '
    if not (text.startswith(head) and text.endswith(');')):
        raise ValueError(f'{p} 不是观察台数据文件')
    return json.loads(text[len(head):-2])


def write(kind, name, payload, source=None, force=False):
    """payload 是 dict；自动补 updated（ISO 时刻）与 source（谁写的）。"""
    payload = dict(payload)
    if source:
        payload['source'] = source
    prior = read(kind, name)
    content = lambda d: {k: v for k, v in (d or {}).items() if k != 'updated'}
    if not force and prior is not None and content(prior) == content(payload):
        return path(kind, name)
    payload['updated'] = time.strftime('%Y-%m-%dT%H:%M:%S')
    p = path(kind, name)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    tmp = p + '.' + str(os.getpid()) + '.tmp'
    with open(tmp, 'w', encoding='utf-8') as f:
        f.write(f'Dash.put({json.dumps(name)}, {json.dumps(payload, ensure_ascii=False)});\n')
    os.replace(tmp, p)
    return p


def app_stamp():
    """以文件集合的高精度版本发号；不使用最大mtime，删除或同秒改动也会换版本。"""
    state = os.path.join(ROOT, '..', '.collab', 'tools', 'state', 'dashboard-app.json')
    files = [os.path.join(ROOT, 'index.html')]
    for directory in ('app', 'vendor'):
        files += [os.path.join(r, f) for r, _, fs in os.walk(os.path.join(ROOT, directory)) for f in fs]
    signature = []
    for p in sorted(files):
        if os.path.isfile(p):
            stat = os.stat(p)
            signature.append([os.path.relpath(p, ROOT).replace('\\', '/'), stat.st_mtime_ns, stat.st_size])
    old = json.load(open(state, encoding='utf-8')) if os.path.isfile(state) else {}
    if old.get('files') != signature:
        old = {'files': signature, 'version': str(time.time_ns())}
        os.makedirs(os.path.dirname(state), exist_ok=True)
        tmp = state + '.' + str(os.getpid()) + '.tmp'
        with open(tmp, 'w', encoding='utf-8') as f:
            json.dump(old, f)
        os.replace(tmp, state)
    return old['version']


AUTO_NAMES = ('live', 'attention', 'feedback', 'progress', 'team', 'system', 'hookfeed', 'stats', 'records', 'docs')
RETIRED = ('pipeline', 'rowhistory', 'letters', 'audits', 'relations', 'docrefs', 'models', 'history',
           'catalog', 'catalogdetail', 'queue', 'structure', 'health', 'decisions', 'audit-auto')


def remove_retired():
    for name in RETIRED:
        target = path('auto', name)
        if os.path.isfile(target):
            os.remove(target)


def refresh_index():
    versions = {}
    for kind, names in (('auto', AUTO_NAMES), ('curated', ('notices',))):
        for name in names:
            target = path(kind, name)
            if os.path.isfile(target):
                stat = os.stat(target)
                versions[name] = f'{stat.st_mtime_ns}-{stat.st_size}'
    write('auto', 'index', {'versions': versions}, source='datafile.py', force=True)
