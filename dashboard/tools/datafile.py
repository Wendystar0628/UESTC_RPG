"""观察台唯一数据发布入口；内容签名与确认时刻在同一短锁内提交。
产出：dashboard/data/{auto,curated}/*.js、state/dashboard-publication.json。
"""
import hashlib
import json
import os
import sys
import threading
import time
import uuid
from catalog import DATASETS, CURATED_NAMES

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, 'data')
KINDS = ('auto', 'curated')
# 公开名单与判鲜参数只从数据登记派生，保留登记顺序。
AUTO_NAMES = {name: spec.interval for name, spec in DATASETS.items() if spec.interval is not None}
sys.path.insert(0, os.path.join(ROOT, '..', '.collab', 'tools'))
from monitor import util as U
from monitor.read.collab import dashboard_data

_LOCK = threading.RLock()
_STATE = 'dashboard-publication.json'
_LOCK_PATH = os.path.join(U.STATE, 'dashboard-publication.lock')


def path(kind, name):
    if kind not in KINDS or name not in (AUTO_NAMES if kind == 'auto' else CURATED_NAMES):
        raise ValueError(f'未知观察台数据：{kind}/{name}')
    return os.path.join(DATA, kind, name + '.js')


def read(kind, name):
    path(kind, name)
    return dashboard_data(kind, name)


def _signature(value):
    raw = json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(',', ':'), allow_nan=False)
    return hashlib.sha256(raw.encode('utf-8')).hexdigest()


def _state():
    try:
        with open(os.path.join(U.STATE, _STATE), encoding='utf-8') as stream:
            return json.load(stream)
    except FileNotFoundError:
        return {'at': None, 'versions': {}, 'checked': {}, '_app': {}}


def _replace(target, text):
    os.makedirs(os.path.dirname(target), exist_ok=True)
    replacement = target + '.' + uuid.uuid4().hex + '.tmp'
    try:
        with open(replacement, 'x', encoding='utf-8', newline='\n') as stream:
            stream.write(text)
        os.replace(replacement, target)
    finally:
        if os.path.exists(replacement):
            os.remove(replacement)


def write(kind, name, payload, source=None):
    """数据原子替换后才提交签名；相同内容只推进 checked，不改 updated。"""
    target = path(kind, name)
    content = {key: value for key, value in payload.items() if key != 'updated'}
    if source is not None:
        content['source'] = source
    content.setdefault('errors', [])
    version = _signature(content)
    now = U.iso(time.time())
    with _LOCK, U.file_lock(_LOCK_PATH):
        state = _state()
        prior = read(kind, name)
        old = {key: value for key, value in (prior or {}).items() if key != 'updated'}
        if prior is None or old != content:
            value = {**content, 'updated': now}
            _replace(target, f'Dash.put({json.dumps(name)}, {json.dumps(value, ensure_ascii=False, allow_nan=False)});\n')
        if kind == 'auto':
            state['versions'][name] = version
            # 失败本身是可发布内容，但不能冒充成功确认了来源。
            if not content['errors']:
                state['checked'][name] = now
            state['at'] = now
            U.state_write(_STATE, state)
    return target


def publication():
    """只读完整已提交快照；新安装尚无发布时明确为空，不读旧数据猜版本。"""
    state = _state()
    return {key: state[key] for key in ('at', 'versions', 'checked')}


def check(names, at=None):
    """调用方已确认来源仍干净；仅推进已发布数据的 checked。"""
    stamp = at or U.iso(time.time())
    with _LOCK, U.file_lock(_LOCK_PATH):
        state = _state()
        known = [name for name in names if name in state['versions'] and name in AUTO_NAMES]
        if known:
            state['checked'].update({name: stamp for name in known})
            state['at'] = stamp
            U.state_write(_STATE, state)


def publish_app(changes=None):
    """首次建立界面内容清单；后续只重读通知点名的文件，删除同样换版本。"""
    # 文件读取/散列在发布锁外；慢界面文件不能卡住实时数据和 API。
    if changes is None:
        targets = [os.path.join(ROOT, 'index.html')]
        for directory in ('app', 'vendor'):
            targets.extend(os.path.join(root, name) for root, _, names in os.walk(os.path.join(ROOT, directory)) for name in names)
    else:
        targets = list(changes)
        # 目录整体移入时只展开这一棵新子树，不重走整个界面目录。
        for target in tuple(targets):
            if os.path.isdir(target):
                targets.extend(os.path.join(root, name) for root, _, names in os.walk(target) for name in names)
    updates = {}
    removed = []
    for target in targets:
        relative = os.path.relpath(os.path.abspath(target), ROOT).replace('\\', '/')
        if relative != 'index.html' and not relative.startswith(('app/', 'vendor/')):
            continue
        try:
            with open(target, 'rb') as stream:
                updates[relative] = hashlib.sha256(stream.read()).hexdigest()
        except FileNotFoundError:
            removed.append(relative)
        except IsADirectoryError:
            continue
        except PermissionError:
            if not os.path.isdir(target):
                raise
    with _LOCK, U.file_lock(_LOCK_PATH):
        state = _state()
        files = {} if changes is None else dict(state.get('_app', {}))
        for relative in removed:
            files = {name: digest for name, digest in files.items() if name != relative and not name.startswith(relative.rstrip('/') + '/')}
        files.update(updates)
        version = _signature(files)
        if state['versions'].get('app') != version:
            state.update(at=U.iso(time.time()), _app=files)
            state['versions']['app'] = version
            U.state_write(_STATE, state)
    return version
